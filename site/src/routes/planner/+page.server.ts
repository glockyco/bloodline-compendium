import { readPlanner } from '#lib/server/data.js';

/** The planner's inputs, which no other route loads. */
export async function load() {
	return { planner: await readPlanner() };
}
