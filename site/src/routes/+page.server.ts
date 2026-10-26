import { readIndex, readCoverage } from '#lib/server/data.js';
export async function load() {
	const [index, coverage] = await Promise.all([readIndex(), readCoverage()]);
	return {
		count: index.items.length,
		categories: index.categories,
		gaps: coverage.gaps.reduce((n, gap) => n + gap.entries.length, 0)
	};
}
