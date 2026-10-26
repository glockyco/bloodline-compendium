import { error, json } from '@sveltejs/kit';
import { readItem } from '#lib/server/data.js';
import { entries as itemEntries } from '../../items/[slug]/+page.server.js';
import type { ItemPreview } from '#lib/previews.js';
import { firstRoute } from '#lib/acquisition.js';
import { sourceHeadings } from '#lib/labels.js';
import type { SourceKind } from '#lib/site-data.js';

// Item links load these on demand to show an item's tooltip before the reader follows the link.
export const prerender = true;
export const entries = itemEntries;
export async function GET({ params }: { params: { slug: string } }) {
	if (!(await itemEntries()).some((item) => item.slug === params.slug))
		error(404, 'Item not found');
	const item = await readItem(params.slug);
	const preview: ItemPreview = {
		name: item.name + (item.qualifier ? ` (${item.qualifier})` : ''),
		icon: item.icon,
		description: item.description,
		facts: item.facts,
		route:
			firstRoute(item.sources, Object.keys(sourceHeadings) as SourceKind[]) ??
			(item.status === 'notInNormalPlay'
				? 'Not obtainable in normal play.'
				: 'No known way to get it.')
	};
	return json(preview);
}
