import { attributes, measuredLevels, type Attribute } from '../rules/attributes.js';
import { rankCosts } from '../rules/progression.js';
import type { PlannerData } from '../site-data.js';
import { evaluate, levelOf } from './evaluate.js';

/** The skill bar's slots: two rows of five, as the game lays them out. */
export const hotbarSlots = 10;

/** The character's level and the points in each attribute. */
export interface AttributesSection {
	level: number;
	points: Record<Attribute, number>;
}

/** Discipline levels default to 1 and passive ranks to 0, so the section stores only what differs. */
export interface SkillsSection {
	disciplines: Record<string, number>;
	/** Skills unlocked in the trees, which cost a skill point each. */
	unlocked: string[];
	/** Skills a book, conversation or event taught, which cost no skill point. */
	learned: string[];
	passives: Record<string, number>;
	/** The Blood Magic proficiency points the character has, which its level does not set. */
	bloodMagicPoints: number | null;
}

export interface SkillBarsSection {
	/** The two rows of five the game shows. */
	main: (string | null)[];
}

/**
 * One planned character as sections, each keyed by catalog key so a link survives renames. A later planner adds
 * sections, such as equipment, without reshaping these, and `later` keeps the sections of a later planner that this one
 * does not know, as read, so saving and sharing pass them on.
 */
export interface PlannerState {
	/** The game build the inputs came from when the build was planned. */
	build: number;
	attributes: AttributesSection;
	skills: SkillsSection;
	skillBars: SkillBarsSection;
	later: Record<string, unknown>;
}

/** A new character: level 1, every attribute and discipline at 1, nothing bought. */
export function newCharacter(data: PlannerData): PlannerState {
	return {
		build: data.build,
		attributes: {
			level: 1,
			points: Object.fromEntries(attributes.map((attribute) => [attribute, 1])) as Record<
				Attribute,
				number
			>
		},
		skills: { disciplines: {}, unlocked: [], learned: [], passives: {}, bloodMagicPoints: null },
		skillBars: { main: Array(hotbarSlots).fill(null) },
		later: {}
	};
}

/** The outcome of an edit: the new state, or the unchanged state and why the game would refuse it. */
export type Edit =
	{ state: PlannerState; refused?: undefined } | { state: PlannerState; refused: string };

const refuse = (state: PlannerState, refused: string): Edit => ({ state, refused });

const withAttributes = (state: PlannerState, change: Partial<AttributesSection>): Edit => ({
	state: { ...state, attributes: { ...state.attributes, ...change } }
});
const withSkills = (state: PlannerState, change: Partial<SkillsSection>): Edit => ({
	state: { ...state, skills: { ...state.skills, ...change } }
});

/** A skill bar without a skill the character no longer knows. */
function withoutOnBars(state: PlannerState, skill: string): SkillBarsSection {
	return {
		...state.skillBars,
		main: state.skillBars.main.map((slot) => (slot === skill ? null : slot))
	};
}

/** Sets a whole number at least 1, as the game's levels are. Lowering keeps choices that no longer fit. */
function positive(value: number): number {
	return Math.max(1, Math.floor(value));
}

export function setLevel(state: PlannerState, level: number): Edit {
	return withAttributes(state, { level: positive(level) });
}

export function setDisciplineLevel(state: PlannerState, discipline: string, level: number): Edit {
	return withSkills(state, {
		disciplines: { ...state.skills.disciplines, [discipline]: positive(level) }
	});
}

/**
 * Sets an attribute's points. The game starts every attribute at 1 and refuses to go lower, and its effects are known
 * only up to the measured level.
 */
export function setAttribute(state: PlannerState, attribute: Attribute, value: number): Edit {
	const level = Math.floor(value);
	if (level < measuredLevels.min)
		return refuse(state, `${attribute} starts at 1 and cannot go lower.`);
	if (level > measuredLevels.max)
		return refuse(state, `${attribute} effects are known up to ${measuredLevels.max}.`);
	return withAttributes(state, { points: { ...state.attributes.points, [attribute]: level } });
}

/**
 * Unlocks or refunds a tree skill. The game unlocks a skill only once its discipline reaches the slot's level and a
 * skill point is left, and refunds the point when the skill is clicked again.
 */
