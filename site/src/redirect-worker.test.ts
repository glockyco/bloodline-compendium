import { expect, test } from 'vitest';
import worker from './redirect-worker.js';

test('redirects a page of the shorter address to the same page, keeping its query', () => {
	const response = worker.fetch(
		new Request('https://bloodline.compendiums.org/items/?category=Weapon&sort=damage')
	);
	expect(response.status).toBe(301);
	expect(response.headers.get('Location')).toBe(
		'https://the-bloodline.compendiums.org/items/?category=Weapon&sort=damage'
	);
});

test('redirects the root and an item page without adding or dropping slashes', () => {
	const at = (path: string) =>
		worker.fetch(new Request('https://bloodline.compendiums.org' + path)).headers.get('Location');
	expect(at('/')).toBe('https://the-bloodline.compendiums.org/');
	expect(at('/items/iron-ingot/')).toBe('https://the-bloodline.compendiums.org/items/iron-ingot/');
	expect(at('/sitemap.xml')).toBe('https://the-bloodline.compendiums.org/sitemap.xml');
});
