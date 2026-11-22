import { error } from '@sveltejs/kit';
import { readSkill, readSkills } from '#lib/server/data.js';

export async function entries() {
	return (await readSkills()).skills.map((skill) => ({ slug: skill.slug }));
}

export async function load({ params }: { params: { slug: string } }) {
	const index = await readSkills();
	const row = index.skills.find((skill) => skill.slug === params.slug);
	if (!row) error(404, 'Skill not found');
	// The heading links the skill's category in the list, as an item's heading links its own.
	const category = index.categories.find((category) => category.slug === row.category)!;
	// The page renders its sources, so the relation list stays out of its data.
	return {
		skill: { ...(await readSkill(params.slug)), relations: [] },
		category: { slug: category.slug, name: category.name }
	};
}
