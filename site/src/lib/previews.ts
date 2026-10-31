import type { EntityKind, Fact } from './site-data.js';

/** What an item link's preview shows: the item's tooltip. */
export interface ItemPreview {
	kind: 'item';
	name: string;
	icon: string | null;
	description: string | null;
	facts: Fact[];
	/** The first way to get the item, or the reason none is known. */
	route: string | null;
	/** What each known game issue does to this item, which changes what its numbers mean. */
	issues: string[];
}

/**
 * Every kind's preview. A kind gets previews by adding its type here, its kind to `previewKinds`, its tooltip to
 * `TooltipPresenter.svelte` and its data to `previewSources` in `server/previews.ts`.
 */
export type Preview = ItemPreview;

/** The kinds whose links show a preview. Links to other kinds lead straight to their page. */
export const previewKinds: ReadonlySet<EntityKind> = new Set<Preview['kind']>(['item']);

/** The address of a kind's preview data, which `routes/tooltips/[kind]/[slug].json` prerenders. */
export function previewPath(kind: EntityKind, slug: string): string {
	return `/tooltips/${kind}/${encodeURIComponent(slug)}.json`;
}

const loaded = new Map<string, Promise<Preview | null>>();

/** Loads a preview once per page, and null when it cannot be loaded. */
export function loadPreview(path: string): Promise<Preview | null> {
	let preview = loaded.get(path);
	if (!preview) {
		preview = fetch(path)
			.then((response) => (response.ok ? (response.json() as Promise<Preview>) : null))
			.catch(() => null);
		loaded.set(path, preview);
	}
	return preview;
}
