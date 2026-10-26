import type { Rule } from './rule.js';
import { experienceRequirement } from './experience.js';

/** Every rule that has game results to compare against. */
export const rules = [experienceRequirement];

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
			if (ours !== game)
				problems.push(`${rule.id} for ${JSON.stringify(inputs)}: rule ${ours}, game ${game}`);
		}
	}
	return problems;
}
