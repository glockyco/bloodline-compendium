import { readIndex } from '#lib/server/data.js';
export const prerender = true;
export async function GET() {
	const index = await readIndex();
	const paths = [
		'/',
		'/items/',
		'/coverage/',
		...index.items.map((item) => `/items/${encodeURIComponent(item.slug)}/`)
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
