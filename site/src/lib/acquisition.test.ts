import { expect, it } from 'vitest';
import { chanceLabel, formatAmount, sortSources, sourceContext } from './acquisition.js';
import fixture from '../../fixtures/site-data/items/lanternedge.json';
import type { ItemPage } from './site-data.js';
const item = fixture as ItemPage;

it('keeps item quantities separate from a chance per pick', () => {
	const source = item.sources.find((entry) => entry.kind === 'containerLoot')!;
	expect(formatAmount(source.amount!)).toBe('1 to 2');
	expect(formatAmount(source.picks!)).toBe('2 to 3');
	expect(chanceLabel(source)).toBe('Chance per pick');
	expect(sourceContext(source)).toBe('25% chance per pick');
});

it('does not turn a weight or a chance without a basis into drop odds', () => {
	const source = item.sources.find((entry) => entry.kind === 'containerLoot')!;
	expect(sourceContext({ ...source, chance: null, amount: null })).toBe(
		'2 of 18.02 relative chance'
	);
	expect(sourceContext({ ...source, chanceBasis: null, amount: null })).toBe(
		'2 of 18.02 relative chance'
	);
	expect(sourceContext({ ...source, chanceBasis: null, weight: null, amount: null })).toBeNull();
});

it('orders comparable shop prices before selecting the answer summary', () => {
	const sources = sortSources(item.sources.filter((source) => source.kind === 'shop'));
	expect(sources[0].price?.amount).toBe(45);
	// The base value 45 at neutral respect's multiplier of 1.4 costs 63.
	expect(sourceContext(sources[0])).toBe('63 gold');
});
