import { readIndex, readItem, readSharedMerchantNames } from '#lib/server/data.js';
import { entityRoute } from '#lib/server/entity-route.js';

export const { entries, load } = entityRoute({
	name: 'Item',
	index: readIndex,
	slugs: (index) => index.items.map((item) => item.slug),
	load: async (slug) => {
		// The page renders every relation from its sources and uses, so the relation list stays out of its data.
		const item = await readItem(slug);
		const shared = await readSharedMerchantNames();
		const names = new Set(
			[item, ...item.versions].flatMap((version) => version.sources.map((source) => source.name))
		);
		return {
			item: {
				...item,
				relations: [],
				versions: item.versions.map((version) => ({ ...version, relations: [] }))
			},
			// Only the names this page's sources use, so the page data stays small.
			sharedMerchantNames: shared.filter((name) => names.has(name))
		};
	}
});
