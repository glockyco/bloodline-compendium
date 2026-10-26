import { formatNumber } from '../format.js';
import { missingInputs, type Rule } from './rule.js';

const f = Math.fround;

/**
 * Experience a character needs to finish a level. The game computes it in 32-bit floats, so every step rounds
 * the same way here: ((level + 3) / 4) ^ 1.35 * 150 / 25, rounded half up, times 25.
 */
export const experienceRequirement: Rule<{ level: number }, number> = {
	id: 'experience-requirement',
	cites: 'PlayerPassiveLevelStatComponent:Set Required EXP',
	check: {
		class: 'PlayerPassiveLevelStatComponent_C',
		function: 'Set Required EXP',
		inputs: { level: 'Player Ref.Level' },
		output: 'Player Ref.Required EXP'
	},
	evaluate(inputs) {
		const missing = missingInputs(inputs, ['level']);
		if (missing.length) return { known: false, missing };
		const level = inputs.level as number;
		const base = f(f(level + 3) / 4);
		const curve = f(Math.pow(base, f(1.35)));
		const steps = f(f(curve * 150) / 25);
		const units = Math.floor(f(steps + 0.5));
		const value = units * 25;
		return {
			known: true,
			value,
			steps: [
				`(${formatNumber(level)} + 3) / 4 = ${formatNumber(base)}`,
				`${formatNumber(base)} ^ 1.35 × 150 / 25 = ${formatNumber(steps)}`,
				`rounded to ${formatNumber(units)}, times 25 = ${formatNumber(value)}`
			]
		};
	}
};
