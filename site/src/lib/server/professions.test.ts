import { expect, it } from 'vitest';
import { levelBands } from './professions.js';
import type { ProfessionRow } from '../site-data.js';

const rows = (counts: Record<number, number>): ProfessionRow[] =>
	Object.entries(counts).flatMap(([level, count]) =>
		Array.from({ length: count }, (_, index) => ({
			product: {
				kind: 'item' as const,
				key: `/Invented/${level}/${index}`,
				name: `Item ${index}`,
				slug: null,
				icon: null
			},
			kind: 'smithing' as const,
			level: Number(level),
			experience: null,
			inputs: [],
			output: 1
		}))
	);

it('keeps a profession that fits one page on one page', () => {
	expect(levelBands(rows({ 1: 120, 2: 80 }))).toEqual([]);
});

it('splits a long profession into pages of whole levels that each fit the page budget', () => {
	const counts = { 1: 150, 2: 40, 3: 30, 4: 170, 5: 60, 9: 10 };
	const bands = levelBands(rows(counts));
	expect(bands.map((band) => [band.min, band.max, band.segment])).toEqual([
		[1, 2, null],
		[3, 4, 'levels-3-4'],
		[5, 9, 'levels-5-9']
	]);
	for (const band of bands) {
		const shown = Object.entries(counts)
			.filter(([level]) => Number(level) >= band.min && Number(level) <= band.max)
			.reduce((sum, [, count]) => sum + count, 0);
		expect(shown).toBeLessThanOrEqual(200);
	}
});
