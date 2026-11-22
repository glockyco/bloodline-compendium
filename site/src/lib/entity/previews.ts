import type { EntityKind, Fact, ItemPage } from '../site-data.js';

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

/** What a skill link's preview shows: the skill's tooltip and its first way to get it, per version as items do. */
export interface SkillPreview {
	kind: 'skill';
	versionId: string | null;
	versions: SkillPreview[];
	name: string;
	icon: string | null;
	description: string | null;
	facts: Fact[];
	route: string | null;
}

/** What a passive link's preview shows: the passive with its level and the cost of each rank. */
export interface PassivePreview {
	kind: 'passive';
	name: string;
	icon: string | null;
	description: string | null;
	/** The discipline level that unlocks it, such as "Pyromancy level 5". */
	unlocks: string;
	costs: number[];
}

/** What a spellsong link's preview shows: its tune and what playing it does. */
export interface SpellsongPreview {
	kind: 'spellsong';
	name: string;
	description: string | null;
	tune: number[];
	charges: number;
	experience: number;
	route: string | null;
}

/**
 * The previews of a hosted kind's entries on one host page, keyed by their anchor, which a link's fragment picks.
 */
export interface HostedPreviews<P extends PassivePreview | SpellsongPreview> {
	kind: P['kind'];
	entries: Record<string, P>;
}

/**
 * Every kind's preview. A kind gets previews by adding its type here, its kind to `previewKinds`, its tooltip to
 * `TooltipPresenter.svelte` and its data to `previewSources` in `server/previews.ts`.
 */
export type Preview = ItemPreview | SkillPreview | PassivePreview | SpellsongPreview;

/** A preview resource: one entity's preview, or the previews of a hosted kind's entries on one page. */
export type PreviewResource =
	Preview | HostedPreviews<PassivePreview> | HostedPreviews<SpellsongPreview>;

/** The kinds whose links show a preview. Links to other kinds lead straight to their page. */
export const previewKinds: ReadonlySet<EntityKind> = new Set<Preview['kind']>([
	'item',
	'skill',
	'passive',
	'spellsong'
]);

/** The address of a kind's preview data, which `routes/tooltips/[kind]/[slug].json` prerenders. */
export function previewPath(kind: EntityKind, slug: string, fragment: string | null): string {
	return `/tooltips/${kind}/${encodeURIComponent(slug)}.json${fragment ? '#' + encodeURIComponent(fragment) : ''}`;
}

const loaded = new Map<string, Promise<PreviewResource | null>>();

/** Loads a preview once per page, and null when it cannot be loaded. */
export async function loadPreview(path: string): Promise<Preview | null> {
	const [resource, fragment] = path.split('#');
	let preview = loaded.get(resource);
	if (!preview) {
		preview = fetch(resource)
			.then((response) => (response.ok ? (response.json() as Promise<PreviewResource>) : null))
			.catch(() => null);
		loaded.set(resource, preview);
	}
	const root = await preview;
	if (!root) return null;
	const key = fragment ? decodeURIComponent(fragment) : null;
	if ('entries' in root) return (key && root.entries[key]) || null;
	if (!('versions' in root) || !key) return root;
	return root.versionId === key
		? root
		: (root.versions.find((version) => version.versionId === key) ?? root);
}
