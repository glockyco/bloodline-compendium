import { expect, test } from '@playwright/test';

test('the skill list hides skills without a source behind a counted reveal, and name search finds them', async ({
	page
}) => {
	await page.goto('/skills/');
	await expect(page.locator('.entity-name')).toHaveText([
		'Cinder Flick',
		'Lanternedge Flare',
		'Moss Ward'
	]);
	await page.getByRole('button', { name: 'Show 2 more without a known way to get them' }).click();
	await expect(page.locator('tr', { hasText: 'Lost Chant' })).toContainText('No known source');
	await expect(page.locator('tr', { hasText: 'Forgotten Spark' })).toContainText(
		'Not in normal play'
	);
	await page.goto('/skills/');
	await page.getByRole('searchbox', { name: /name/i }).fill('Lost');
	await expect(page.locator('.entity-name')).toHaveText(['Lost Chant']);
});

test('the skill list filters by discipline in its address and sorts by required level', async ({
	page
}) => {
	await page.goto('/skills/');
	const filters = page.getByRole('complementary', { name: 'Skill filters' });
	await filters.getByRole('checkbox', { name: /^Emberlore\s/ }).check();
	await expect(page).toHaveURL(/discipline=Emberlore/);
	await expect(page.locator('.entity-name')).toHaveText(['Cinder Flick', 'Lanternedge Flare']);
	await page.reload();
	await expect(page.locator('.entity-name')).toHaveText(['Cinder Flick', 'Lanternedge Flare']);
	// A level is a requirement, so the first click lists the lowest first.
	await page.getByRole('button', { name: 'Required Level', exact: true }).click();
	await expect(page.getByRole('columnheader', { name: /Required Level/ })).toHaveAttribute(
		'aria-sort',
		'ascending'
	);
	await page.getByRole('button', { name: 'Required Level', exact: true }).click();
	await expect(page.locator('.entity-name')).toHaveText(['Lanternedge Flare', 'Cinder Flick']);
});

test('a tree skill page names its discipline row, required level and skill point cost', async ({
	page
}) => {
	await page.goto('/skills/lanternedge-flare/');
	const tree = page.locator('section[aria-labelledby="source-skillTree"]');
	await expect(tree).toContainText('A skill costs 1 skill point');
	const row = tree.locator('tr', { hasText: 'Emberlore' });
	await expect(row).toContainText('5');
	await expect(row.getByRole('link', { name: 'Emberlore' })).toHaveAttribute(
		'href',
		'/disciplines/emberlore/#skill-lanternedge-flare'
	);
	await row.getByRole('link', { name: 'Emberlore' }).click();
	await expect(page.locator('#skill-lanternedge-flare')).toBeInViewport();
});

test('a skill the game never offers says so', async ({ page }) => {
	await page.goto('/skills/forgotten-spark/');
	await expect(
		page.getByText('This skill is unused. The game never offers it to you.')
	).toBeVisible();
});

test('a passive link previews each rank cost and leads to its row on the discipline page', async ({
	page
}) => {
	await page.goto('/planner/');
	await expect(page.locator('.planner[data-ready]')).toBeAttached();
	const kindle = page.getByRole('link', { name: 'Kindle' });
	await kindle.hover();
	const preview = page.locator('#entity-preview');
	await expect(preview).toContainText('Kindle');
	await expect(preview).toContainText('5 ranks: 2, 4, 6, 8, 10 points');
	await kindle.click();
	await expect(page).toHaveURL('/disciplines/emberlore/#passive-kindle');
	await expect(page.locator('#passive-kindle')).toBeInViewport();
});

test('a spellsong has its own anchor on its discipline page', async ({ page }) => {
	await page.goto('/disciplines/songcraft/#spellsong-ember-ballad');
	const song = page.locator('#spellsong-ember-ballad');
	await expect(song).toBeInViewport();
	await expect(song).toHaveText('Ember Ballad');
});
