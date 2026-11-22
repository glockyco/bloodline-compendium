import { formatNumber } from '../format.js';
import { missingInputs, type Rule, type RuleResult } from './rule.js';

/**
 * The cost of a passive's next rank: buying a rank raises the button's Point Cost by its Point Increase Per Level,
 * so rank k costs the first rank's cost plus k - 1 increases. Run passives1 bought Lasting Servitude for 2, 4, 6, 8
 * and 10 points, and Lingering Souls for 1 to 5.
 */
export const nextRankCost: Rule<{ pointCost: number; costIncrease: number }, number> = {
	id: 'next-rank-cost',
	cites: 'PassiveSkillButton:Increase Passive Level',
	check: {
		class: 'PassiveSkillButton_C',
		function: 'Increase Passive Level',
		inputs: { pointCost: 'Point Cost', costIncrease: 'Point Increase Per Level' },
		output: 'Point Cost'
	},
	evaluate(inputs) {
		const missing = missingInputs(inputs, ['pointCost', 'costIncrease']);
		if (missing.length) return { known: false, missing };
		const value = (inputs.pointCost as number) + (inputs.costIncrease as number);
		return {
			known: true,
			value,
			steps: [
				`${formatNumber(inputs.pointCost as number)} + ${formatNumber(inputs.costIncrease as number)} = ${formatNumber(value)} points`
			]
		};
	}
};

/**
 * The cost of each rank of a passive, from its first rank's cost, its increase and its number of ranks. A passive
 * whose maximum rank is unknown lists only its first rank, the one the game is sure to offer.
 */
export function rankCosts(passive: {
	pointCost: number;
	costIncrease: number;
	ranks: number | null;
}): number[] {
	const costs = [passive.pointCost];
	while (costs.length < (passive.ranks ?? 1)) {
		const next = nextRankCost.evaluate({
			pointCost: costs[costs.length - 1],
			costIncrease: passive.costIncrease
		});
		if (!next.known) break;
		costs.push(next.value);
	}
	return costs;
}

/**
 * Proficiency points a discipline grants when it reaches the next level, by the name of the level the game tracks,
 * such as Necromancy or Staff. Each discipline's tracking function calls Add Proficiency Points with its own amount:
 * Necromancy 2, Staff 1 for its first level up and none after, Vampirism none, and every other discipline 1. Blood
 * Magic sets its points to the Blood Levels setting plus 1 instead, so its points per level depend on the setting.
 * Runs grants1, progression2 and progression3 recorded every level up.
 */
export function proficiencyPointsPerLevel(
	levelName: string,
	fromLevel: number
): RuleResult<number> & { setByBloodLevels?: true } {
	if (levelName === 'Blood Magic')
		return { known: false, missing: ['Blood Levels setting'], setByBloodLevels: true };
	const value =
		levelName === 'Necromancy'
			? 2
			: levelName === 'Vampirism' || (levelName === 'Staff' && fromLevel > 1)
				? 0
				: 1;
	return {
		known: true,
		value,
		steps: [
			`${levelName} level ${formatNumber(fromLevel)} to ${formatNumber(fromLevel + 1)}: ${formatNumber(value)} points`
		]
	};
}

/** What one character level up grants: skill points for the skill trees and level up points for attributes. */
export interface LevelGrant {
	skillPoints: number;
	levelUpPoints: number;
}

/**
 * The points a character gets for each character level. The player's Level Up adds 1 skill point and 3 level up
 * points, at every level that runs passives1, progression2, treeshots1, planner1 and attributes2 recorded.
 */
export const characterLevelGrant: LevelGrant = { skillPoints: 1, levelUpPoints: 3 };
