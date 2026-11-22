import { formatNumber } from '../format.js';
import { missingInputs, type Rule } from './rule.js';

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

/** The cost of each rank of a passive, from its first rank's cost, its increase and its number of ranks. */
export function rankCosts(passive: { pointCost: number; costIncrease: number; ranks: number }): number[] {
	const costs = [passive.pointCost];
	while (costs.length < passive.ranks) {
		const next = nextRankCost.evaluate({
			pointCost: costs[costs.length - 1],
			costIncrease: passive.costIncrease
		});
		if (!next.known) break;
		costs.push(next.value);
	}
	return costs;
}
