import { readDisciplines, readIndex, readKinds, readSkills } from '#lib/server/data.js';
import { homeActions, homeSections } from '#lib/entity/home.js';
import { itemHomeSection } from '#lib/items/home.js';
import { disciplineHomeSection } from '#lib/skills/home.js';
export async function load() {
	const [kinds, index, disciplines, skills] = await Promise.all([
		readKinds(),
		readIndex(),
		readDisciplines(),
		readSkills()
	]);
	return {
		actions: homeActions(kinds, ['item', 'skill']),
		sections: homeSections(kinds, {
			item: (kinds) => itemHomeSection(index, kinds),
			discipline: (kinds) => disciplineHomeSection(disciplines, skills, kinds)
		})
	};
}
