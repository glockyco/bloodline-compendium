import { expect, it } from 'vitest';
import {
	formatAmount,
	firstRoute,
	sortSources,
	sourceContext,
	summaryConditions,
	summaryContext
} from './acquisition.js';
import fixture from '../../fixtures/site-data/items/lanternedge.json';
import type { ItemPage } from './site-data.js';
const item = fixture as ItemPage;

it('keeps item quantities separate from the chance per container', () => {
	const source = item.sources.find((entry) => entry.kind === 'containerLoot')!;
	expect(formatAmount(source.amount!)).toBe('1 to 2');
	expect(sourceContext(source)).toBe('50.8% to 86.6% per container');
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
const withChance = (chance: number, highestChance?: number | null) => ({
	...container,
	odds: { ...container.odds!, chance, highestChance }
});

it('states a chance in a summary only when every source shares it, and otherwise its range', () => {
	expect(summaryContext([withChance(0.5), withChance(0.5)])).toBe('50% per container');
	expect(summaryContext([withChance(0.017), withChance(0.385)])).toBe(
		'1.7% to 38.5% per container'
	);
	// A source without a known chance leaves the summary without one.
	expect(summaryContext([withChance(0.5), { ...container, odds: null }])).toBeNull();
});

it('formats a source chance range and falls back to its lower bound when no upper bound is set', () => {
	expect(sourceContext(withChance(0.125, 0.375))).toBe('12.5% to 37.5% per container');
	expect(sourceContext(withChance(0.125))).toBe('12.5% per container');
	expect(sourceContext(withChance(0.125, null))).toBe('12.5% per container');
	expect(sourceContext(withChance(0.0001, 0.0005))).toBe(
		'about 1 in 10,000 to 1 in 2,000 per container'
	);
});

it('summarizes both bounds across ranged and fixed source chances', () => {
	expect(summaryContext([withChance(0.125, 0.75)])).toBe('12.5% to 75% per container');
	expect(summaryContext([withChance(0.125, 0.75), withChance(0.25, 0.5)])).toBe(
		'12.5% to 75% per container'
	);
	expect(summaryContext([withChance(0.125, 0.375), withChance(0.75)])).toBe(
		'12.5% to 75% per container'
	);
	expect(summaryContext([withChance(0.5, null)])).toBe('50% per container');
});

it('orders sources by their highest chance without mutating the input', () => {
	const ranged = { ...withChance(0.125, 0.75), name: 'Ranged' };
	const fixed = { ...withChance(0.5), name: 'Fixed' };
	const nullable = { ...withChance(0.25, null), name: 'Nullable' };
	const zero = { ...withChance(0, 0), name: 'Zero' };
	const unknown = { ...container, name: 'Unknown', odds: null };
	const sources = [unknown, nullable, fixed, zero, ranged];
	expect(sortSources(sources)).toEqual([ranged, fixed, nullable, zero, unknown]);
	expect(sources).toEqual([unknown, nullable, fixed, zero, ranged]);
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

it('omits unconfirmed routes from previews and search descriptions', () => {
	const unconfirmed = { ...container, kind: 'dialogue' as const, unconfirmed: true };
	const confirmed = { ...container, unconfirmed: false };
	expect(firstRoute([unconfirmed], ['dialogue'])).toBeNull();
	expect(firstRoute([unconfirmed, confirmed], ['dialogue', 'containerLoot'])).toBe(
		'Found in Stargazer cache in Glass Reach, 50.8% to 86.6% per container'
	);
	expect(
		firstRoute(
			[{ ...unconfirmed, kind: 'containerLoot', name: 'Unused cache' }, confirmed],
			['containerLoot']
		)
	).toBe('Found in Stargazer cache in Glass Reach, 50.8% to 86.6% per container');
});

it('describes loose loot as found on the ground with its known place and odds', () => {
	const pickup = {
		...container,
		kind: 'pickup' as const,
		unconfirmed: false,
		odds: { ...container.odds!, event: 'lootSpot' as const }
	};
	expect(firstRoute([pickup], ['pickup'])).toBe(
		'Found on the ground in Glass Reach, 50.8% to 86.6% per loot spot'
	);
});

it('counts an item currency instead of pluralizing its name', () => {
	const source = item.sources.find((entry) => entry.kind === 'currencyShop')!;
	expect(sourceContext(source)).toBe('3 × Cinderpear');
	expect(summaryContext([source, source])).toBe('3 × Cinderpear');
});
