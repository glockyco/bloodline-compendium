import type { Rule } from './rule.js';
import { experienceRequirement } from './experience.js';
import { attributeEffects, attributes } from './attributes.js';
import { buyPrice, respectDiscount, sellPrice } from './price.js';
import { characterLevelGrant, proficiencyPointsPerLevel, rankCosts } from './progression.js';

/** Every rule that has game results to compare against. */
export const rules = [experienceRequirement, respectDiscount, buyPrice, sellPrice];

/** One row of a lab `call` result: the inputs set on the object and the outputs read back. */
export interface CallRow {
	inputs: Record<string, number>;
	outputs: Record<string, number>;
}

export interface CallRecord {
	class: string;
	function: string;
	rows: CallRow[];
}

/**
 * The rows where a rule disagrees with the game's own result. Records of other functions are ignored, and a
 * rule result that is unknown counts as a disagreement, because the game gave a value for those inputs.
 */
export function ruleMismatches(
	rule: Rule<Record<string, number>, number>,
	records: CallRecord[]
): string[] {
	const problems: string[] = [];
	for (const record of records) {
		if (!record.class.startsWith(rule.check.class) || record.function !== rule.check.function)
			continue;
		for (const row of record.rows) {
			const inputs: Record<string, number> = {};
			for (const [name, path] of Object.entries(rule.check.inputs)) inputs[name] = row.inputs[path];
			const game = row.outputs[rule.check.output];
			const result = rule.evaluate(inputs);
			const ours = result.known ? result.value : 'unknown';
			// The lab bridge prints floats with 14 significant digits, so values compare at 32-bit precision.
			if (typeof ours !== 'number' || Math.fround(ours) !== Math.fround(game))
				problems.push(`${rule.id} for ${JSON.stringify(inputs)}: rule ${ours}, game ${game}`);
		}
	}
	return problems;
}

/** A lab `eval` of `progression.lua`: its arguments and the steps it recorded, each with the numbers that changed. */
export interface ProgressionRecord {
	args: { action?: string; tree?: string; widget?: string };
	steps: {
		changed?: Record<string, [number, number]>;
		/** A passive button's state, or the level up window's texts and attribute levels. */
		widget?: { cost?: number; texts?: Record<string, string>; levels?: Record<string, number> };
	}[];
}

/**
 * The level ups where the proficiency points a discipline gained differ from the rule. A step counts when exactly
 * one tracked level rose by one, so a step that skipped levels, as large grants do, cannot blur the amount. Blood
 * Magic's points follow the Blood Levels setting, which these runs do not record, so they are left out.
 */
export function proficiencyMismatches(records: ProgressionRecord[]): {
	compared: number;
	problems: string[];
} {
	let compared = 0;
	const problems: string[] = [];
	for (const record of records) {
		if (record.args.action !== 'discipline') continue;
		for (const step of record.steps) {
			const changed = step.changed ?? {};
			const level = Object.keys(changed).find(
				(name) => name.startsWith('instance.') && name.endsWith(' Level')
			);
			if (!level) continue;
			const [from, to] = changed[level];
			if (to - from !== 1) continue;
			const name = level.slice('instance.'.length, -' Level'.length);
			const expected = proficiencyPointsPerLevel(name, from);
			if (!expected.known) continue;
			const points = Object.keys(changed).find(
				(key) => key.startsWith('instance.') && key.includes('Point')
			);
			const gained = points ? changed[points][1] - changed[points][0] : 0;
			compared++;
			if (gained !== expected.value)
				problems.push(
					`proficiency-points for ${name} level ${from}: rule ${expected.value}, game ${gained}`
				);
		}
	}
	return { compared, problems };
}

/**
 * The passive purchases where the points the game took differ from the rule's rank costs. Each press of a passive's
 * button that took points bought the next rank, so the amounts taken, in order, are its rank costs.
 */
