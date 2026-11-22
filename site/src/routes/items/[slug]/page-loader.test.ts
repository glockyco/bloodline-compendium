import { expect, it, vi } from 'vitest';
import { entries, load } from './+page.server.js';
import { readIndex, readItem, readSharedMerchantNames } from '#lib/server/data.js';
import fixture from '../../../../fixtures/site-data/items/lanternedge.json';
import type { ItemIndex, ItemPage } from '#lib/site-data.js';

vi.mock('#lib/server/data.js', () => ({
	readIndex: vi.fn(),
	readItem: vi.fn(),
	readSharedMerchantNames: vi.fn()
}));

it('prerenders one page per plain slug even when distinct items share it', async () => {
	const item = { kind: 'item', key: '/Invented/A', slug: 'shared-blade', versionId: 'bladea' };
	vi.mocked(readIndex).mockResolvedValue({
		items: [item, { ...item, key: '/Invented/B', versionId: 'bladeb' }]
	} as unknown as ItemIndex);
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
	vi.mocked(readSharedMerchantNames).mockResolvedValue([]);
	const data = await load({ params: { slug: root.slug } });
	expect(readItem).toHaveBeenCalledWith(root.slug);
	expect(data.item.versionId).toBe('bladea');
	expect(data.item.versions).toEqual([{ ...second, relations: [] }]);
	expect(data.item.relations).toEqual([]);
});
