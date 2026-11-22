import { readDisciplines, readIndex, readItem, readSkills } from '#lib/server/data.js';
import { homeData } from '#lib/items/home.js';
import { homeSkills } from '#lib/skills/home.js';
export async function load() {
	const index = await readIndex();
	const pages = await Promise.all([...new Set(index.items.map((item) => item.slug))].map(readItem));
	return {
		...homeData(
			index,
			pages.flatMap((item) => [item, ...item.versions])
		),
		skills: homeSkills(await readDisciplines(), await readSkills())
	};
}
