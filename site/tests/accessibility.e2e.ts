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
			'/known-issues/',
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

		test('expanded acquisition tables and issue details remain accessible', async ({ page }) => {
			for (const route of ['/items/lanternedge/', '/known-issues/']) {
				await page.goto(route);
				if (route === '/items/lanternedge/') {
					// Hydration replaces the static disclosure, so expand the table through its live control.
					const more = page.getByRole('button', { name: 'Show 5 more in Sold by', exact: true });
					await more.click();
					await expect(
						page.getByRole('button', { name: 'Show fewer in Sold by', exact: true })
					).toHaveAttribute('aria-expanded', 'true');
					await expect(
						page.getByRole('table', { name: 'Sold by continued', exact: true })
					).toBeVisible();
				}
				await expect(page.locator('details > summary').first()).toBeVisible();
				for (const summary of await page.locator('details > summary').all()) {
					await summary.click();
				}
				await expect(page.locator('details:not([open])')).toHaveCount(0);
				const result = await new AxeBuilder({ page })
					.withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'best-practice'])
					.analyze();
				expect(result.violations).toEqual([]);
			}
		});
	});
}
