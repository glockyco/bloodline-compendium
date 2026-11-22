import { error } from '@sveltejs/kit';
import { readDiscipline, readDisciplines } from '#lib/server/data.js';

export async function entries() {
	return (await readDisciplines()).disciplines.map((discipline) => ({ slug: discipline.slug }));
}

export async function load({ params }: { params: { slug: string } }) {
	const index = await readDisciplines();
	if (!index.disciplines.some((discipline) => discipline.slug === params.slug))
		error(404, 'Discipline not found');
	// The page marks every relation on the entries that show it, so the publication gate finds each one.
	return { discipline: await readDiscipline(params.slug) };
}
