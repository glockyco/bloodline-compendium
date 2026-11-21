import { error } from '@sveltejs/kit';
import {
	readGraph,
	readIndex,
	readKinds,
	readItem,
	readSharedMerchantNames
} from '#lib/server/data.js';
export async function entries() {
	const [graph, kinds] = await Promise.all([readGraph(), readKinds()]);
	const kind = kinds.find((entry) => entry.kind === 'item');
	return kind?.hasPages
		? [
				...new Set(
					graph.entities
						.filter(({ entity }) => entity.kind === kind.kind && entity.slug)
						.map(({ entity }) => entity.slug!)
				)
			].map((slug) => ({ slug }))
		: [];
}
export async function load({ params }: { params: { slug: string } }) {
	const index = await readIndex();
	if (!index.items.some((item) => item.slug === params.slug)) error(404, 'Item not found');
	// The page renders every relation from its sources and uses, so the relation list stays out of its data.
	const item = await readItem(params.slug);
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
