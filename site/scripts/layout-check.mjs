// Loads the pages whose real data stresses layout most at four widths and reports the layout problems that
// site/tests/layout.ts finds. Fixtures cannot hold these cases, because game data may not enter the repository.
// Usage: bun run layout:check <site data directory> [base URL]
import { readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';
import { chromium } from '@playwright/test';
import { layoutProblems } from '../tests/layout.ts';

const [directory, base = 'http://127.0.0.1:5190'] = process.argv.slice(2);
if (!directory) throw new Error('Usage: bun run layout:check <site data directory> [base URL]');

const pages = [];
for (const name of await readdir(join(directory, 'items'))) {
	pages.push(JSON.parse(await readFile(join(directory, 'items', name), 'utf8')));
}
const top = (score) => [...pages].sort((a, b) => score(b) - score(a))[0].slug;
const routes = new Set([
	'/',
	'/items/',
	'/items/?category=weapon',
	'/items/?category=armor&unknown=1',
	'/known-issues/',
	'/about/',
	'/missing/page',
	`/items/${top((page) => (page.name + (page.qualifier ?? '')).length)}/`,
	`/items/${top((page) => page.sources.length)}/`,
	`/items/${top((page) => Math.max(0, ...page.sources.map((source) => source.places.length)))}/`,
	`/items/${top((page) => page.uses.length)}/`,
	`/items/${top((page) => (page.sources.some((source) => source.price?.currency) ? page.sources.length : 0))}/`,
	`/items/${top((page) => (page.icon ? 0 : 1 + page.facts.length))}/`,
	`/items/${top((page) => page.notes.length)}/`
]);

console.log([...routes].join(' '));
const browser = await chromium.launch();
let failures = 0;
for (const width of [1440, 1024, 768, 390]) {
	const page = await browser.newPage({ viewport: { width, height: 900 } });
	for (const route of routes) {
		await page.goto(base + route);
		for (const problem of await layoutProblems(page)) {
			failures++;
			console.log(`${width}px ${route}: ${problem}`);
		}
	}
	await page.close();
}
await browser.close();
console.log(`Layout: ${routes.size} pages at 4 widths, ${failures} problems.`);
if (failures) process.exitCode = 1;
