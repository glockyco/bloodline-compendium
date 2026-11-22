import { expect, test } from '@playwright/test';

test('an entry that checks another discipline names and links it', async ({ page }) => {
	await page.goto('/disciplines/emberlore/');
	const row = page.locator('#passive-ashen-heart-of-the-long-burning-hearth');
	await expect(row).toContainText('Unlocks at Songcraft level 10');
	await expect(row.getByRole('link', { name: 'Songcraft' })).toHaveAttribute(
		'href',
		'/disciplines/songcraft/'
	);
	// An entry of the page's own discipline needs no link to the page it is on.
	await expect(page.locator('#passive-glow')).toContainText('Unlocks at Emberlore level 3');
	await expect(page.locator('#passive-glow').getByRole('link', { name: 'Emberlore' })).toHaveCount(
		0
	);
});
