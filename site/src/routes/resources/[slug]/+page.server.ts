import { readResource, readResources } from '#lib/server/data.js';
import { entityRoute } from '#lib/server/entity-route.js';

export const { entries, load } = entityRoute({
	name: 'Resource',
	index: readResources,
	slugs: (index) => index.resources.map((resource) => resource.slug),
	load: async (slug) => ({ resource: await readResource(slug) })
});
