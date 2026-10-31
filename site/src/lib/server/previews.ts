import { readItem } from './data.js';
import { entries as itemEntries } from '../../routes/items/[slug]/+page.server.js';
import { firstRoute } from '../acquisition.js';
import { sourceHeadings } from '../labels.js';
import type { Preview } from '../previews.js';
import type { SourceKind } from '../site-data.js';

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
			const item = await readItem(slug);
			return {
				kind: 'item',
				name: item.name + (item.qualifier ? ` (${item.qualifier})` : ''),
				icon: item.icon,
				description: item.description,
				facts: item.facts,
				issues: item.notes.map((note) => note.title),
				route:
					firstRoute(item.sources, Object.keys(sourceHeadings) as SourceKind[]) ??
					(item.status === 'notInNormalPlay'
						? 'Not obtainable in normal play.'
						: 'No known source.')
			};
		}
	}
};
