import { expect, test } from '@playwright/test';

test('an entry that checks another discipline names and links it', async ({ page }) => {
	await page.goto('/disciplines/emberlore/');
	const row = page.locator('#passive-ashen-heart-of-the-long-burning-hearth');
	await expect(row).toContainText('Unlocks at Songcraft level 10');
	await expect(row.getByRole('link', { name: 'Songcraft' })).toHaveAttribute(
		'href',
		'/disciplines/songcraft/'
	);
	// An entry of the page's own discipline sits under the level that unlocks it, so it needs no note.
	const glow = page.locator('#passive-glow');
	await expect(
		glow.locator('xpath=preceding-sibling::tr[contains(@class, "group-heading")][1]')
	).toHaveText('Level 3');
	await expect(glow).not.toContainText('Unlocks at');
});

test('text written for the site links the entities it marks', async ({ page }) => {
	await page.goto('/known-issues/');
	const effect = page.locator('#invented-coat-armor .effect');
	await expect(effect).toHaveText(
		'The Mosswoven Coat adds no armor when worn, although its tooltip shows armor.'
	);
	await expect(effect.getByRole('link', { name: 'Mosswoven Coat' })).toHaveAttribute(
		'href',
		'/items/mosswoven-coat/'
	);
});

test('a profession page lists every recipe under the level that unlocks it, with what it takes', async ({
	page
}) => {
	await page.goto('/professions/starsmithing/');
	await expect(page.getByRole('heading', { level: 1 })).toHaveText('Starsmithing');
	const table = page.getByRole('table', { name: 'Recipes' });
	await expect(table.locator('tr.group-heading')).toHaveText(['Level 12 2', 'Level None 1']);
	const row = table
		.getByRole('row')
		.filter({ has: page.getByRole('link', { name: 'Lanternedge' }) });
	await expect(row.getByRole('list', { name: 'Ingredients' }).getByRole('link')).toHaveText([
		'3 Cinderpear',
		'1 Hollow Prism'
	]);
	await expect(row.getByRole('cell').last()).toHaveText('40');
	await expect(
		table.getByRole('row').filter({ has: page.getByRole('link', { name: 'Mosswoven Coat' }) })
	).toContainText('Makes 2');
	await page.getByRole('link', { name: 'Sort and filter in the item list' }).click();
	await expect(page).toHaveURL(/\/items\/\?.*profession=Starsmithing.*sort=professionLevel/);
});

test('an item names the resource it is gathered from, whose page lists the item and the areas', async ({
	page
}) => {
	await page.goto('/items/cinderpear/');
	const row = page.locator('[data-relation-id="fixture:cinderpear:source:0"]');
	await expect(row).toContainText('In 1 area');
	await row.getByRole('link', { name: 'Ember orchard' }).click();
	await expect(page).toHaveURL(/\/resources\/ember-orchard\/$/);
	await expect(page.locator('[data-relation-id="fixture:cinderpear:source:0"]')).toContainText(
		'Cinderpear'
	);
	const area = page.getByRole('table', { name: 'Areas with Ember orchard' }).locator('tbody tr');
	await expect(area).toContainText('Glass Reach');
	await expect(area.locator('td')).toHaveText(['Glass Reach', '1', '4']);
});

test('a mechanic page links what it explains, and section intros link the mechanic', async ({
	page
}) => {
	await page.goto('/mechanics/skill-points/');
	await expect(page.getByRole('heading', { level: 1 })).toHaveText('Skill Points');
	await expect(page.locator('.intro').getByRole('link', { name: 'discipline' })).toHaveAttribute(
		'href',
		'/disciplines/emberlore/'
	);
	await expect(page.locator('#earning-skill-points')).toHaveText('Earning Skill Points');
	await expect(page.getByText('For Developers')).toBeVisible();
	await page.goto('/disciplines/emberlore/');
	await expect(
		page
			.getByRole('region', { name: /^Passives/ })
			.getByRole('link', { name: 'proficiency points' })
	).toHaveAttribute('href', '/mechanics/proficiency-points/');
	await page.goto('/skills/cinder-flick/');
	await expect(page.getByRole('link', { name: 'skill point' })).toHaveAttribute(
		'href',
		'/mechanics/skill-points/'
	);
});
