import type { ResolvedPathname } from '$app/types';
import type { EntityKind, EntityKindDefinition, EntityReference } from './site-data.js';

// The site publishes at the origin root. Registry paths serve navigation and metadata directly.
export function kindPath(
	kind: EntityKind,
	kinds: EntityKindDefinition[],
	suffix = ''
): ResolvedPathname | null {
	const definition = kinds.find((entry) => entry.kind === kind);
	if (!definition?.hasPages || !definition.route) return null;
	const route = definition.route.replace(/^\/+|\/+$/g, '');
	if (!/^[a-z][a-z0-9-]*(?:\/[a-z][a-z0-9-]*)*$/.test(route))
		throw new Error('Invalid entity route: ' + definition.route);
	// Origin-root publication and the validated registry route establish this resolved path boundary.
	return ('/' + route + '/' + suffix) as ResolvedPathname;
}

/** Every kind with pages, in the registry's order, with its list's label and address, as navigation lists them. */
export function browseKinds(
	kinds: EntityKindDefinition[]
): { kind: EntityKind; label: string; path: ResolvedPathname }[] {
	return kinds
		.filter((kind) => kind.hasPages)
		.map((kind) => ({
			kind: kind.kind,
			label: kind.searchFilter,
			path: kindPath(kind.kind, kinds)!
		}));
}

/** A kind's name for one entity, such as "Item" or "Calendar Event", from its ID. */
export function kindLabel(kind: EntityKind): string {
	return kind.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, (letter) => letter.toUpperCase());
}

/**
 * A count of entities in their kind's words, such as "1 item" or "3 disciplines", from the kind's ID and its list
 * label. Entities of several kinds count as pages.
 */
export function kindCount(
	entities: { kind: EntityKind }[],
	kinds: EntityKindDefinition[]
): { count: number; noun: string; many: string } {
	const shared = new Set(entities.map((entity) => entity.kind));
	const kind = shared.size === 1 ? kinds.find((entry) => entry.kind === [...shared][0]) : null;
	const many = kind ? kind.searchFilter.toLocaleLowerCase('en') : 'pages';
	const one = kind ? kindLabel(kind.kind).toLocaleLowerCase('en') : 'page';
	return { count: entities.length, noun: entities.length === 1 ? one : many, many };
}

/**
 * The page of an entity. An entity of a hosted kind, such as a passive, has no page of its own and leads to its
 * host's page at its anchor.
 */
export function entityPath(
	entity: EntityReference,
	kinds: EntityKindDefinition[]
): ResolvedPathname | null {
	if (entity.key && entity.host && entity.fragment) {
		const host = entityPath({ ...entity.host, fragment: null }, kinds);
		return host && ((host + '#' + encodeURIComponent(entity.fragment)) as ResolvedPathname);
	}
	if (!entity.key || !entity.slug) return null;
	return kindPath(
		entity.kind,
		kinds,
		encodeURIComponent(entity.slug) +
			'/' +
			(entity.fragment ? '#' + encodeURIComponent(entity.fragment) : '')
	);
}
