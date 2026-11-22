import { error } from '@sveltejs/kit';
import { readDiscipline, readDisciplines, readSkill } from '#lib/server/data.js';
import type { Fact } from '#lib/site-data.js';

export async function entries() {
	return (await readDisciplines()).disciplines.map((discipline) => ({ slug: discipline.slug }));
}

export async function load({ params }: { params: { slug: string } }) {
	const index = await readDisciplines();
	if (!index.disciplines.some((discipline) => discipline.slug === params.slug))
		error(404, 'Discipline not found');
	// The page marks every relation on the entries that show it, so the publication gate finds each one.
	const discipline = await readDiscipline(params.slug);
	// The skill table shows each skill's cooldown and cost, which its own page holds.
	const skillFacts: Record<string, Fact[]> = {};
	for (const row of discipline.skillRows)
		for (const slot of row.slots)
			if (slot.skill.key && slot.skill.slug)
				skillFacts[slot.skill.key] = (await readSkill(slot.skill.slug)).facts;
	return { discipline, skillFacts };
}
