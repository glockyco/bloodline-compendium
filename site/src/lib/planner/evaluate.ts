import { attributeEffects, attributes, type AttributeEffects } from '../rules/attributes.js';
import { characterLevelGrant, proficiencyPointsPerLevel, rankCosts } from '../rules/progression.js';
import type { PlannerData } from '../site-data.js';
import type { PlannerState } from './state.js';

/** Points earned and spent. Remaining is null when the earned points are unknown, as Blood Magic's without an entry. */
export interface Balance {
	earned: number | null;
	spent: number;
	remaining: number | null;
}

/** A balance whose earned points the character level always sets. */
export interface KnownBalance extends Balance {
	earned: number;
	remaining: number;
}

/** Something in a build that the game would not allow, kept visible until the reader fixes it. */
export type Diagnostic =
	| { kind: 'level'; entry: string; discipline: string; needs: number }
	| { kind: 'ranks'; entry: string; ranks: number; max: number }
	| { kind: 'overspent'; balance: string; by: number }
	| { kind: 'attribute'; attribute: string; level: number }
	| { kind: 'unresolved'; entry: string }
	| { kind: 'hotbar'; entry: string; slot: number };

export interface Evaluation {
	skillPoints: KnownBalance;
	levelUpPoints: KnownBalance;
	/** Each discipline's proficiency points, by discipline key, for the disciplines that have passives. */
	proficiency: Record<string, Balance>;
	/** What the attributes give, or null when one is outside the measured levels. */
	effects: AttributeEffects | null;
	diagnostics: Diagnostic[];
	valid: boolean;
}

/** A discipline's planned level, 1 unless the build sets another. */
export function levelOf(state: PlannerState, discipline: string): number {
	return state.skills.disciplines[discipline] ?? 1;
}

function balance(earned: number | null, spent: number): Balance {
	return { earned, spent, remaining: earned === null ? null : earned - spent };
}

/**
 * Runs the rules over a build: the skill, level up and proficiency point balances, the attribute effects, and every
 * choice the game would refuse at these levels. The editor, the link, the text and the page cues read this result.
 */
export function evaluate(state: PlannerState, data: PlannerData): Evaluation {
	const diagnostics: Diagnostic[] = [];
	const disciplines = new Map(data.disciplines.map((discipline) => [discipline.key, discipline]));
	const slots = new Map(
		data.disciplines.flatMap((discipline) =>
			discipline.slots.map((slot) => [slot.skill, slot] as const)
		)
	);
	const learnable = new Set(data.skills.filter((skill) => skill.learned).map((skill) => skill.key));

	const gainedLevels = state.attributes.level - 1;
	const known = (earned: number, spent: number): KnownBalance => ({
		earned,
		spent,
		remaining: earned - spent
	});
	const skillPoints = known(
		gainedLevels * characterLevelGrant.skillPoints,
		state.skills.unlocked.length
	);
	const levelUpPoints = known(
		gainedLevels * characterLevelGrant.levelUpPoints,
		attributes.reduce((sum, attribute) => sum + (state.attributes.points[attribute] - 1), 0)
	);

	for (const skill of state.skills.unlocked) {
		const slot = slots.get(skill);
		if (!slot) diagnostics.push({ kind: 'unresolved', entry: skill });
		else if (levelOf(state, slot.levelDiscipline) < slot.requiredLevel)
			diagnostics.push({
				kind: 'level',
				entry: skill,
				discipline: slot.levelDiscipline,
				needs: slot.requiredLevel
			});
	}
	for (const skill of state.skills.learned)
		if (!learnable.has(skill)) diagnostics.push({ kind: 'unresolved', entry: skill });

	const proficiency: Record<string, Balance> = {};
	const spent = new Map<string, number>();
	for (const [key, ranks] of Object.entries(state.skills.passives)) {
		const owner = data.disciplines.find((discipline) =>
			discipline.passives.some((passive) => passive.key === key)
		);
		const passive = owner?.passives.find((entry) => entry.key === key);
		if (!owner || !passive) {
			diagnostics.push({ kind: 'unresolved', entry: key });
			continue;
		}
		const costs = rankCosts(passive);
		if (ranks > costs.length)
			diagnostics.push({ kind: 'ranks', entry: key, ranks, max: costs.length });
		if (levelOf(state, passive.levelDiscipline) < passive.requiredLevel)
			diagnostics.push({
				kind: 'level',
				entry: key,
				discipline: passive.levelDiscipline,
				needs: passive.requiredLevel
			});
		spent.set(
			owner.key,
			(spent.get(owner.key) ?? 0) + costs.slice(0, ranks).reduce((sum, cost) => sum + cost, 0)
		);
	}
	for (const discipline of data.disciplines) {
		if (!discipline.passives.length || discipline.levelName === null) continue;
		const level = levelOf(state, discipline.key);
		// Blood Magic's points follow the Blood Levels setting rather than its level, so the reader enters them.
		let earned: number | null = proficiencyPointsPerLevel(discipline.levelName, 1).setByBloodLevels
			? state.skills.bloodMagicPoints
			: 0;
		for (let from = 1; from < level && earned !== null; from++) {
			const points = proficiencyPointsPerLevel(discipline.levelName, from);
			if (points.setByBloodLevels) break;
			earned = points.known ? earned + points.value : null;
		}
		proficiency[discipline.key] = balance(earned, spent.get(discipline.key) ?? 0);
	}

	for (const [name, points] of [
		['skill points', skillPoints],
		['level up points', levelUpPoints],
		...Object.entries(proficiency).map(
			([key, points]) => [disciplines.get(key)!.name + ' points', points] as const
		)
	] as const)
		if (points.remaining !== null && points.remaining < 0)
			diagnostics.push({ kind: 'overspent', balance: name, by: -points.remaining });

	const effects = attributeEffects(state.attributes.points);
	if (!effects.known)
		for (const attribute of effects.missing)
			diagnostics.push({
				kind: 'attribute',
				attribute,
				level: state.attributes.points[attribute as keyof typeof state.attributes.points]
			});

	state.skillBars.main.forEach((skill, slot) => {
		if (
			skill !== null &&
			!state.skills.unlocked.includes(skill) &&
			!state.skills.learned.includes(skill)
		)
			diagnostics.push({ kind: 'hotbar', entry: skill, slot });
	});

	return {
		skillPoints,
		levelUpPoints,
		proficiency,
		effects: effects.known ? effects.value : null,
		diagnostics,
		valid: diagnostics.length === 0
	};
}
