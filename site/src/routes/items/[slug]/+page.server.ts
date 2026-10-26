import { error } from '@sveltejs/kit';
import { readGraph, readKinds, readItem } from '#lib/server/data.js';
export async function entries() {
	const [graph, kinds] = await Promise.all([readGraph(), readKinds()]);
	const kind = kinds.find((entry) => entry.kind === 'item');
	return kind?.hasPages
		? graph.entities
				.filter(({ entity }) => entity.kind === kind.kind && entity.slug)
				.map(({ entity }) => ({ slug: entity.slug! }))
		: [];
}
export async function load({ params }: { params: { slug: string } }) {
	if (!(await entries()).some((item) => item.slug === params.slug)) error(404, 'Item not found');
	return { item: await readItem(params.slug) };
}
