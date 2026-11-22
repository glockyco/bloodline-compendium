import { error } from '@sveltejs/kit';
import { readMechanic, readMechanics } from '#lib/server/data.js';

export async function entries() {
	return (await readMechanics()).mechanics.map((mechanic) => ({ slug: mechanic.slug }));
}

export async function load({ params }: { params: { slug: string } }) {
	if (!(await readMechanics()).mechanics.some((mechanic) => mechanic.slug === params.slug))
		error(404, 'Mechanic not found');
	return { mechanic: await readMechanic(params.slug) };
}
