import { afterEach, expect, it, vi } from 'vitest';
import { loadPreview, previewPath, type ItemPreview } from './previews.js';
import { itemPreview } from './server/previews.js';
import fixture from '../../fixtures/site-data/items/lanternedge.json';
import type { ItemPage } from './site-data.js';

afterEach(() => vi.unstubAllGlobals());

const preview = (versionId: string | null, icon: string): ItemPreview => ({
	kind: 'item',
	versionId,
	versions: [],
	name: 'Shared Blade',
	icon,
	description: null,
	facts: [],
	route: null
});

it('puts the encoded version fragment on a preview address without changing its resource', () => {
	expect(previewPath('item', 'shared blade', 'blade a')).toBe(
		'/tooltips/item/shared%20blade.json#blade%20a'
	);
	expect(previewPath('item', 'shared blade', null)).toBe('/tooltips/item/shared%20blade.json');
});

it('previews the exact linked version while fetching the grouped resource once', async () => {
	const root = { ...preview('bladea', 'first'), versions: [preview('bladeb', 'second')] };
	const fetcher = vi.fn().mockResolvedValue({ ok: true, json: async () => root });
	vi.stubGlobal('fetch', fetcher);
	const path = previewPath('item', 'preview-shared-blade', null);
	expect((await loadPreview(path + '#bladeb'))?.icon).toBe('second');
	expect((await loadPreview(path + '#bladea'))?.icon).toBe('first');
	expect((await loadPreview(path))?.name).toBe('Shared Blade');
	expect(fetcher).toHaveBeenCalledExactlyOnceWith(path);
});

it('uses the root preview for an unknown fragment', async () => {
	const root = preview(null, 'ordinary');
	vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => root }));
	expect(await loadPreview(previewPath('item', 'ordinary-preview', 'missing'))).toEqual(root);
});

it("publishes plain names and each version's own tooltip in one preview", () => {
	const base = {
		...fixture,
		versionId: 'bladea',
		versionLabel: '24 Damage',
		versionName: '24 Damage',
		versions: []
	} as ItemPage;
	const second = {
		...base,
		versionId: 'bladeb',
		icon: 'second-icon',
		versionLabel: '12 Damage',
		versionName: '12 Damage'
	};
	const result = itemPreview({ ...base, versions: [second] });
	expect(result.name).toBe(base.name);
	expect(result.versionId).toBe('bladea');
	expect(result.versions).toMatchObject([
		{ versionId: 'bladeb', name: base.name, icon: 'second-icon', versions: [] }
	]);
});
