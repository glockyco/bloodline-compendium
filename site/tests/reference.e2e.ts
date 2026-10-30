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
	await page.getByRole('button', { name: 'Show 1 more without a known way to get them' }).click();
	await expect(page.locator('.item-name').last()).toHaveText('Hollow Prism');
	await expect(page.locator('tr', { hasText: 'Hollow Prism' })).toContainText(
		'No known way to get it'
	);
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

test('combines filters, restores them from a shared URL and adds stat columns', async ({
	page
}) => {
	await page.goto('/items/?category=weapon&category=food&weaponType=Sword');
	const facets = page.getByRole('complementary', { name: 'Item filters' });
	await expect(facets.getByRole('checkbox', { name: /^Weapons\s/ })).toBeChecked();
	await expect(facets.getByRole('checkbox', { name: /^Food\s/ })).toBeChecked();
	await expect(facets.getByRole('checkbox', { name: /^Sword\s/ })).toBeChecked();
	await expect(page.locator('.item-name')).toHaveText(['Lanternedge']);
	// A list opened from a filtered address shows each row with its own icon.
	await expect(page.locator('.item-name img')).toHaveAttribute('src', '/icons/lanternedge-48.webp');
	await facets.getByRole('checkbox', { name: /^Sword\s/ }).uncheck();
	await expect(page.locator('.item-name')).toHaveText(['Cinderpear', 'Lanternedge']);
	await page.reload();
	await expect(page.locator('.item-name')).toHaveText(['Cinderpear', 'Lanternedge']);
	await page.goto('/items/');
	await facets.locator('summary', { hasText: 'Stats' }).click();
	await facets.getByRole('combobox', { name: 'Add a stat filter' }).selectOption('armor');
	await expect(page.getByRole('columnheader', { name: 'Armor' })).toHaveAttribute(
		'aria-sort',
		'descending'
	);
	await expect(page.locator('.item-name')).toHaveText(['Mosswoven Coat']);
	await expect(page).toHaveURL(/stat=armor/);
});

test('name search finds items without a known way to get them', async ({ page }) => {
	await page.goto('/items/?name=prism');
	await expect(page.locator('.item-name')).toHaveText(['Hollow Prism']);
	await expect(page.locator('tr', { hasText: 'Hollow Prism' })).toContainText(
		'No known way to get it'
	);
});

