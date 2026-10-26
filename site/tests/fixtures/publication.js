import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';

export const publicationOrigin = 'https://bloodline.compendiums.org';
export const publicationPaths = [
	'/',
	'/items/',
	'/items/invented/',
	'/items/other/',
	'/coverage/',
	'/about/'
];

export function publicationPage(title, body = '', description = title + ' invented description.') {
	return `<!doctype html><html lang="en"><head><title>${title}</title>
		<meta name="description" content="${description}">
		<link rel="icon" href="/favicon.svg">
		<meta property="og:image" content="${publicationOrigin}/og-default.png">
		</head><body><h1>${title}</h1>${body}</body></html>`;
}

export function publicationSitemap(paths = publicationPaths) {
	return `<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">${paths
		.map((path) => `<url><loc>${publicationOrigin}${path}</loc></url>`)
		.join('')}</urlset>`;
}

export async function writePublication(directory) {
	const contents = {
		'index.html': publicationPage('Invented home', '<a href="/items/">Items</a>'),
		'items/index.html': publicationPage(
			'Invented items',
			'<a href="/items/invented/">First item</a><a href="/items/other/">Second item</a>'
		),
		'items/invented/index.html': publicationPage(
			'Invented blade',
			'<img alt="" src="/icons/invented-160.webp" srcset="/icons/invented-48.webp 48w, /icons/invented-160.webp 160w"><a href="/coverage/#unknown">Coverage</a>'
		),
		'items/other/index.html': publicationPage(
			'Invented pebble',
			'<a href="../invented/">Blade</a>'
		),
		'coverage/index.html': publicationPage(
			'Invented coverage',
			'<section id="unknown"><details><summary>Gaps</summary><a href="/items/other/">Pebble</a></details></section>'
		),
		'about/index.html': publicationPage('Invented about'),
		'404.html': publicationPage('Invented not found'),
		'sitemap.xml': publicationSitemap(),
		'favicon.svg': '<svg xmlns="http://www.w3.org/2000/svg"></svg>',
		'og-default.png': 'invented image',
		'icons/invented-48.webp': 'invented small icon',
		'icons/invented-160.webp': 'invented large icon'
	};
	for (const [file, content] of Object.entries(contents)) {
		await mkdir(dirname(join(directory, file)), { recursive: true });
		await writeFile(join(directory, file), content);
	}
}
