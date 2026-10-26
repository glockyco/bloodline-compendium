import { readCoverage } from '#lib/server/data.js';
export async function load() {
	return { coverage: await readCoverage() };
}
