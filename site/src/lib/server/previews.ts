import { readItem, readSharedMerchantNames } from './data.js';
import { entries as itemEntries } from '../../routes/items/[slug]/+page.server.js';
import { firstRoute } from '../acquisition.js';
import { sourceHeadings } from '../labels.js';
import type { ItemPreview, Preview } from '../previews.js';
import type { ItemPage, SourceKind } from '../site-data.js';

/** For each kind with previews, the slugs that have one and how a slug's preview is built. */
export const previewSources: {
	[Kind in Preview['kind']]: {
		slugs(): Promise<string[]>;
		build(slug: string): Promise<Extract<Preview, { kind: Kind }>>;
	};
} = {
	item: {
		async slugs() {
			return (await itemEntries()).map((entry) => entry.slug);
		},
		async build(slug) {
			const [item, shared] = await Promise.all([readItem(slug), readSharedMerchantNames()]);
			return itemPreview(item, new Set(shared));
		}
	}
};

/** Each grouped preview retains the tooltip of every version at the name's one resource URL. */
export function itemPreview(item: ItemPage, shared: ReadonlySet<string> = new Set()): ItemPreview {
	return {
		kind: 'item',
		versionId: item.versionId ?? null,
		versions: item.versions.map((version) => itemPreview(version, shared)),
		name: item.name,
		icon: item.icon,
		description: item.description,
		facts: item.facts,
		projectileGroups: item.projectileGroups,
		onHitEffects: item.onHitEffects,
		route:
			firstRoute(item.sources, Object.keys(sourceHeadings) as SourceKind[], shared) ??
			(item.status === 'notInNormalPlay'
				? 'Cannot be obtained in normal play.'
				: 'No known source.')
	};
}
