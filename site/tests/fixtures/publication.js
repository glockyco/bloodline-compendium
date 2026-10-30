import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';

export const publicationOrigin = 'https://the-bloodline.compendiums.org';
export const publicationPaths = [
	'/',
	'/items/',
	'/items/invented/',
	'/items/other/',
	'/known-issues/',
	'/about/'
];

export function publicationPage(
	title,
	body = '',
	description = title + ' invented description.',
	head = ''
) {
	return `<!doctype html><html lang="en"><head><title>${title}</title>
		<meta name="description" content="${description}">${head}
		<link rel="icon" href="/favicon-32x32.png">
		<meta property="og:image" content="${publicationOrigin}/og-default.png">
		</head><body><h1>${title}</h1>${body}</body></html>`;
}

/** An item page, whose article carries the search body and filter that every entity page needs. */
export function publicationItemPage(title, body = '', description, head) {
	return publicationPage(
		title,
		`<article data-pagefind-body data-pagefind-filter="kind:Items">${body}</article>`,
		description,
		head
	);
}

export function publicationSitemap(paths = publicationPaths) {
	return `<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">${paths
		.map((path) => `<url><loc>${publicationOrigin}${path}</loc></url>`)
		.join('')}</urlset>`;
}

export async function writePublication(directory) {
	const contents = {
		'kinds.json': JSON.stringify([
			{
				kind: 'item',
				hasPages: true,
				route: 'items',
				searchFilter: 'Items',
				structuredDataType: 'Thing'
			}
		]),
		'graph.json': JSON.stringify({ entities: [], relations: [] }),
		'index.html': publicationPage('Invented home', '<a href="/items/">Items</a>'),
		'items/index.html': publicationPage(
			'Invented items',
			'<a href="/items/invented/">First item</a><a href="/items/other/">Second item</a>'
		),
		'items/invented/index.html': publicationItemPage(
			'Invented blade',
			'<img alt="" src="/icons/invented-160.webp" srcset="/icons/invented-48.webp 48w, /icons/invented-160.webp 160w"><a href="/known-issues/#unknown">Known issue</a>'
		),
		'items/other/index.html': publicationItemPage(
			'Invented pebble',
			'<a href="../invented/">Blade</a>'
		),
		'known-issues/index.html': publicationPage(
			'Invented issues',
			'<section id="unknown"><details><summary>Affected items</summary><a href="/items/other/">Pebble</a></details></section>'
		),
		'about/index.html': publicationPage('Invented about'),
		'404.html': publicationPage(
			'Invented not found',
			'',
			undefined,
			'<meta name="robots" content="noindex">'
		),
		'sitemap.xml': publicationSitemap(),
		'favicon-32x32.png': 'invented favicon',
		'og-default.png': 'invented image',
		'icons/invented-48.webp': 'invented small icon',
		'icons/invented-160.webp': 'invented large icon'
	};
	for (const [file, content] of Object.entries(contents)) {
		await mkdir(dirname(join(directory, file)), { recursive: true });
		await writeFile(join(directory, file), content);
	}
}
