import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';
import { layoutProblems } from './layout.js';

const routes = ['/design/', '/', '/items/', '/items/lanternedge/', '/known-issues/', '/about/'];

for (const width of [390, 768, 1280]) {
	test.describe(`layout at ${width}px`, () => {
		test.use({ viewport: { width, height: 900 } });
		for (const route of routes)
			test(`has no layout problems on ${route}`, async ({ page }) => {
				await page.goto(route);
				expect(await layoutProblems(page)).toEqual([]);
			});
	});
}

test.describe('planted defects on the fixture page', () => {
	test.use({ viewport: { width: 1280, height: 900 } });
	for (const [defect, css, message] of [
		['content leaving a card', '[data-ui=slots] { width: 150%; }', 'leaves'],
		[
			'misaligned card rows',
			'[data-ui=card]:first-child > * { display: flex; flex-direction: column; }',
			'starts at'
		],
		['text below the smallest step', '[data-ui=card] h3 { font-size: 12px; }', 'below'],
		['a page wider than the window', 'main { min-width: 2000px; }', 'scrolls sideways']
	])
		test(`reports ${defect}`, async ({ page }) => {
			await page.goto('/design/');
			await page.addStyleTag({ content: css });
			expect((await layoutProblems(page)).join('\n')).toContain(message);
		});

	test('reports a cell off the shared inset and a comma without a space', async ({ page }) => {
		await page.goto('/items/lanternedge/');
		await page.addStyleTag({ content: '.relation-panel td { padding-left: 2px !important; }' });
		await page.evaluate(() => {
			const places = document.querySelector('.source-places');
			if (places) places.textContent = 'Glass Reach,Amber Coast';
		});
		const problems = (await layoutProblems(page)).join('\n');
		expect(problems).toContain('instead of');
		expect(problems).toContain('comma without a space');
	});

	test('reports low contrast through axe', async ({ page }) => {
		await page.goto('/design/');
		await page.addStyleTag({ content: '[data-ui=card] .card-text { color: #2a2433; }' });
		const result = await new AxeBuilder({ page }).withRules(['color-contrast']).analyze();
		expect(result.violations.map((violation) => violation.id)).toContain('color-contrast');
	});
});
