import { expect, it } from 'vitest';
import { entityPath, kindPath } from './entities.js';
import type { EntityKindDefinition, EntityReference } from './site-data.js';
import registry from '../../fixtures/site-data/kinds.json';
const kinds = registry as EntityKindDefinition[];
const merchant: EntityReference = {
	kind: 'merchant',
	key: '/Invented/Merchant',
	name: 'Merchant',
	slug: null,
	icon: null
};

it('links only resolved references whose registry kind has pages', () => {
	expect(entityPath({ ...merchant, kind: 'item', slug: 'invented blade' }, kinds)).toBe(
		'/items/invented%20blade/'
	);
	expect(entityPath({ ...merchant, slug: 'invented' }, kinds)).toBeNull();
	expect(entityPath({ ...merchant, kind: 'item', key: null, slug: 'invented' }, kinds)).toBeNull();
});

it('uses registry routes when a reference kind gains pages', () => {
	const promoted = kinds.map((kind) =>
		kind.kind === 'merchant' ? { ...kind, hasPages: true, route: 'people' } : kind
	);
	expect(entityPath({ ...merchant, slug: 'invented' }, promoted)).toBe('/people/invented/');
});

it.each(kinds.filter((kind) => !kind.hasPages))('keeps $kind references unlinked', (kind) => {
	expect(entityPath({ ...merchant, kind: kind.kind, slug: 'invented' }, kinds)).toBeNull();
	expect(entityPath({ ...merchant, kind: kind.kind }, kinds)).toBeNull();
});

it('derives list paths from registry routes and page decisions', () => {
	expect(kindPath('item', kinds)).toBe('/items/');
	expect(kindPath('merchant', kinds)).toBeNull();
	const moved = kinds.map((kind) =>
		kind.kind === 'item' ? { ...kind, route: 'inventory' } : kind
	);
	expect(kindPath('item', moved)).toBe('/inventory/');
	expect(entityPath({ ...merchant, kind: 'item', slug: 'invented' }, moved)).toBe(
		'/inventory/invented/'
	);
});

it('keeps query and fragment suffixes inside the navigation boundary', () => {
	expect(kindPath('item', kinds, '?category=weapon#results')).toBe(
		'/items/?category=weapon#results'
	);
});

it('rejects registry routes that are not local route segments', () => {
	const invalid = kinds.map((kind) =>
		kind.kind === 'item' ? { ...kind, route: '../outside' } : kind
	);
	expect(() => kindPath('item', invalid)).toThrow('Invalid entity route');
});
