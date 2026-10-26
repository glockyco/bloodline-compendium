import { expect, test } from '@playwright/test';
test('filters categories and sorts by name and numeric value', async ({ page }) => {
	await page.goto('/items/');
	await page.getByRole('combobox', { name: 'Category', exact: true }).selectOption('weapons');
	await expect(page.locator('.item-name')).toHaveText([/Lanternedge\s*Weapons/]);
	await page.getByRole('combobox', { name: 'Category', exact: true }).selectOption('');
	await page.getByRole('combobox', { name: 'Sort by' }).selectOption('value-desc');
	await expect(page.locator('.item-name')).toHaveText([
		/Mosswoven Coat\s*Armour/,
		/Lanternedge\s*Weapons/,
		/Cinderpear\s*Food/,
		/Hollow Prism\s*Curios/
	]);
	await page.getByRole('combobox', { name: 'Sort by' }).selectOption('name-desc');
	await expect(page.locator('.item-name').first()).toContainText('Mosswoven Coat');
	await expect(page.locator('.item-row img').first()).toHaveAttribute('src', /-48.webp$/);
});
test('shows facts, complete source fields and unknowns', async ({ page }) => {
	await page.goto('/items/lanternedge/');
	await expect(page.locator('.facts')).toContainText(/Damage\s*12/);
	await expect(page.locator('.facts')).toContainText(/Attack speed\s*1.2/);
	await expect(page.locator('.facts')).toContainText(/Critical chance\s*Unknown/);
	for (const text of [
		'Glass Reach',
		'45 gold',
		'3 Cinderpear',
		'1 to 2',
		'2 of 18.02',
		'25%',
		'Consumed inputs',
		'Silverglass shard',
		'Requires a lit forge.',
		/Places:\s*Unknown/
	])
		await expect(page.locator('article')).toContainText(text);
	await expect(page.locator('.large-icon')).toHaveAttribute('src', /-160.webp$/);
	await page.goto('/items/hollow-prism/');
	await expect(page.getByText('No source is known for this item.', { exact: false })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Read about gaps in coverage' })).toHaveAttribute(
		'href',
		'/coverage/#itemWithoutSource'
	);
	await expect(page.getByText('No icon known')).toBeVisible();
});
test('finds items by word prefix from the shared search', async ({ page }) => {
	await page.goto('/');
	await page.getByText('Search items', { exact: true }).click();
	const search = page.locator('pagefind-searchbox input');
	await search.fill('Lanter');
	await expect(
		page
			.locator('pagefind-searchbox')
			.getByRole('option', { name: /Lanternedge/ })
			.first()
	).toBeVisible();
});
test('coverage, build labels, provenance and static 404', async ({ page, request }) => {
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
		await expect(page.getByText('Steam build 90000001')).toBeVisible();
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
	const page = await context.newPage();
	await page.goto('/items/lanternedge/');
	const shops = page
		.locator('.source-group')
		.filter({ has: page.getByRole('heading', { name: 'Sold by' }) });
	await expect(shops.locator('.source-row:visible')).toHaveCount(3);
	await expect(shops.getByText('Lilac Feather Trader', { exact: true })).not.toBeVisible();
	await shops.locator('summary').click();
	await expect(shops.locator('.source-row:visible')).toHaveCount(13);
	await expect(shops.getByText('Lilac Feather Trader', { exact: true })).toBeVisible();
	await shops.locator('summary').click();
	await expect(shops.locator('.source-row:visible')).toHaveCount(3);
	await page.goto('/coverage/');
	for (const id of ['sourceWithoutPlace', 'placeWithoutName']) {
		const gap = page.locator('#' + id);
		await expect(gap.locator('.gap-list > li:visible')).toHaveCount(8);
		const total = await gap.locator('.gap-list > li').count();
		await gap.locator('summary').click();
		await expect(gap.locator('.gap-list > li:visible')).toHaveCount(total);
		await gap.locator('summary').click();
		await expect(gap.locator('.gap-list > li:visible')).toHaveCount(8);
	}
	await context.close();
});

test('each public page has production identity, social metadata and generated browser icons', async ({
	page,
	request
}) => {
	const origin = 'https://bloodline.compendiums.org';
	const routes = ['/', '/items/', '/items/lanternedge/', '/items/hollow-prism/', '/coverage/'];
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
		await page.locator('.item-heading .intro').innerText()
	);
	await page.goto('/items/hollow-prism/');
	await expect(page.locator('meta[name="description"]')).toHaveAttribute('content', /Hollow Prism/);
	for (const icon of [
		'favicon.svg',
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
			icon.endsWith('.svg') ? 'image/svg+xml' : 'image/png'
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
