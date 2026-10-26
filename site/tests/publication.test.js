import { expect, it } from 'vitest';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { publicationGate } from '../../scripts/publication-gate.mjs';
import {
	publicationPage,
	publicationPaths,
	publicationSitemap,
	writePublication
} from './fixtures/publication.js';

async function publication(action) {
	const directory = await mkdtemp(join(tmpdir(), 'bloodline-publication-'));
	try {
		await writePublication(directory);
		await action(directory);
	} finally {
		await rm(directory, { recursive: true, force: true });
	}
}

it('resolves rendered disclosures, relative URLs, fragments, srcsets and social images', async () => {
	await publication(async (directory) => {
		const file = join(directory, 'items/other/index.html');
		await writeFile(
			file,
			publicationPage(
				'Invented pebble',
				'<a href="../invented/?review=1">Blade</a><a href="https://example.invalid/">External</a><a href="mailto:invented@example.invalid">Email</a>'
			)
		);
		expect(await publicationGate(directory)).toMatchObject({ pages: 7, sitemapEntries: 6 });
	});
});

it.each([
	['item link', 'items/index.html', '<a href="/items/missing/">Missing</a>', 'missing target'],
	[
		'coverage link',
		'items/other/index.html',
		'<a href="/coverage/missing/">Gaps</a>',
		'missing target'
	],
	[
		'coverage backlink',
		'coverage/index.html',
		'<details><summary>Gaps</summary><a href="/items/missing/">Missing</a></details>',
		'missing target'
	],
	[
		'coverage fragment',
		'items/other/index.html',
		'<a href="/coverage/#missing">Gap</a>',
		'missing fragment'
	],
	[
		'same-page fragment',
		'items/other/index.html',
		'<a href="#missing">Gap</a>',
		'missing fragment'
	],
	['image', 'items/other/index.html', '<img src="/icons/missing.webp" alt="">', 'missing target'],
	[
		'srcset variant',
		'items/other/index.html',
		'<img src="/icons/invented-48.webp" srcset="/icons/invented-48.webp 48w, /icons/missing.webp 160w" alt="">',
		'missing target'
	],
	[
		'encoded link',
		'coverage/index.html',
		'<a href="/items/%6dissing/">Missing</a>',
		'missing target'
	]
])('rejects a broken %s anywhere in rendered HTML', async (_, file, body, error) => {
	await publication(async (directory) => {
		await writeFile(join(directory, file), publicationPage('Changed invented page', body));
		await expect(publicationGate(directory)).rejects.toThrow(error);
	});
});

it.each([
	'favicon-32x32.png',
	'og-default.png',
	'icons/invented-48.webp',
	'icons/invented-160.webp'
])('rejects missing and empty referenced asset %s', async (asset) => {
	await publication(async (directory) => {
		await rm(join(directory, asset));
		await expect(publicationGate(directory)).rejects.toThrow(asset);
		await writeFile(join(directory, asset), '');
		await expect(publicationGate(directory)).rejects.toThrow('empty target');
	});
});

it.each([
	['title', publicationPage(' Invented   BLADE ')],
	['title', publicationPage('Invented &#98;lade')],
	['description', publicationPage('Different item', '', 'Invented blade invented description.')],
	['nonempty title', publicationPage(' ')],
	['nonempty description', publicationPage('Different item', '', ' ')],
	[
		'one nonempty title',
		publicationPage('Different item').replace('</title>', '</title><title>Extra</title>')
	],
	[
		'one nonempty description',
		publicationPage('Different item').replace(
			'</head>',
			'<meta name="description" content="Extra"></head>'
		)
	]
])('rejects nonunique or absent %s metadata', async (error, html) => {
	await publication(async (directory) => {
		await writeFile(join(directory, 'items/other/index.html'), html);
		await expect(publicationGate(directory)).rejects.toThrow(error);
	});
});

it.each([
	[
		'missing item',
		publicationSitemap(publicationPaths.filter((path) => path !== '/items/other/')),
		'Sitemap is missing /items/other/'
	],
	[
		'missing about',
		publicationSitemap(publicationPaths.filter((path) => path !== '/about/')),
		'Sitemap is missing /about/'
	],
	['missing route', publicationSitemap([...publicationPaths, '/items/missing/']), 'missing target'],
	[
		'duplicate route',
		publicationSitemap([...publicationPaths, '/items/other/']),
		'Duplicate sitemap'
	],
	['404 page', publicationSitemap([...publicationPaths, '/404.html']), 'Invalid sitemap'],
	['fragment', publicationSitemap([...publicationPaths, '/coverage/#unknown']), 'Invalid sitemap'],
	['query', publicationSitemap([...publicationPaths, '/items/?name=blade']), 'Invalid sitemap'],
	[
		'external origin',
		publicationSitemap().replace(
			'https://bloodline.compendiums.org/items/other/',
			'https://example.invalid/items/other/'
		),
		'non-production origin'
	],
	['empty sitemap', '<urlset></urlset>', 'Sitemap is missing']
])('rejects a sitemap with a %s', async (_, sitemap, error) => {
	await publication(async (directory) => {
		await writeFile(join(directory, 'sitemap.xml'), sitemap);
		await expect(publicationGate(directory)).rejects.toThrow(error);
	});
});