export function toggleSkill(state: PlannerState, data: PlannerData, skill: string): Edit {
	if (state.skills.unlocked.includes(skill))
		return {
			state: {
				...state,
				skills: {
					...state.skills,
					unlocked: state.skills.unlocked.filter((key) => key !== skill)
				},
				skillBars: state.skills.learned.includes(skill)
					? state.skillBars
					: withoutOnBars(state, skill)
			}
		};
	const slot = data.disciplines
		.flatMap((discipline) => discipline.slots)
		.find((entry) => entry.skill === skill);
	if (!slot) return refuse(state, 'No skill tree holds this skill.');
	const checked = data.disciplines.find((discipline) => discipline.key === slot.levelDiscipline);
	if (levelOf(state, slot.levelDiscipline) < slot.requiredLevel)
		return refuse(state, `Needs ${checked?.name ?? 'its discipline'} level ${slot.requiredLevel}.`);
	if (evaluate(state, data).skillPoints.remaining < 1)
		return refuse(state, 'No skill point is left.');
	return withSkills(state, { unlocked: [...state.skills.unlocked, skill] });
}

/** Marks a skill a book, conversation or event teaches as known, or unmarks it. */
export function toggleLearned(state: PlannerState, data: PlannerData, skill: string): Edit {
	if (state.skills.learned.includes(skill))
		return {
			state: {
				...state,
				skills: { ...state.skills, learned: state.skills.learned.filter((key) => key !== skill) },
				skillBars: state.skills.unlocked.includes(skill)
					? state.skillBars
					: withoutOnBars(state, skill)
			}
		};
	if (!data.skills.some((entry) => entry.key === skill && entry.learned))
		return refuse(state, 'No book, conversation or event teaches this skill.');
	return withSkills(state, { learned: [...state.skills.learned, skill] });
}

/**
 * Buys the next rank of a passive. The game sells a rank only once the passive's discipline reaches its level, while
 * ranks remain and its discipline's points cover the rank's cost.
 */
export function buyRank(state: PlannerState, data: PlannerData, passive: string): Edit {
	const owner = data.disciplines.find((discipline) =>
		discipline.passives.some((entry) => entry.key === passive)
	);
	const entry = owner?.passives.find((candidate) => candidate.key === passive);
	if (!owner || !entry) return refuse(state, 'No skill tree holds this passive.');
	const ranks = state.skills.passives[passive] ?? 0;
	const costs = rankCosts(entry);
	if (ranks >= costs.length)
		return refuse(
			state,
			entry.ranks === null ? 'Only its first rank is known.' : 'Every rank is bought.'
		);
	const checked = data.disciplines.find((discipline) => discipline.key === entry.levelDiscipline);
	if (levelOf(state, entry.levelDiscipline) < entry.requiredLevel)
		return refuse(
			state,
			`Needs ${checked?.name ?? 'its discipline'} level ${entry.requiredLevel}.`
		);
	const balance = evaluate(state, data).proficiency[owner.key];
	if (balance?.remaining !== null && balance !== undefined && balance.remaining < costs[ranks])
		return refuse(state, `Needs ${costs[ranks]} ${owner.name} points.`);
	return withSkills(state, { passives: { ...state.skills.passives, [passive]: ranks + 1 } });
}

/** Removes the last bought rank, a planning edit the game itself does not offer. */
export function removeRank(state: PlannerState, passive: string): Edit {
	const ranks = state.skills.passives[passive] ?? 0;
	if (ranks < 1) return refuse(state, 'No rank is bought.');
	const passives = { ...state.skills.passives, [passive]: ranks - 1 };
	if (passives[passive] === 0) delete passives[passive];
	return withSkills(state, { passives });
}

export function setBloodMagicPoints(state: PlannerState, points: number | null): Edit {
	return withSkills(state, {
		bloodMagicPoints: points === null ? null : Math.max(0, Math.floor(points))
	});
}

/** Puts a known skill on a skill bar slot, or empties the slot with null. */
export function placeOnHotbar(state: PlannerState, slot: number, skill: string | null): Edit {
	if (!Number.isInteger(slot) || slot < 0 || slot >= hotbarSlots)
		return refuse(state, 'The skill bar has two rows of five slots.');
	if (
		skill !== null &&
		!state.skills.unlocked.includes(skill) &&
		!state.skills.learned.includes(skill)
	)
		return refuse(state, 'Only an unlocked or learned skill goes on the skill bar.');
	const main = state.skillBars.main.map((entry, index) =>
		index === slot ? skill : entry === skill && skill !== null ? null : entry
	);
	return { state: { ...state, skillBars: { ...state.skillBars, main } } };
}
