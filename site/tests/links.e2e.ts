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

test('a profession page shows what each level unlocks and leads to its items in the list', async ({
	page
}) => {
	await page.goto('/professions/starsmithing/');
	await expect(page.getByRole('heading', { level: 1 })).toHaveText('Starsmithing');
	const groups = page.locator('.level-group');
	await expect(groups).toHaveCount(2);
	await expect(groups.first()).toContainText('Level12');
	await expect(groups.first()).toContainText('2 items');
	await expect(groups.first().getByRole('link', { name: 'Lanternedge' })).toBeVisible();
	await expect(groups.last()).toContainText('LevelNone');
	await page.getByRole('link', { name: 'All 3 in the item list' }).click();
	await expect(page).toHaveURL(/\/items\/\?.*profession=Starsmithing.*sort=professionLevel/);
});

test('a phone collapses profession levels until a reader opens one', async ({ browser }) => {
	const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
	const page = await context.newPage();
	await page.goto('/professions/starsmithing/');
	const level = page.locator('.level-group').first();
	await expect(level.getByRole('link', { name: 'Lanternedge' })).toBeHidden();
	await level.getByRole('button', { name: /^Show/ }).click();
	await expect(level.getByRole('link', { name: 'Lanternedge' })).toBeVisible();
	await context.close();
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
