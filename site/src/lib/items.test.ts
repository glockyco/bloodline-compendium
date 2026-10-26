import { expect, it } from 'vitest';
import { selectItems } from './items.js';
import fixture from '../../fixtures/site-data/items.json';
import type { ItemIndex } from './site-data.js';
const index = fixture as ItemIndex;
it('filters categories and sorts numeric values with unknown values last', () => {
	expect(selectItems(index.items, 'weapons', 'name-asc').map((i) => i.name)).toEqual([
		'Lanternedge'
	]);
	expect(selectItems(index.items, '', 'value-desc').map((i) => i.value)).toEqual([90, 45, 8, null]);
	expect(selectItems(index.items, '', 'value-asc').map((i) => i.value)).toEqual([8, 45, 90, null]);
	expect(selectItems(index.items, '', 'name-desc').map((i) => i.name)).toEqual([
		'Mosswoven Coat',
		'Lanternedge',
		'Hollow Prism',
		'Cinderpear'
	]);
});
