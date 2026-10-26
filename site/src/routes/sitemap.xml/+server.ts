import { readGraph, readKinds } from '#lib/server/data.js';
import { entityPath, kindPath } from '#lib/entities.js';
export const prerender = true;
export async function GET() {
	const [graph, kinds] = await Promise.all([readGraph(), readKinds()]);
	const paths = [
		'/',
		'/coverage/',
		'/known-issues/',
		'/about/',
		...kinds.map((kind) => kindPath(kind.kind, kinds)).filter((path) => path !== null),
		...graph.entities.map(({ entity }) => entityPath(entity, kinds)).filter((path) => path !== null)
	];
	return new Response(
		'<?xml version="1.0" encoding="UTF-8"?>\n' +
			'<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">' +
			paths
				.map((path) => `<url><loc>https://bloodline.compendiums.org${path}</loc></url>`)
				.join('') +
			'</urlset>\n',
		{ headers: { 'content-type': 'application/xml; charset=utf-8' } }
	);
}
