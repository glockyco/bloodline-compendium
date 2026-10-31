import { expect, it } from 'vitest';
import {
	formatAmount,
	sortSources,
	sourceContext,
	summarize,
	summaryConditions,
	summaryContext
} from './acquisition.js';
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

const container = item.sources.find((entry) => entry.kind === 'containerLoot')!;
const withChance = (chance: number) => ({ ...container, odds: { ...container.odds!, chance } });

it('states a chance in a summary only when every source shares it, and otherwise its range', () => {
	expect(summaryContext([withChance(0.5), withChance(0.5)])).toBe('50% per container');
	expect(summaryContext([withChance(0.017), withChance(0.385)])).toBe(
		'1.7% to 38.5% per container'
	);
	// A source without a known chance leaves the summary without one.
	expect(summaryContext([withChance(0.5), { ...container, odds: null }])).toBeNull();
});

it('states the range of levels when the summarized recipes need different levels of one skill', () => {
	const level = (value: number) => ({
		...container,
		conditions: [
			{
				entity: null,
				kind: 'proficiencyLevel' as const,
				label: `Requires weaponsmithing level ${value}.`,
				value
			}
		]
	});
	expect(summaryConditions([level(10), level(14), level(40)])).toEqual([
		'Requires weaponsmithing level 10 to 40.'
	]);
	expect(summaryConditions([level(12), level(12)])).toEqual(['Requires weaponsmithing level 12.']);
	expect(summaryConditions([level(12), { ...container, conditions: [] }])).toEqual([]);
});

it('says that a lying item lies in an unknown place when no place is recorded', () => {
	const pickup = { ...container, kind: 'pickup' as const, places: [], odds: null };
	expect(summarize('pickup', [pickup])).toMatchObject({
		verb: 'Found on the ground in',
		places: ['an unknown place']
	});
});

it('counts an item currency instead of pluralizing its name', () => {
	const source = item.sources.find((entry) => entry.kind === 'currencyShop')!;
	expect(sourceContext(source)).toBe('3 × Cinderpear');
	expect(summaryContext([source, source])).toBe('3 × Cinderpear');
});
