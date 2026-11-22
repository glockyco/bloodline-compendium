import { readDisciplines, readIndex, readSkills } from '#lib/server/data.js';
import { homeData } from '#lib/items/home.js';
import { homeSkills } from '#lib/skills/home.js';
export async function load() {
	return {
		...homeData(await readIndex()),
		skills: homeSkills(await readDisciplines(), await readSkills())
	};
}
