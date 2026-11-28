import type { EntityKind, EntityKindDefinition, Fact, ItemPage } from '../site-data.js';

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

/** What a discipline link's preview shows: its category and what it teaches. */
export interface DisciplinePreview {
	kind: 'discipline';
	name: string;
	category: string;
	/** Its skills, passives and spellsongs counted, such as "31 skills and 13 passives", or null when it has none. */
	contents: string | null;
}

/**
 * What a resource, profession, mechanic or creature link's preview shows: the facts its list shows, or a mechanic's
 * introduction, as lines under its name.
 */
export interface SummaryPreview<Kind extends SummaryKind = SummaryKind> {
	kind: Kind;
	name: string;
	icon: string | null;
	lines: string[];
}

type SummaryKind = 'resource' | 'profession' | 'mechanic' | 'creature';

/**
 * The previews of a hosted kind's entries on one host page, keyed by their anchor, which a link's fragment picks.
 */
export interface HostedPreviews<P extends PassivePreview | SpellsongPreview> {
	kind: P['kind'];
	entries: Record<string, P>;
}

/**
 * Every kind's preview. A kind gets previews by adding its type here, its kind to `previewKinds`, its tooltip to
 * `TooltipPresenter.svelte` and its data to `previewSources` in `server/previews.ts`. Publication fails for a kind
 * with pages that `previewKinds` lacks.
 */
export type Preview =
	| ItemPreview
	| SkillPreview
	| PassivePreview
	| SpellsongPreview
	| DisciplinePreview
	| SummaryPreview<'resource'>
	| SummaryPreview<'profession'>
	| SummaryPreview<'mechanic'>
	| SummaryPreview<'creature'>;

/** A preview resource: one entity's preview, or the previews of a hosted kind's entries on one page. */
export type PreviewResource =
	Preview | HostedPreviews<PassivePreview> | HostedPreviews<SpellsongPreview>;

/** The kinds whose links show a preview. Links to other kinds lead straight to their page. */
export const previewKinds: ReadonlySet<EntityKind> = new Set<Preview['kind']>([
	'item',
	'skill',
	'passive',
	'spellsong',
	'discipline',
	'resource',
	'creature',
	'profession',
	'mechanic'
]);

/** The address of a kind's preview data, which `routes/tooltips/[kind]/[slug].json` prerenders. */
export function previewPath(kind: EntityKind, slug: string, fragment: string | null): string {
	return `/tooltips/${kind}/${encodeURIComponent(slug)}.json${fragment ? '#' + encodeURIComponent(fragment) : ''}`;
}

/**
 * The preview address of a link marked `data-preview`. A hosted entry's link, such as a passive's, holds its address,
 * because its page's address names the host. Any other link leaves the attribute empty, because its page's address
 * names the kind by its route and the entity by its slug, which keeps long tables small.
 */
export function linkPreviewPath(
	link: HTMLAnchorElement,
	kinds: EntityKindDefinition[]
): string | null {
	if (link.dataset.preview) return link.dataset.preview;
	const url = new URL(link.href, window.location.href);
	const [route, slug] = url.pathname.split('/').filter(Boolean);
	const kind = kinds.find(
		(entry) => entry.hasPages && entry.route?.replace(/^\/+|\/+$/g, '') === route
	);
	if (!kind || !slug) return null;
	return previewPath(
		kind.kind,
		decodeURIComponent(slug),
		url.hash ? decodeURIComponent(url.hash.slice(1)) : null
	);
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