test('an item link covers its icon and name, not the rest of its cell', async ({ page }) => {
	await page.goto('/items/');
	const link = page.locator('.item-name', { hasText: 'Lanternedge' });
	const linkBox = (await link.boundingBox())!;
	const cellBox = (await page.locator('td', { has: link }).boundingBox())!;
	expect(linkBox.width).toBeLessThan(cellBox.width - 40);
	await page.mouse.move(cellBox.x + cellBox.width - 10, cellBox.y + cellBox.height / 2);
	await page.waitForTimeout(400);
	await expect(page.locator('#entity-preview')).toHaveCount(0);
	await page.mouse.move(linkBox.x + linkBox.width / 2, linkBox.y + linkBox.height / 2);
	await expect(page.locator('#entity-preview')).toContainText('Lanternedge');
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
	await expect(page.getByRole('heading', { name: 'How to Get It', exact: true })).toBeVisible();
	await expect(page.locator('.acquisition-routes')).toContainText('Buy from');
	const details = page.getByRole('complementary', { name: 'Item details' });
	await expect(details).toContainText('Damage: 12');
	await expect(details.locator('.first')).toHaveText('Average');
	await expect(details).toContainText(/Attack Speed\s*1.2/);
	await expect(details).toContainText(/Range\s*Unknown/);
	const shopLink = page.locator('.acquisition-routes a[href="#source-shop"]');
	await shopLink.click();
	await expect(page).toHaveURL(/#source-shop$/);
	await expect(page.locator('#source-shop')).toBeInViewport();
	for (const summary of await page.locator('.item-relations details > summary').all())
		await summary.click();
	for (const text of [
		'Glass Reach',
		'63',
		'Cinderpear',
		'1 to 2',
		'50.8%',
		'per container',
		'Makes',
		'Silverglass shard',
		'Requires a lit forge.',
		'Location unknown'
	])
		await expect(page.locator('article')).toContainText(text);
	await expect(details.locator('img')).toHaveAttribute('src', /-160.webp$/);
	await expect(page.locator('article')).not.toContainText('Selection weight');
	await page.goto('/items/mosswoven-coat/');
	await expect(page.locator('.source-group')).toContainText('20%');
	await expect(page.locator('.source-group')).toContainText('per kill');
	await page.goto('/items/cinderpear/');
	const unknown = page.locator('.source-group').getByRole('button', { name: 'Unknown' });
	await expect(page.getByRole('tooltip')).toBeHidden();
	await unknown.focus();
	await expect(page.getByRole('tooltip')).toHaveText('How often the orchard yields it is unknown.');
	await page.goto('/items/hollow-prism/');
	await expect(
		page.getByText('No way to get this item is known in this game version.')
	).toBeVisible();
	await expect(page.getByText('No icon is known for this item.')).toBeVisible();
});
test('previews an item link on keyboard focus and hover', async ({ page }) => {
	await page.goto('/items/lanternedge/');
	const ingredient = page.locator('.recipe').getByRole('link', { name: 'Cinderpear' });
	await ingredient.focus();
	const preview = page.locator('#entity-preview');
	await expect(preview).toContainText('Cinderpear');
	await expect(ingredient).toHaveAttribute('aria-describedby', 'entity-preview');
	await page.keyboard.press('Escape');
	await expect(preview).toBeHidden();
	await ingredient.hover();
	await expect(preview).toContainText('Cinderpear');
	// Following a link leaves no tooltip behind on the next page.
	await ingredient.click();
	await expect(page).toHaveURL('/items/cinderpear/');
	await expect(page.getByRole('heading', { level: 1 })).toHaveText('Cinderpear');
	await page.waitForTimeout(400);
	await expect(preview).toHaveCount(0);
});
test('a first tap previews an item link and a second tap follows it', async ({
	browser,
	baseURL
}) => {
	const context = await browser.newContext({
		baseURL,
		hasTouch: true,
		isMobile: true,
		viewport: { width: 390, height: 844 }
	});
	try {
		const page = await context.newPage();
		await page.goto('/items/lanternedge/');
		await page.waitForLoadState('networkidle');
		const ingredient = page.locator('.recipe').getByRole('link', { name: 'Cinderpear' });
		await ingredient.tap();
		await expect(page.locator('#entity-preview')).toContainText('Cinderpear');
		await expect(page).toHaveURL('/items/lanternedge/');
		await ingredient.tap();
		await expect(page).toHaveURL('/items/cinderpear/');
	} finally {
		await context.close();
	}
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
test('release labels, provenance and static 404', async ({ page, request }) => {
	for (const route of ['/', '/items/', '/items/lanternedge/', '/known-issues/', '/missing/page']) {
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
	// The missing page loads the site's styles and scripts at a nested address too.
	expect(
		await page.evaluate(() => getComputedStyle(document.documentElement).backgroundColor)
	).not.toBe('rgba(0, 0, 0, 0)');
	await expect(page.locator('pagefind-searchbox input')).toBeVisible();
	const missing = await request.get('/missing/page');
	expect(missing.status()).toBe(404);
	expect((await request.get('/coverage/')).status()).toBe(404);
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
			['issues', '/known-issues/']
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

test('renders rows past the preview only when asked', async ({ page }) => {
	await page.goto('/items/lanternedge/');
	const shops = page.locator('.source-group').filter({
		has: page.getByRole('heading', { name: /^Sold by/ })
	});
	await expect(shops.locator('.source-row')).toHaveCount(8);
	const toggle = shops.getByRole('button', { name: /^Show 5 more/ });
	await expect(toggle).toHaveAttribute('aria-expanded', 'false');
	await toggle.click();
	await expect(shops.locator('.source-row')).toHaveCount(13);
	await shops.getByRole('button', { name: /^Show fewer/ }).click();
	await expect(shops.locator('.source-row')).toHaveCount(8);
});
test('source disclosures work without JavaScript', async ({ browser, baseURL }) => {
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
	} finally {
		await context.close();
	}
});

test('each public page has production identity, social metadata and generated browser icons', async ({
	page,
	request
}) => {
	const origin = 'https://the-bloodline.compendiums.org';
	const routes = [
		'/',
		'/items/',
		'/items/lanternedge/',
		'/items/hollow-prism/',
		'/known-issues/',
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
			'/known-issues/',
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
		// Merchants sharing a place stand under one heading for it.
		await expect(
			page
				.locator('.source-group', { has: page.locator('#source-shop') })
				.locator('.place-heading')
				.first()
		).toContainText('Glass Reach');
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

test('notes a known issue on the affected item and explains it on a linked, searchable page', async ({
	page
}) => {
	await page.goto('/items/mosswoven-coat/');
	const note = page.getByRole('complementary', { name: 'Known game issue' });
	await expect(note).toContainText('The Mosswoven Coat adds no armor');
	const details = page.getByRole('complementary', { name: 'Item details' });
	await expect(details).toContainText('ARMOR: 0');
	await expect(
		details.getByRole('link', { name: "The game's tooltip shows Armor 8" })
	).toHaveAttribute('href', '/known-issues/#invented-coat-armor');
	await note.getByRole('link', { name: 'Read about this issue' }).click();
	await expect(page).toHaveURL(/\/known-issues\/#invented-coat-armor$/);
	const entry = page.locator('#invented-coat-armor');
	await expect(entry).toBeInViewport();
	await expect(entry.getByRole('link', { name: 'Mosswoven Coat' })).toHaveAttribute(
		'href',
		'/items/mosswoven-coat/'
	);
	await expect(entry.getByText('Not reported to the developer yet.')).toBeHidden();
	await entry.getByText('For Developers').click();
	await expect(entry.getByText('Not reported to the developer yet.')).toBeVisible();
	await page.goto('/known-issues/');
	await page.getByRole('navigation', { name: 'Issues' }).getByRole('link').first().click();
	await expect(page).toHaveURL(/\/known-issues\/#invented-coat-armor$/);
	await page.goto('/about/');
	for (const region of ['main', 'footer'])
		await expect(page.locator(region).getByRole('link', { name: 'Known Issues' })).toHaveAttribute(
			'href',
			'/known-issues/'
		);
	const urls = await page.evaluate(async () => {
		const path = '/pagefind/pagefind.js';
		// Pagefind is generated at build time and exists only in the served browser bundle.
		const pagefind = await import(/* @vite-ignore */ path);
		const result = await pagefind.search('Mosswoven');
		return Promise.all(
			result.results.map(
				async (found: { data: () => Promise<{ url: string }> }) => (await found.data()).url
			)
		);
	});
	expect(urls).toContain('/known-issues/');
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
