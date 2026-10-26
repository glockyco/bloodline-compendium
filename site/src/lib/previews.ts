import type { Fact } from './site-data.js';

/** What an item link's preview shows: the item's tooltip. */
export interface ItemPreview {
	name: string;
	icon: string | null;
	description: string | null;
	facts: Fact[];
	/** The first way to get the item, or the reason none is known. */
	route: string | null;
}

const loaded = new Map<string, Promise<ItemPreview | null>>();

/** Loads an item's tooltip once per page, and null when it cannot be loaded. */
export function loadPreview(slug: string): Promise<ItemPreview | null> {
	let preview = loaded.get(slug);
	if (!preview) {
		preview = fetch(`/tooltips/${encodeURIComponent(slug)}.json`)
			.then((response) => (response.ok ? (response.json() as Promise<ItemPreview>) : null))
			.catch(() => null);
		loaded.set(slug, preview);
	}
	return preview;
}
