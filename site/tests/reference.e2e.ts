import { expect, test } from '@playwright/test';
test('filters items with counted facets, preserves URL state and sorts numeric values', async ({
	page
}) => {
	await page.goto('/items/');
	await expect(page.locator('.item-name[href="/items/hollow-prism/"]')).toHaveCount(0);
	const categories = page.getByRole('complementary', { name: 'Item filters' });
	const weapons = categories.getByRole('checkbox', { name: /^Weapons\s/ });
	await weapons.check();
	await expect(page.locator('.item-name')).toHaveText(['Lanternedge']);
	await expect(page).toHaveURL(/category=weapon/);
	await page.reload();
	await expect(weapons).toBeChecked();
	await expect(page.locator('.item-name')).toHaveText(['Lanternedge']);
	await weapons.uncheck();
	await page.getByRole('button', { name: 'Value', exact: true }).click();
	await expect(page.getByRole('columnheader', { name: 'Value' })).toHaveAttribute(
		'aria-sort',
		'descending'
	);
	await expect(page.locator('.item-name')).toHaveText([
		'Mosswoven Coat',
		'Lanternedge',
		'Cinderpear'
	]);
	await page.getByRole('checkbox', { name: /Show \d+ items without a known source/ }).check();
	await expect(page.locator('.item-name').last()).toHaveText('Hollow Prism');
	await expect(page.locator('.item-name img').first()).toHaveAttribute('src', /-48.webp$/);
	await page.getByRole('button', { name: 'Item', exact: true }).click();
	await page.getByRole('button', { name: 'Item', exact: true }).click();
	await expect(page.locator('.item-name').first()).toHaveText('Mosswoven Coat');
});

test('name filtering combines with facets and empty results can be cleared', async ({ page }) => {
	await page.goto('/items/');
	const categories = page.getByRole('complementary', { name: 'Item filters' });
	await categories.getByRole('checkbox', { name: /^Weapons\s/ }).check();
	const name = page.getByRole('searchbox', { name: 'Search by name' });
	await name.fill('Lantern');
	await name.press('Enter');
	await expect(page.locator('.item-name')).toHaveText(['Lanternedge']);
	await expect(page).toHaveURL(/name=Lantern/);
	await name.fill('No invented match');
	await name.press('Enter');
	await expect(page.locator('.item-name')).toHaveCount(0);
	await expect(page.getByText('No items match these filters.', { exact: false })).toBeVisible();
	await page.getByRole('button', { name: 'Clear all', exact: true }).click();
	await expect(name).toHaveValue('');
	await expect(page.locator('.item-name')).toHaveCount(3);
	await expect(page).toHaveURL('/items/');
});

test('combines Category and Type selections and restores them from a shared URL', async ({
	page
}) => {
	await page.goto('/items/?category=weapon&category=food&type=Sword');
	const facets = page.getByRole('complementary', { name: 'Item filters' });
	await expect(facets.getByRole('checkbox', { name: /^Weapons\s/ })).toBeChecked();
	await expect(facets.getByRole('checkbox', { name: /^Food\s/ })).toBeChecked();
	await expect(facets.getByRole('checkbox', { name: /^Sword\s/ })).toBeChecked();
	await expect(page.locator('.item-name')).toHaveText(['Lanternedge']);
	await facets.getByRole('checkbox', { name: /^Fruit\s/ }).check();
	await expect(page.locator('.item-name')).toHaveText(['Cinderpear', 'Lanternedge']);
	await page.reload();
	await expect(page.locator('.item-name')).toHaveText(['Cinderpear', 'Lanternedge']);
});

