import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

for (const width of [1280, 390]) {
	test.describe(`accessibility at ${width}px`, () => {
		test.use({ viewport: { width, height: 900 } });

		for (const route of [
			'/',
			'/items/',
			'/items/lanternedge/',
			'/items/hollow-prism/',
			'/coverage/',
			'/about/',
			'/missing/accessibility-probe',
			'/design/'
		]) {
			test(`has no axe violations on ${route}`, async ({ page }) => {
				await page.goto(route);
				await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
				const result = await new AxeBuilder({ page })
					.withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'best-practice'])
					.analyze();
				expect(result.violations).toEqual([]);
			});
		}

		test('expanded acquisition and coverage tables remain accessible', async ({ page }) => {
			for (const route of ['/items/lanternedge/', '/coverage/']) {
				await page.goto(route);
				for (const summary of await page.locator('details > summary').all()) {
					await summary.click();
				}
				const result = await new AxeBuilder({ page })
					.withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'best-practice'])
					.analyze();
				expect(result.violations).toEqual([]);
			}
		});
	});
}
