/**
 * A game rule that pages, calculators and the planner share. A rule reproduces one game function, cites it, and
 * explains its result in steps a reader can follow. An input the reader has not given makes the result unknown
 * rather than a guess.
 */
export interface Rule<Inputs extends Record<string, number>, Output> {
	id: string;
	/** The game function the rule reproduces, as Package:Function. */
	cites: string;
	/** Where the lab's `call` command reads the game's own result, so the local rule check can compare. */
	check: { class: string; function: string; inputs: Record<keyof Inputs, string>; output: string };
	evaluate(inputs: Partial<Inputs>): RuleResult<Output>;
}

export type RuleResult<Output> =
	{ known: true; value: Output; steps: string[] } | { known: false; missing: string[] };

/** The names of inputs that are absent or not finite numbers. */
export function missingInputs(
	inputs: Record<string, number | undefined>,
	names: string[]
): string[] {
	return names.filter((name) => {
		const value = inputs[name];
		return value === undefined || !Number.isFinite(value);
	});
}
