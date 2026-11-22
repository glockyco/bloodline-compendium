import { expect, test, type Page } from '@playwright/test';

const level = (page: Page) => page.getByLabel('Character Level');
const discipline = (page: Page) => page.getByLabel('Emberlore Level');

async function open(page: Page) {
	await page.goto('/planner/');
	// The planner reads stored characters and any link once it has hydrated, and marks itself ready.
	await expect(page.locator('.planner[data-ready]')).toBeAttached();
}

test('a character level grants skill and level up points, and an attribute point spends one', async ({
	page
}) => {
	await open(page);
	await level(page).fill('3');
	await level(page).press('Enter');
	await expect(page.getByText('2 of 2 skill points left')).toBeVisible();
	await expect(page.getByText('6 of 6 level up points left')).toBeVisible();
	await page.getByRole('button', { name: 'Add a Fortitude point' }).click();
	await expect(page.getByText('5 of 6 level up points left')).toBeVisible();
	await expect(page.locator('.effects dd', { hasText: 'Health' }).first()).toContainText('175');
});

test('a skill above its level is refused, and lowering a level keeps the skill marked', async ({
	page
}) => {
	await open(page);
	await level(page).fill('6');
	await level(page).press('Enter');
	const flare = page.getByLabel('Unlock Lanternedge Flare');
	await flare.click();
	await expect(page.getByText('Needs Emberlore level 5.')).toBeVisible();
	await expect(flare).not.toBeChecked();
	await discipline(page).fill('5');
	await discipline(page).press('Enter');
	await flare.check();
	await expect(flare).toBeChecked();
	await discipline(page).fill('3');
	await discipline(page).press('Enter');
	await expect(flare).toBeChecked();
	await expect(page.locator('section[aria-labelledby="problems"]')).toContainText(
		'Lanternedge Flare needs Emberlore level 5.'
	);
});

test('a passive refuses a rank past its last', async ({ page }) => {
	await open(page);
	await discipline(page).fill('4');
	await discipline(page).press('Enter');
	await page.getByRole('button', { name: 'Buy a Glow rank' }).click();
	await expect(page.getByLabel('Glow ranks')).toHaveText('1 of 1');
	await expect(page.getByRole('button', { name: 'Buy a Glow rank' })).toBeDisabled();
});

test('a link opens the build as a preview, and saving keeps it as a character', async ({
	page
}) => {
	await open(page);
	await level(page).fill('4');
	await level(page).press('Enter');
	await page.getByLabel('Moss Ward').check();
	await page.getByRole('button', { name: 'Row 1, slot 3: empty' }).click();
	await page
		.getByRole('group', { name: 'Skills for Row 1, slot 3' })
		.getByRole('button', { name: 'Moss Ward' })
		.click();
	await page.getByRole('button', { name: 'Copy Link' }).click();
	const link = await page.getByLabel('Build link').inputValue();
	expect(link).toMatch(/\/planner\/\?build=v1\./);
	await page.getByRole('button', { name: 'Copy as Text' }).click();
	const text = await page.getByLabel('Build as text').inputValue();
	expect(text.split('\n')).toEqual([
		expect.stringContaining('The Bloodline build'),
		'Level 4',
		'Attributes: all at 1',
		'Disciplines: all at 1',
		'Skill bar:',
		expect.stringMatching(/^3\. Moss Ward: .*\/skills\/moss-ward\/$/),
		`Build: ${link}`
	]);

	await page.goto(new URL(link).pathname + new URL(link).search);
	await expect(page.locator('.planner[data-ready]')).toBeAttached();
	await expect(page.getByText('This is a shared build.')).toBeVisible();
	await expect(level(page)).toHaveValue('4');
	await expect(page.getByRole('button', { name: 'Row 1, slot 3: Moss Ward' })).toBeVisible();
	await expect(page.getByLabel('Saved Character')).toHaveValue('');
	await page.getByLabel('Name').fill('Warden');
	await page.getByRole('button', { name: 'Save as New' }).click();
	await open(page);
	await expect(page.getByLabel('Saved Character')).toHaveValue(/.+/);
	await expect(page.getByLabel('Saved Character').locator('option:checked')).toHaveText('Warden');
	await expect(level(page)).toHaveValue('4');
});

test('a malformed link says so and opens a new character', async ({ page }) => {
	await page.goto('/planner/?build=v9.abc');
	await expect(page.locator('.planner[data-ready]')).toBeAttached();
	await expect(page.getByText('This planner reads v1 links, not v9.')).toBeVisible();
	await expect(level(page)).toHaveValue('1');
});

test('a skill page does not load the planner', async ({ page }) => {
	const requests: string[] = [];
	page.on('request', (request) => requests.push(request.url()));
	await page.goto('/skills/cinder-flick/');
	await page.waitForLoadState('networkidle');
	expect(requests.filter((url) => url.includes('planner'))).toEqual([]);
});
