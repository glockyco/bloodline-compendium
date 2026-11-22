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
