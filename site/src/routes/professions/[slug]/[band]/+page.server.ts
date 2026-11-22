import { professionBandEntries, professionPage } from '#lib/server/professions.js';

export const entries = professionBandEntries;

export const load = ({ params }: { params: { slug: string; band: string } }) =>
	professionPage(params.slug, params.band);
