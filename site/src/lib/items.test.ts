import { expect, it } from 'vitest';
import { filterItems, matchesItem, readFilters } from './items.js';
import fixture from '../../fixtures/site-data/items.json';
import type { ItemIndex } from './site-data.js';
const index = fixture as ItemIndex;
const defaults = readFilters(new URLSearchParams());

it('hides items without a known source by default without removing their records', () => {
	expect(filterItems(index.items, defaults).map((item) => item.name)).toEqual([
		'Cinderpear',
		'Lanternedge',
		'Mosswoven Coat'
	]);
	expect(filterItems(index.items, { ...defaults, showUnknown: true })).toHaveLength(4);
	expect(index.items).toHaveLength(4);
});

it('matches several values in a facet and combines different facets', () => {
	const filters = { ...defaults, categories: ['weapons', 'food'], types: ['Sword'] };
	expect(filterItems(index.items, filters).map((item) => item.name)).toEqual(['Lanternedge']);
	expect(filterItems(index.items, { ...filters, types: [] })).toHaveLength(2);
	expect(
		index.items.filter(
			(item) => item.category === 'food' && matchesItem(item, { ...filters, categories: [] })
		)
	).toHaveLength(0);
});

it('restores all supported filters and sort from a shared URL', () => {
	expect(
		readFilters(
			new URLSearchParams(
				'name=lantern&category=weapons&category=food&type=Sword&unknown=1&sort=value-desc'
			)
		)
	).toEqual({
		name: 'lantern',
		categories: ['weapons', 'food'],
		types: ['Sword'],
		showUnknown: true,
		sort: 'value-desc'
	});
	expect(readFilters(new URLSearchParams('sort=not-a-sort')).sort).toBe('name-asc');
});

it('sorts numeric values with unknown values last in both directions', () => {
	expect(
		filterItems(index.items, { ...defaults, showUnknown: true, sort: 'value-desc' }).map(
			(item) => item.value
		)
	).toEqual([90, 45, 8, null]);
	expect(
		filterItems(index.items, { ...defaults, showUnknown: true, sort: 'value-asc' }).map(
			(item) => item.value
		)
	).toEqual([8, 45, 90, null]);
});

it('sorts types and filters names without case sensitivity', () => {
	expect(
		filterItems(index.items, { ...defaults, sort: 'type-asc' }).map((item) => item.type)
	).toEqual(['Coat', 'Fruit', 'Sword']);
	expect(
		filterItems(index.items, { ...defaults, name: 'LANTERN' }).map((item) => item.slug)
	).toEqual(['lanternedge']);
});