export function rankCostMismatches(
	records: ProgressionRecord[],
	passives: Map<string, { pointCost: number; costIncrease: number; ranks: number | null }>
): { compared: number; problems: string[] } {
	let compared = 0;
	const problems: string[] = [];
	for (const record of records) {
		if (record.args.action !== 'passive' || !record.args.tree || !record.args.widget) continue;
		const passive = passives.get(record.args.tree + ':' + record.args.widget);
		if (!passive) {
			problems.push(`next-rank-cost has no passive ${record.args.tree} ${record.args.widget}`);
			continue;
		}
		const taken = record.steps.flatMap((step) =>
			Object.entries(step.changed ?? {})
				.filter(([key, [before, after]]) => key.includes('Point') && after < before)
				.map(([, [before, after]]) => before - after)
		);
		const costs = rankCosts(passive);
		compared += taken.length;
		taken.forEach((amount, rank) => {
			if (amount !== costs[rank])
				problems.push(
					`next-rank-cost for ${record.args.widget} rank ${rank + 1}: rule ${costs[rank]}, game ${amount}`
				);
		});
	}
	return { compared, problems };
}

/**
 * The character level ups where the skill points or level up points the game added differ from the level grant rule.
 * Only a step that raised the level by one counts, so a step that crossed several levels cannot blur the amounts.
 */
export function levelGrantMismatches(records: ProgressionRecord[]): {
	compared: number;
	problems: string[];
} {
	let compared = 0;
	const problems: string[] = [];
	for (const record of records) {
		if (record.args.action !== 'levels') continue;
		for (const step of record.steps) {
			const changed = step.changed ?? {};
			const level = changed['instance.Player Level'];
			if (!level || level[1] - level[0] !== 1) continue;
			const gained = (key: string) => (changed[key] ? changed[key][1] - changed[key][0] : 0);
			compared++;
			const skill = gained('instance.Skill Points');
			const points = gained('instance.Level Up Points');
			if (skill !== characterLevelGrant.skillPoints || points !== characterLevelGrant.levelUpPoints)
				problems.push(
					`level-grant for level ${level[0]}: rule ${characterLevelGrant.skillPoints} skill and ${characterLevelGrant.levelUpPoints} level up points, game ${skill} and ${points}`
				);
		}
	}
	return { compared, problems };
}

/** The values the level up window shows, each read after its label, such as "Pickup Strength: 515". */
function panelOf(text: string) {
	const read = (label: string) => {
		const match = new RegExp(`(?:^|\\s)${label}: \\+?([\\d,.]+)`).exec(text);
		return match ? Number(match[1].replace(/,/g, '')) : undefined;
	};
	return {
		health: read('Health'),
		mana: read('Mana'),
		stamina: read('Stamina'),
		healthRegen: read('Health Regen'),
		manaRegen: read('Mana Regen'),
		staminaRegen: read('Stamina Regen'),
		armorWeightReduction: read('Armor Weight Reduction'),
		diseaseResistance: read('Disease Resistance'),
		pickupStrength: read('Pickup Strength'),
		strengthExpBonus: read('Strength EXP bonus'),
		intelligenceExpBonus: read('Intelligence EXP bonus'),
		dexterityExpBonus: read('Dexterity EXP bonus')
	};
}

/**
 * The level up window readings where a value the window shows differs from the attribute rules at the levels it
 * recorded. The window rounds regeneration to two decimals and the EXP bonuses to one, so each value compares at the
 * precision the window shows it.
 */
export function attributeMismatches(records: ProgressionRecord[]): {
	compared: number;
	problems: string[];
} {
	let compared = 0;
	const problems: string[] = [];
	for (const record of records) {
		if (record.args.action !== 'attributes') continue;
		for (const step of record.steps) {
			const widget = step.widget;
			if (!widget?.texts || !widget.levels) continue;
			const effects = attributeEffects(widget.levels);
			if (!effects.known) continue;
			const shown = panelOf(Object.values(widget.texts).join('\n'));
			compared++;
			for (const [name, value] of Object.entries(shown)) {
				if (value === undefined) continue;
				const ours = effects.value[name as keyof typeof effects.value];
				const decimals = (String(value).split('.')[1] ?? '').length;
				if (Math.abs(ours - value) > 0.5 * 10 ** -decimals + 1e-9)
					problems.push(
						`attributes at ${attributes.map((attribute) => widget.levels![attribute]).join('/')}: ${name} rule ${ours}, game ${value}`
					);
			}
		}
	}
	return { compared, problems };
}
