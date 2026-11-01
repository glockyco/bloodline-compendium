import type { EntityKind, Fact, ItemPage } from './site-data.js';

/** What an item link's preview shows: the item's tooltip. */
export interface ItemPreview {
	kind: 'item';
	versionId: string | null;
	versions: ItemPreview[];
	name: string;
	icon: string | null;
	description: string | null;
	facts: Fact[];
	projectileGroups?: ItemPage['projectileGroups'];
	onHitEffects?: ItemPage['onHitEffects'];
	/** The first way to get the item, or the reason none is known. */
	route: string | null;
}

/**
 * Every kind's preview. A kind gets previews by adding its type here, its kind to `previewKinds`, its tooltip to
 * `TooltipPresenter.svelte` and its data to `previewSources` in `server/previews.ts`.
 */
export type Preview = ItemPreview;

/** The kinds whose links show a preview. Links to other kinds lead straight to their page. */
export const previewKinds: ReadonlySet<EntityKind> = new Set<Preview['kind']>(['item']);

/** The address of a kind's preview data, which `routes/tooltips/[kind]/[slug].json` prerenders. */
export function previewPath(kind: EntityKind, slug: string, fragment: string | null): string {
	return `/tooltips/${kind}/${encodeURIComponent(slug)}.json${fragment ? '#' + encodeURIComponent(fragment) : ''}`;
}

const loaded = new Map<string, Promise<Preview | null>>();

/** Loads a preview once per page, and null when it cannot be loaded. */
export async function loadPreview(path: string): Promise<Preview | null> {
	const [resource, fragment] = path.split('#');
	let preview = loaded.get(resource);
	if (!preview) {
		preview = fetch(resource)
			.then((response) => (response.ok ? (response.json() as Promise<Preview>) : null))
			.catch(() => null);
		loaded.set(resource, preview);
	}
	const root = await preview;
	if (!root || !fragment) return root;
	const versionId = decodeURIComponent(fragment);
	return root.versionId === versionId
		? root
		: (root.versions.find((version) => version.versionId === versionId) ?? root);
}