it('keeps a noindex page out of the sitemap without failing the gate', async () => {
	await publication(async (directory) => {
		await mkdir(join(directory, 'design'));
		await writeFile(
			join(directory, 'design/index.html'),
			publicationPage('Invented fixtures', '', undefined, '<meta name="robots" content="noindex">')
		);
		expect((await publicationGate(directory)).sitemapEntries).toBe(publicationPaths.length);
		await writeFile(
			join(directory, 'sitemap.xml'),
			publicationSitemap([...publicationPaths, '/design/'])
		);
		await expect(publicationGate(directory)).rejects.toThrow('Invalid sitemap');
	});
});

it('decodes HTML entities before checking destinations and metadata', async () => {
	await publication(async (directory) => {
		const file = join(directory, 'items/other/index.html');
		const html = await readFile(file, 'utf8');
		await writeFile(file, html.replace('../invented/', '../invented/?one=1&amp;two=2'));
		expect((await publicationGate(directory)).references).toBeGreaterThan(0);
	});
});

async function writeRelationFixture(directory) {
	const source = {
		kind: 'item',
		key: '/Invented/Blade',
		name: 'Blade',
		slug: 'invented',
		icon: null
	};
	const target = {
		kind: 'item',
		key: '/Invented/Pebble',
		name: 'Pebble',
		slug: 'other',
		icon: null
	};
	const row = {
		id: 'invented-input',
		kind: 'Input',
		source,
		target,
		forwardLabel: 'Used for',
		reverseLabel: 'Requires',
		conditions: [],
		attributes: null
	};
	const graph = {
		entities: [
			{
				entity: source,
				relations: [
					{
						id: row.id,
						kind: row.kind,
						direction: 'outgoing',
						entity: target,
						label: row.forwardLabel,
						conditions: []
					}
				]
			},
			{
				entity: target,
				relations: [
					{
						id: row.id,
						kind: row.kind,
						direction: 'incoming',
						entity: source,
						label: row.reverseLabel,
						conditions: []
					}
				]
			}
		],
		relations: [row]
	};
	await writeFile(join(directory, 'graph.json'), JSON.stringify(graph));
	for (const [slug, direction] of [
		['invented', 'outgoing'],
		['other', 'incoming']
	])
		await writeFile(
			join(directory, `items/${slug}/index.html`),
			publicationPage(
				slug,
				`<p data-relation-id="${row.id}" data-relation-direction="${direction}">Related item</p>`
			)
		);
	return graph;
}

it('requires both directions of item relations in static HTML', async () => {
	await publication(async (directory) => {
		await writeRelationFixture(directory);
		expect((await publicationGate(directory)).relations).toBe(1);
		await writeFile(
			join(directory, 'items/other/index.html'),
			publicationPage('Other', '<p>Relation omitted</p>')
		);
		await expect(publicationGate(directory)).rejects.toThrow(
			'missing static relation invented-input (incoming)'
		);
	});
});

it('rejects a relation planted only by JavaScript', async () => {
	await publication(async (directory) => {
		await writeRelationFixture(directory);
		await writeFile(
			join(directory, 'items/other/index.html'),
			publicationPage(
				'Other',
				`<script>document.body.insertAdjacentHTML('beforeend', '<p data-relation-id="invented-input" data-relation-direction="incoming">Related item</p>')</script>`
			)
		);
		await expect(publicationGate(directory)).rejects.toThrow('missing static relation');
	});
});

it.each([
	[
		'missing reference key',
		(graph) => {
			graph.relations[0].target = { ...graph.relations[0].target, key: '/Missing' };
		},
		'Missing reference key'
	],
	[
		'duplicate key',
		(graph) => {
			graph.entities.push(graph.entities[0]);
		},
		'Duplicate entity key'
	],
	[
		'duplicate slug',
		(graph) => {
			graph.entities[1].entity = { ...graph.entities[1].entity, slug: 'invented' };
		},
		'Duplicate entity slug'
	],
	[
		'missing reverse relation',
		(graph) => {
			graph.entities[1].relations = [];
		},
		'Missing or invalid incoming relation'
	],
	[
		'invalid slug decision',
		(graph) => {
			graph.entities[0].entity = { ...graph.entities[0].entity, slug: null };
			graph.relations[0].source = graph.entities[0].entity;
		},
		'Invalid slug decision'
	]
])('rejects graph integrity defect: %s', async (_, plant, error) => {
	await publication(async (directory) => {
		const graph = await writeRelationFixture(directory);
		plant(graph);
		await writeFile(join(directory, 'graph.json'), JSON.stringify(graph));
		await expect(publicationGate(directory)).rejects.toThrow(error);
	});
});

it('rejects a slug on a kind without pages', async () => {
	await publication(async (directory) => {
		const graph = await writeRelationFixture(directory);
		const source = { ...graph.entities[0].entity, kind: 'merchant' };
		graph.entities[0].entity = source;
		graph.relations[0].source = source;
		graph.entities[1].relations[0].entity = source;
		await writeFile(
			join(directory, 'kinds.json'),
			JSON.stringify([
				{
					kind: 'item',
					hasPages: true,
					route: 'items',
					searchFilter: 'Items',
					structuredDataType: 'Thing'
				},
				{
					kind: 'merchant',
					hasPages: false,
					route: null,
					searchFilter: 'Merchants',
					structuredDataType: 'Person'
				}
			])
		);
		await writeFile(join(directory, 'graph.json'), JSON.stringify(graph));
		await expect(publicationGate(directory)).rejects.toThrow('Invalid slug decision');
	});
});
