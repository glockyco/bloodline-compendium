import { readIndex } from '#lib/server/data.js';

export const prerender = true;

/** Real icons and one real item page, so the fixture page renders in fixture and game builds alike. */
export async function load() {
	const index = await readIndex();
	const icons = index.items.flatMap((item) => (item.icon ? [item.icon] : [])).slice(0, 5);
	const item = index.items[0];
	return {
		icons,
		item: { kind: item.kind, key: item.key, name: item.name, slug: item.slug, icon: item.icon }
	};
}