test('phone filters open as a keyboard-accessible sheet and apply without navigation', async ({
	page
}) => {
	await page.setViewportSize({ width: 390, height: 900 });
	await page.goto('/items/');
	const open = page.getByRole('button', { name: 'Filters', exact: true });
	await open.focus();
	await page.keyboard.press('Enter');
	const dialog = page.getByRole('dialog', { name: 'Filters' });
	await expect(dialog).toBeVisible();
	const weapon = dialog.getByRole('checkbox', { name: /^Weapons\s/ });
	await weapon.focus();
	await page.keyboard.press('Space');
	await expect(weapon).toBeChecked();
	await dialog.getByRole('button', { name: 'Show 1 item', exact: true }).click();
	await expect(dialog).not.toBeVisible();
	await expect(page.locator('.item-name')).toHaveText(['Lanternedge']);
	await expect(page).toHaveURL(/category=weapon/);
});
test('answers acquisition first and exposes facts, quantities and uncertainty', async ({
	page
}) => {
	await page.goto('/items/lanternedge/');
	await expect(page.getByRole('heading', { name: 'How to get it', exact: true })).toBeVisible();
	const details = page.getByRole('complementary', { name: 'Item details' });
	await expect(details).toContainText(/Damage\s*12/);
	await expect(details).toContainText(/Speed\s*Average/);
	await expect(details).toContainText(/Attack Speed\s*1.2/);
	await expect(details).toContainText(/Range\s*Unknown/);
	const shopLink = page.locator('.acquisition-routes a[href="#source-shop"]');
	await shopLink.click();
	await expect(page).toHaveURL(/#source-shop$/);
	await expect(page.locator('#source-shop')).toBeInViewport();
	for (const summary of await page.locator('.source-groups details > summary').all())
		await summary.click();
	for (const text of [
		'Glass Reach',
		'45',
		'Cinderpear',
		'1 to 2',
		'25%',
		'Chance per pick',
		'2 to 3 picks per container',
		'Required items',
		'Silverglass shard',
		'Requires a lit forge.',
		'Location unknown'
	])
		await expect(page.locator('article')).toContainText(text);
	await expect(details.locator('img')).toHaveAttribute('src', /-160.webp$/);
	await expect(page.locator('article')).not.toContainText('Selection weight');
	await page.goto('/items/mosswoven-coat/');
	await expect(page.locator('.source-group')).toContainText('1 of 5');
	await expect(page.locator('.source-group')).toContainText('Relative chance');
	await expect(page.locator('.source-group')).not.toContainText('20%');
	await page.goto('/items/hollow-prism/');
	await expect(page.getByText('No known source.', { exact: false })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Read about gaps in coverage' })).toHaveAttribute(
		'href',
		'/coverage/#itemWithoutSource'
	);
	await expect(page.getByText('No icon is known for this item.')).toBeVisible();
});
test('finds items by word prefix from the shared search', async ({ page }) => {
	await page.goto('/');
	const search = page.locator('pagefind-searchbox input');
	await search.fill('Lanter');
	await expect(
		page
			.locator('pagefind-searchbox')
			.getByRole('option', { name: /Lanternedge/ })
			.first()
	).toBeVisible();
});
test('coverage, release labels, provenance and static 404', async ({ page, request }) => {
	await page.goto('/coverage/');
	for (const text of [
		'Items without a known source',
		'Sources without a known place',
		'Items without an icon',
		'Unknown item facts',
		'Places without a known name',
		'Unread objects and packages'
	])
		await expect(page.getByRole('heading', { name: text })).toBeVisible();
	await expect(page.locator('#itemWithoutSource a')).toHaveAttribute(
		'href',
		'/items/hollow-prism/'
	);
	for (const route of ['/', '/items/', '/items/lanternedge/', '/coverage/', '/missing/page']) {
		await page.goto(route);
		await expect(page.locator('footer').getByText('Invented Fixture Release')).toBeVisible();
		await expect(page.locator('footer').getByRole('link', { name: 'Patch Notes' })).toHaveAttribute(
			'href',
			'https://example.com/invented-bloodline-patch'
		);
		await expect(page.locator('footer time').first()).toHaveAttribute('datetime', '2026-10-01');
		await expect(page.locator('footer time').last()).toHaveAttribute('datetime', '2026-10-02');
	}
	await expect(page.getByRole('heading', { name: 'Page not found' })).toBeVisible();
	const missing = await request.get('/missing/page');
	expect(missing.status()).toBe(404);
	const info = await (await request.get('/build-info.json')).json();
	expect(Object.keys(info).sort()).toEqual(['appId', 'buildId', 'commit', 'depots', 'dirty']);
	expect(info.buildId).toBe(90000001);
});
test('reference layouts stay readable on desktop and phone', async ({ page }, testInfo) => {
	for (const [name, width] of [
		['desktop', 1280],
		['phone', 390]
	] as const) {
		await page.setViewportSize({ width, height: 900 });
		for (const [label, route] of [
			['list', '/items/'],
			['item', '/items/lanternedge/'],
			['coverage', '/coverage/']
		]) {
			await page.goto(route);
			expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
				true
			);
			await page.screenshot({
				path: testInfo.outputPath(label + '-' + name + '.png'),
				fullPage: true
			});
		}
	}
});

