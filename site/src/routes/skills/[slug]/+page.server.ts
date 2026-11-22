import { readSkill, readSkills } from '#lib/server/data.js';
import { entityRoute } from '#lib/server/entity-route.js';

export const { entries, load } = entityRoute({
	name: 'Skill',
	index: readSkills,
	slugs: (index) => index.skills.map((skill) => skill.slug),
	load: async (slug, index) => {
		const row = index.skills.find((skill) => skill.slug === slug)!;
		// The heading links the skill's category in the list, as an item's heading links its own.
		const category = index.categories.find((category) => category.slug === row.category)!;
		// The page renders its sources, so the relation list stays out of its data.
		return {
			skill: { ...(await readSkill(slug)), relations: [] },
			category: { slug: category.slug, name: category.name }
		};
	}
});
