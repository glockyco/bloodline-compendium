import { expect, test, type Page } from '@playwright/test';

const level = (page: Page) => page.getByLabel('Character Level');
const discipline = (page: Page) => page.getByLabel('Emberlore Level');

const total = (page: Page, name: string) =>
	page.locator('.totals div', { has: page.getByText(name, { exact: true }) }).locator('dd');
const tab = (page: Page, name: string) => page.getByRole('tab', { name }).click();

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
	await expect(total(page, 'Skill Points Left')).toHaveText('2 of 2');
	await expect(total(page, 'Level Up Points Left')).toHaveText('6 of 6');
	await tab(page, 'Attributes');
	await page.getByRole('button', { name: 'Add a Fortitude point' }).click();
	await expect(total(page, 'Level Up Points Left')).toHaveText('5 of 6');
	await expect(total(page, 'Health')).toHaveText('175');
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
	await tab(page, 'Skill Bars');
	await page.getByRole('button', { name: 'Row 1, slot 3: empty', exact: true }).click();
	await page
		.getByRole('group', { name: 'Skills for Row 1, slot 3', exact: true })
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
	await tab(page, 'Skill Bars');
	await expect(
		page.getByRole('button', { name: 'Row 1, slot 3: Moss Ward', exact: true })
	).toBeVisible();
	await expect(page.getByLabel('Saved Character')).toHaveValue('');
	await page.getByLabel('Name').fill('Warden');
	await page.getByRole('button', { name: 'Save as New' }).click();
	await open(page);
	await expect(page.getByLabel('Saved Character')).toHaveValue(/.+/);
	await expect(page.getByLabel('Saved Character').locator('option:checked')).toHaveText('Warden');
	await expect(level(page)).toHaveValue('4');
});

test('the skill bar works from the keyboard and places taught skills', async ({ page }) => {
	await open(page);
	await tab(page, 'Skill Bars');
	const slot = page.getByRole('button', { name: 'Row 2, slot 1: empty', exact: true });
	await slot.press('Enter');
	// Opening a slot moves focus to its search field, Enter places the first match, and focus returns to the slot.
	const search = page.getByRole('searchbox', { name: 'Search skills' });
	await expect(search).toBeFocused();
	const chooser = page.getByRole('group', { name: 'Skills for Row 2, slot 1', exact: true });
	await expect(chooser.getByRole('heading', { name: 'Taught by a Book or Event' })).toBeVisible();
	await search.fill('flare');
	await expect(chooser.getByRole('button')).toHaveText(['Lanternedge Flare']);
	await search.press('Enter');
	const placed = page.getByRole('button', {
		name: 'Row 2, slot 1: Lanternedge Flare',
		exact: true
	});
	await expect(placed).toBeFocused();
	// A taught skill on the bar costs no skill point, so the build stays valid at level 1.
	await expect(page.locator('section[aria-labelledby="problems"]')).toHaveCount(0);
	await placed.press('Enter');
	await expect(chooser.getByRole('heading', { name: 'Known' })).toBeVisible();
	await page.keyboard.press('Escape');
	await expect(placed).toBeFocused();
	await expect(chooser).toHaveCount(0);
});

test('each section is a tab with its own address, and a phone switches between skills and passives', async ({
	page
}) => {
	await page.goto('/planner/#skill-bars');
	await expect(page.locator('.planner[data-ready]')).toBeAttached();
	const bars = page.getByRole('tab', { name: 'Skill Bars' });
	await expect(bars).toHaveAttribute('aria-selected', 'true');
	await expect(
		page.getByRole('button', { name: 'Row 1, slot 1: empty', exact: true })
	).toBeVisible();
	await expect(page.getByRole('button', { name: 'Offhand row 1, slot 1: empty' })).toBeVisible();
	await bars.press('ArrowLeft');
	await expect(page.getByRole('tab', { name: 'Skills' })).toBeFocused();
	await expect(page).toHaveURL(/#skills$/);
	// Wide enough, a discipline shows its skills and passives side by side.
	await expect(page.getByLabel('Unlock Lanternedge Flare')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Buy a Glow rank' })).toBeVisible();

	await page.setViewportSize({ width: 390, height: 844 });
	await expect(page.getByRole('button', { name: 'Buy a Glow rank' })).toBeHidden();
	await page.getByRole('button', { name: 'Passives', exact: true }).click();
	await expect(page.getByRole('button', { name: 'Buy a Glow rank' })).toBeVisible();
	await expect(page.getByLabel('Unlock Lanternedge Flare')).toBeHidden();
});

test('tabs that do not fit become a dropdown of the same sections', async ({ page }) => {
	await page.setViewportSize({ width: 390, height: 844 });
	await open(page);
	await expect(page.getByRole('tab', { name: 'Skill Bars' })).toBeVisible();
	await page.setViewportSize({ width: 280, height: 844 });
	const choose = page.getByLabel('Character sections');
	await expect(choose).toBeVisible();
	await expect(page.getByRole('tab')).toHaveCount(0);
	await choose.selectOption('skill-bars');
	await expect(
		page.getByRole('button', { name: 'Row 1, slot 1: empty', exact: true })
	).toBeVisible();
	await expect(page).toHaveURL(/#skill-bars$/);
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

test('pages show the active character against their levels, and stay neutral without one', async ({
	page
}) => {
	await page.goto('/skills/lanternedge-flare/');
	await page.waitForLoadState('networkidle');
	await expect(page.locator('.cue')).toHaveCount(0);
	await page.evaluate(() =>
		localStorage.setItem(
			'bloodline-planner',
			JSON.stringify({
				version: 1,
				active: 'a',
				characters: [
					{
						id: 'a',
						name: 'Warden',
						state: {
							build: 90000001,
							attributes: {
								level: 5,
								points: Object.fromEntries(
									[
										'Strength',
										'Intelligence',
										'Dexterity',
										'Endurance',
										'Fortitude',
										'Constitution',
										'Vitality',
										'Wisdom',
										'Willpower'
									].map((attribute) => [attribute, 1])
								)
							},
							skills: {
								disciplines: { '/Invented/Trees/Emberlore': 3 },
								unlocked: ['/Invented/Skills/CinderFlick'],
								learned: [],
								passives: {},
								bloodMagicPoints: null
							},
							skillBars: { main: Array(10).fill(null) },
							later: {}
						}
					}
				]
			})
		)
	);
	await page.reload();
	await expect(page.locator('.cue')).toHaveText(
		'Warden needs Emberlore level 5 for this skill. It is at level 3. Open in the Character Planner'
	);
	await page.goto('/disciplines/emberlore/');
	await expect(page.locator('.cue')).toHaveText(
		'Warden has Emberlore at level 3, with 1 of its 2 skills unlocked. Open in the Character Planner'
	);
});
