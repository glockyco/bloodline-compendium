import { readIndex, readItem } from '#lib/server/data.js';
import { homeData } from '#lib/home.js';
export async function load() {
	const index = await readIndex();
	const pages = await Promise.all([...new Set(index.items.map((item) => item.slug))].map(readItem));
	return homeData(
		index,
		pages.flatMap((item) => [item, ...item.versions])
	);
}
