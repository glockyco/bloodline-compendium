import { expect, it } from 'vitest';
import {
	facetGroups,
	matches,
	nameMatches,
	readState,
	shownColumns,
	sortItems,
	writeState
} from './list.js';
import fixture from '../../fixtures/site-data/items.json';
import type { ItemIndex, ItemSummary } from './site-data.js';
const index = fixture as unknown as ItemIndex;
const read = (query = '') => readState(new URLSearchParams(query), index);
const names = (items: ItemSummary[]) => items.map((item) => item.name);
const shown = (query = '') =>
	sortItems(
		index.items.filter((item) => matches(item, read(query), index)),
		read(query).sort
	);

it('browses known items by default while name search finds every item', () => {
	expect(names(shown())).toEqual(['Cinderpear', 'Lanternedge', 'Mosswoven Coat']);
	expect(names(shown('name=prism'))).toEqual(['Hollow Prism']);
	expect(shown('unknown=1')).toHaveLength(4);
});

it('counts each filter value under the other filters and hides values without items', () => {
	const state = read('category=weapon');
	const groups = facetGroups(
		index.items,
		shown('category=weapon'),
		state,
		index,
		(_, value) => value
	);
	const category = groups.find((group) => group.facet.id === 'category')!;
	// The category's own selection does not narrow its counts.
	expect(category.options.map((option) => [option.value, option.count])).toEqual([
		['weapon', 1],
		['armour', 1],
		['food', 1]
	]);
	const source = groups.find((group) => group.facet.id === 'source')!;
	expect(source.options.map((option) => option.value)).not.toContain('gathering');
	// A filter for weapons appears once the list holds weapons only.
	expect(groups.map((group) => group.facet.id)).toContain('weaponType');
	expect(
		facetGroups(index.items, shown(), read(), index, (_, value) => value).map(
			(group) => group.facet.id
		)
	).not.toContain('weaponType');
});

it('adds the columns of the selected category and of each stat filter', () => {
	expect(
		shownColumns(shown('category=weapon'), read('category=weapon'), index).map((c) => c.id)
	).toEqual(['damage', 'speed']);
	expect(shownColumns(shown(), read('stat=armor::'), index).map((c) => c.id)).toEqual(['armor']);
});

it('combines stat filters with every or any and sorts missing values last', () => {
	expect(names(shown('stat=damage:10:&stat=armor::'))).toEqual([]);
	expect(names(shown('stat=damage:10:&stat=armor::&match=any'))).toEqual([
		'Lanternedge',
		'Mosswoven Coat'
	]);
	expect(names(shown('sort=damage'))).toEqual(['Lanternedge', 'Cinderpear', 'Mosswoven Coat']);
	expect(names(shown('sort=damage&dir=asc'))).toEqual([
		'Lanternedge',
		'Cinderpear',
		'Mosswoven Coat'
	]);
});

it('writes only what differs from the defaults and reads it back', () => {
	const query = 'name=lan&category=weapon&stat=damage:5:&sort=damage&dir=asc&unknown=1';
	expect(writeState(read(query), index).toString()).toBe(
		'name=lan&category=weapon&stat=damage%3A5%3A&unknown=1&sort=damage&dir=asc'
	);
	expect(read('sort=not-a-column').sort).toEqual({ column: 'name', direction: 'asc' });
	expect(read('category=not-a-category').facets).toEqual({});
});

it('sorts words by their rank and keeps words of equal rank together', () => {
	const sword = index.items.find((item) => item.slug === 'lanternedge')!;
	const speed = (name: string, number: number, text: string): ItemSummary => ({
		...sword,
		name,
		cells: { speed: { number, text } }
	});
	const items = [
		speed('A', 3, 'Standard'),
		speed('B', 3, 'Average'),
		speed('C', 5, 'Fast'),
		speed('D', 3, 'Standard'),
		speed('E', 1, 'Slow'),
		speed('F', 3, 'Average')
	];
	expect(names(sortItems(items, { column: 'speed', direction: 'asc' }))).toEqual([
		'E',
		'B',
		'F',
		'A',
		'D',
		'C'
	]);
});

it('keeps a place filter to the selected ways to get an item and counts places the same way', () => {
	const sword = index.items.find((item) => item.slug === 'lanternedge')!;
	const towns = {
		...index,
		facets: index.facets.map((facet) =>
			facet.id === 'place' ? { ...facet, values: ['Port', 'Town'] } : facet
		)
	};
	const item = (name: string, placeSources: Record<string, string[]>): ItemSummary => ({
		...sword,
		name,
		status: 'obtainable',
		facets: {
			category: ['weapon'],
			source: [...new Set(Object.values(placeSources).flat())],
			place: Object.keys(placeSources)
		},
		placeSources: Object.values(placeSources)
	});
	const items = [
		item('Sold here', { Town: ['shop'] }),
		item('Looted here, sold elsewhere', { Town: ['containerLoot'], Port: ['shop'] })
	];
	const state = readState(new URLSearchParams('source=shop&place=Town'), towns);
	expect(names(items.filter((entry) => matches(entry, state, towns)))).toEqual(['Sold here']);
	const groups = facetGroups(items, items, state, towns, (_, value) => value);
	// Places count where items are sold, and ways to get count those in the selected place.
	const count = (facet: string) =>
		groups
			.find((group) => group.facet.id === facet)!
			.options.map((option) => [option.value, option.count]);
	expect(count('place')).toEqual([
		['Port', 1],
		['Town', 1]
	]);
	expect(count('source')).toEqual([
		['shop', 1],
		['containerLoot', 1]
	]);
});

it('finds names the game misspells by one letter and words in any order', () => {
	expect(nameMatches('Steel Rapier Gaurd', 'steel rapier guard')).toBe(true);
	expect(nameMatches('Steel Rapier Gaurd', 'rapier steel')).toBe(true);
	expect(nameMatches('Iron Ingot', 'iron ignot')).toBe(true);
	expect(nameMatches('Iron Ingot', 'ingots')).toBe(true);
	// Short words need an exact start, so a search does not drift to unrelated names.
	expect(nameMatches('Iron Ingot', 'irn')).toBe(false);
	expect(nameMatches('Copper Ore', 'iron')).toBe(false);
});