test('source and coverage disclosures work without JavaScript', async ({ browser, baseURL }) => {
	const context = await browser.newContext({ javaScriptEnabled: false, baseURL });
	try {
		const page = await context.newPage();
		await page.goto('/items/lanternedge/');
		const shops = page.locator('.source-group').filter({
			has: page.getByRole('heading', { name: /^Sold by/ })
		});
		const rows = shops.locator('.source-row');
		const total = await rows.count();
		expect(total).toBeGreaterThan(10);
		await expect(shops.locator('.source-row:visible')).toHaveCount(8);
		await shops.locator('.more-entries > summary').click();
		await expect(shops.locator('.source-row:visible')).toHaveCount(total);
		await shops.locator('.more-entries > summary').click();
		await expect(shops.locator('.source-row:visible')).toHaveCount(8);
		await page.goto('/coverage/');
		for (const id of ['sourceWithoutPlace', 'placeWithoutName']) {
			const gap = page.locator('#' + id);
			const total = await gap.locator('.gap-list > li').count();
			await expect(gap.locator('.gap-list > li:visible')).toHaveCount(total <= 10 ? total : 8);
			if (total > 10) {
				await gap.locator('summary').click();
				await expect(gap.locator('.gap-list > li:visible')).toHaveCount(total);
				await gap.locator('summary').click();
				await expect(gap.locator('.gap-list > li:visible')).toHaveCount(8);
			} else {
				await expect(gap.locator('summary')).toHaveCount(0);
			}
		}
	} finally {
		await context.close();
	}
});

test('each public page has production identity, social metadata and generated browser icons', async ({
	page,
	request
}) => {
	const origin = 'https://bloodline.compendiums.org';
	const routes = [
		'/',
		'/items/',
		'/items/lanternedge/',
		'/items/hollow-prism/',
		'/coverage/',
		'/about/'
	];
	for (const route of routes) {
		await page.goto(route + '?review=1');
		const title = await page.title();
		const description = await page.locator('meta[name="description"]').getAttribute('content');
		expect(description?.length).toBeGreaterThan(10);
		await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', origin + route);
		for (const prefix of ['og', 'twitter']) {
			const attribute = prefix === 'og' ? 'property' : 'name';
			await expect(page.locator(`meta[${attribute}="${prefix}:title"]`)).toHaveAttribute(
				'content',
				title
			);
			await expect(page.locator(`meta[${attribute}="${prefix}:description"]`)).toHaveAttribute(
				'content',
				description!
			);
			await expect(page.locator(`meta[${attribute}="${prefix}:image"]`)).toHaveAttribute(
				'content',
				origin + '/og-default.png'
			);
		}
	}
	await page.goto('/items/lanternedge/');
	await expect(page.locator('meta[name="description"]')).toHaveAttribute(
		'content',
		/Lanternedge.*The Bloodline/
	);
	await page.goto('/items/hollow-prism/');
	await expect(page.locator('meta[name="description"]')).toHaveAttribute('content', /Hollow Prism/);
	for (const icon of [
		'brand/emblem-48.webp',
		'brand/emblem-96.webp',
		'favicon-16x16.png',
		'favicon-32x32.png',
		'favicon-192x192.png',
		'favicon-512x512.png',
		'apple-touch-icon.png',
		'og-default.png'
	]) {
		const response = await request.get('/' + icon);
		expect(response.status()).toBe(200);
		expect(response.headers()['content-type']).toContain(
			icon.endsWith('.webp') ? 'image/webp' : 'image/png'
		);
	}
	const sitemap = await (await request.get('/sitemap.xml')).text();
	const locations = [...sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)].map((match) => match[1]);
	expect(locations.sort()).toEqual(
		[
			'/',
			'/items/',
			'/coverage/',
			'/items/cinderpear/',
			'/about/',
			'/items/hollow-prism/',
			'/items/lanternedge/',
			'/items/mosswoven-coat/'
		]
			.map((route) => origin + route)
			.sort()
	);
	expect(await (await request.get('/robots.txt')).text()).toContain(
		'Sitemap: ' + origin + '/sitemap.xml'
	);
});

