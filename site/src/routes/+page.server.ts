import { readIndex, readItem } from '#lib/server/data.js';
import { homeData } from '#lib/home.js';
export async function load() {
	const index = await readIndex();
	const pages = await Promise.all(index.items.map((item) => readItem(item.slug)));
	return homeData(index, pages);
}
