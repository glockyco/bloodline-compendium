import { readProfessions } from '#lib/server/data.js';
import { professionPage } from '#lib/server/professions.js';

export async function entries() {
	return (await readProfessions()).professions.map((profession) => ({ slug: profession.slug }));
}

export const load = ({ params }: { params: { slug: string } }) => professionPage(params.slug, null);
