import { expect, test } from '@playwright/test';

const path = '/items/twin-lantern/';

test('server renders every version with one plain page title', async ({ page }) => {
	const response = await page.goto(path);
	const html = await response!.text();
	expect(html).toContain('twin_lantern_a');
	expect(html).toContain('twin_lantern_b');
	await expect(page.getByRole('heading', { level: 1 })).toHaveText('Twin Lantern');
	await expect(page).toHaveTitle('Twin Lantern | The Bloodline Compendium');
	const tabs = page.getByRole('tablist', { name: 'Twin Lantern' });
	await expect(tabs.getByRole('tab')).toHaveText(['12 Damage', '24 Damage']);
	await expect(page.getByRole('tabpanel')).toHaveCount(1);
	await expect(page.getByRole('complementary', { name: 'Game card' })).toContainText('Damage: 12');
});

test('opens a linked version and switches the whole view with keyboard tabs', async ({ page }) => {
	await page.goto(path + '#twin_lantern_b');
	const first = page.getByRole('tab', { name: '12 Damage', exact: true });
	const second = page.getByRole('tab', { name: '24 Damage', exact: true });
	await expect(second).toHaveAttribute('aria-selected', 'true');
	await expect(page.getByRole('complementary', { name: 'Game card' })).toContainText('Damage: 24');
	await second.focus();
	await second.press('ArrowRight');
	await expect(first).toBeFocused();
	await expect(first).toHaveAttribute('aria-selected', 'true');
	await expect(page).toHaveURL(/#twin_lantern_a$/);
	await expect(page.getByRole('complementary', { name: 'Game card' })).toContainText('Damage: 12');
	await first.press('End');
	await expect(second).toBeFocused();
	await expect(page).toHaveURL(/#twin_lantern_b$/);
	await second.press('Home');
	await expect(first).toBeFocused();
	await first.press('ArrowLeft');
	await expect(second).toBeFocused();
	await first.click();
	await expect(page.getByRole('tabpanel')).toHaveAttribute('id', 'twin_lantern_a');
	await page.reload();
	await expect(first).toHaveAttribute('aria-selected', 'true');
});

test('list rows link and preview the exact version while search returns one name', async ({
	page
}) => {
	await page.goto('/items/?name=Twin%20Lantern');
	await expect(page.locator('.item-name')).toHaveCount(2);
	await expect(page.locator('.version-label')).toHaveText(['12 Damage', '24 Damage']);
	const second = page.locator('.item-name[href$="#twin_lantern_b"]');
	await second.focus();
	await expect(page.locator('#entity-preview')).toContainText('Damage: 24');
	await expect(page.locator('#entity-preview .name')).toHaveText('Twin Lantern');
	await second.press('Enter');
	await expect(page.getByRole('tab', { name: '24 Damage', exact: true })).toHaveAttribute(
		'aria-selected',
		'true'
	);
	await page.goto('/');
	await page.locator('pagefind-searchbox input').fill('Twin Lantern');
	await expect(
		page.locator('pagefind-searchbox').getByRole('option', { name: /Twin Lantern/ })
	).toHaveCount(1);
});

test('the first version works without JavaScript', async ({ browser }) => {
	const context = await browser.newContext({ javaScriptEnabled: false });
	const page = await context.newPage();
	await page.goto(path);
	await expect(page.getByRole('heading', { level: 1 })).toHaveText('Twin Lantern');
	await expect(page.getByRole('complementary', { name: 'Game card' })).toContainText('Damage: 12');
	await expect(page.getByRole('tabpanel')).toHaveCount(1);
	await context.close();
});
