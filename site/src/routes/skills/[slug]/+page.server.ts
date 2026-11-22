import { error } from '@sveltejs/kit';
import { readSkill, readSkills } from '#lib/server/data.js';

export async function entries() {
	return (await readSkills()).skills.map((skill) => ({ slug: skill.slug }));
}

export async function load({ params }: { params: { slug: string } }) {
	const index = await readSkills();
	if (!index.skills.some((skill) => skill.slug === params.slug)) error(404, 'Skill not found');
	// The page renders its sources, so the relation list stays out of its data.
	return { skill: { ...(await readSkill(params.slug)), relations: [] } };
}
