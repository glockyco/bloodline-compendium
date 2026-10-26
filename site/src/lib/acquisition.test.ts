import { expect, it } from 'vitest';
import { formatAmount, sortSources, sourceContext } from './acquisition.js';
import fixture from '../../fixtures/site-data/items/lanternedge.json';
import type { ItemPage } from './site-data.js';
const item = fixture as ItemPage;

it('keeps item quantities separate from the chance per container', () => {
	const source = item.sources.find((entry) => entry.kind === 'containerLoot')!;
	expect(formatAmount(source.amount!)).toBe('1 to 2');
	expect(sourceContext(source)).toBe('50.8% per container');
});

it('gives no chance where the selection rule is unknown', () => {
	const source = item.sources.find((entry) => entry.kind === 'containerLoot')!;
	const odds = { event: 'container' as const, chance: null, expected: null, unknown: 'Unknown.' };
	expect(sourceContext({ ...source, odds })).toBeNull();
});

it('orders comparable shop prices before selecting the answer summary', () => {
	const sources = sortSources(item.sources.filter((source) => source.kind === 'shop'));
	expect(sources[0].price?.amount).toBe(45);
	// The base value 45 at neutral respect's multiplier of 1.4 costs 63.
	expect(sourceContext(sources[0])).toBe('63 gold');
});
