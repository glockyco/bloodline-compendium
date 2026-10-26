import { expect, it } from 'vitest';
import { facetGroups, matches, readState, shownColumns, sortItems, writeState } from './list.js';
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
