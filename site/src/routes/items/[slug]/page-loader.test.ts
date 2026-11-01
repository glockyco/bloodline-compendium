import { expect, it, vi } from 'vitest';
import { entries, load } from './+page.server.js';
import { readGraph, readIndex, readItem, readKinds } from '#lib/server/data.js';
import fixture from '../../../../fixtures/site-data/items/lanternedge.json';
import registry from '../../../../fixtures/site-data/kinds.json';
import type { EntityGraph, EntityKindDefinition, ItemIndex, ItemPage } from '#lib/site-data.js';

vi.mock('#lib/server/data.js', () => ({
	readGraph: vi.fn(),
	readIndex: vi.fn(),
	readItem: vi.fn(),
	readKinds: vi.fn()
}));

it('prerenders one page per plain slug even when distinct graph items share it', async () => {
	const entity = { kind: 'item', key: '/Invented/A', slug: 'shared-blade', fragment: 'bladea' };
	vi.mocked(readKinds).mockResolvedValue(registry as EntityKindDefinition[]);
	vi.mocked(readGraph).mockResolvedValue({
		entities: [{ entity }, { entity: { ...entity, key: '/Invented/B', fragment: 'bladeb' } }]
	} as unknown as EntityGraph);
	expect(await entries()).toEqual([{ slug: 'shared-blade' }]);
});

it('loads every grouped version for SSR without repeating graph relations', async () => {
	const root = {
		...fixture,
		versionId: 'bladea',
		versionLabel: '24 Damage',
		versionName: '24 Damage',
		versions: []
	} as ItemPage;
	const second = { ...root, key: '/Invented/B', versionId: 'bladeb' };
	const grouped = { ...root, versions: [second] };
	vi.mocked(readIndex).mockResolvedValue({ items: [root, second] } as unknown as ItemIndex);
	vi.mocked(readItem).mockResolvedValue(grouped);
	const data = await load({ params: { slug: root.slug } });
	expect(readItem).toHaveBeenCalledWith(root.slug);
	expect(data.item.versionId).toBe('bladea');
	expect(data.item.versions).toEqual([{ ...second, relations: [] }]);
	expect(data.item.relations).toEqual([]);
});
