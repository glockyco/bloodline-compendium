import { readGraph, readKinds } from '#lib/server/data.js';
import { professionBandEntries } from '#lib/server/professions.js';
import { entityPath, kindPath } from '#lib/entities.js';
import { SITE_ORIGIN } from '#lib/seo.js';
export const prerender = true;
export async function GET() {
	const [graph, kinds, bands] = await Promise.all([
		readGraph(),
		readKinds(),
		professionBandEntries()
	]);
	const paths = [
		'/',
		'/known-issues/',
		'/about/',
		...kinds.map((kind) => kindPath(kind.kind, kinds)).filter((path) => path !== null),
		...graph.entities
			.map(({ entity }) => entityPath({ ...entity, fragment: null }, kinds))
			.filter((path) => path !== null),
		// A long profession's later levels are pages of their own.
		...bands.map(({ slug, band }) => `${kindPath('profession', kinds)}${slug}/${band}/`)
	];
	return new Response(
		'<?xml version="1.0" encoding="UTF-8"?>\n' +
			'<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">' +
			[...new Set(paths)].map((path) => `<url><loc>${SITE_ORIGIN}${path}</loc></url>`).join('') +
			'</urlset>\n',
		{ headers: { 'content-type': 'application/xml; charset=utf-8' } }
	);
}