test('renders typed references and every relation without JavaScript', async ({
	browser,
	baseURL,
	request
}) => {
	const graph = await (await request.get('/graph.json')).json();
	const kinds = await (await request.get('/kinds.json')).json();
	const itemKind = kinds.find((kind: { kind: string }) => kind.kind === 'item');
	const context = await browser.newContext({ javaScriptEnabled: false, baseURL });
	try {
		const page = await context.newPage();
		for (const document of graph.entities.filter(
			(entry: { entity: { kind: string } }) => entry.entity.kind === 'item'
		)) {
			await page.goto(`/${itemKind.route}/${document.entity.slug}/`);
			for (const relation of document.relations)
				await expect(
					page.locator(
						`[data-relation-id="${relation.id}"][data-relation-direction="${relation.direction}"]`
					)
				).toHaveCount(1);
		}
		await page.goto('/items/lanternedge/');
		const source = page.locator('#source-shop-1');
		await expect(source).toContainText('Mira at the Glass Market');
		await expect(source.locator('a')).toHaveCount(0);
		await expect(source).toContainText('Glass Reach');
		await expect(source).toContainText('Stock refresh conditions are unknown.');
		await expect(page.locator('article')).toHaveAttribute(
			'data-pagefind-filter',
			`kind:${itemKind.searchFilter}`
		);
		const structured = await page.locator('script[type="application/ld+json"]').allTextContents();
		expect(structured.map((value) => JSON.parse(value))).toContainEqual(
			expect.objectContaining({
				'@type': itemKind.structuredDataType,
				identifier: '/Invented/Items/Lanternedge'
			})
		);
		const sitemap = await (await request.get('/sitemap.xml')).text();
		for (const document of graph.entities.filter(
			(entry: { entity: { kind: string } }) => entry.entity.kind === 'item'
		))
			expect(sitemap).toContain(`/${itemKind.route}/${document.entity.slug}/`);
	} finally {
		await context.close();
	}
});

test('Pagefind searches with the registry kind filter', async ({ page, request }) => {
	const kinds = await (await request.get('/kinds.json')).json();
	const label = kinds.find((kind: { kind: string }) => kind.kind === 'item').searchFilter;
	await page.goto('/');
	const count = await page.evaluate(async (label) => {
		const path = '/pagefind/pagefind.js';
		// Pagefind is generated at build time and exists only in the served browser bundle.
		const pagefind = await import(/* @vite-ignore */ path);
		const result = await pagefind.search('Lanter', { filters: { kind: label } });
		return result.results.length;
	}, label);
	expect(count).toBeGreaterThan(0);
});

test('condition prose links its entity without repeating the name or threshold', async ({
	page
}) => {
	await page.goto('/design/');
	const condition = page.locator('#condition-fixture li');
	const name = await condition.getByRole('link').innerText();
	const text = await condition.innerText();
	expect(text.split(name)).toHaveLength(2);
	expect(text.match(/13/g)).toHaveLength(1);
});
