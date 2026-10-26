import { error } from '@sveltejs/kit';
import { readIndex, readItem } from '#lib/server/data.js';
export async function entries() {
	return (await readIndex()).items.map((item) => ({ slug: item.slug }));
}
export async function load({ params }: { params: { slug: string } }) {
	if (!(await readIndex()).items.some((item) => item.slug === params.slug))
		error(404, 'Item not found');
	return { item: await readItem(params.slug) };
}
