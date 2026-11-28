import { expect, it } from 'vitest';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { counts, linkCoverage } from '../../scripts/link-coverage.mjs';

const kinds = [{ kind: 'item', hasPages: true, route: 'items' }];

/** Builds a site with one item page holding `body` and a second page that links the shaft and the stone. */
async function site(body, action) {
	const directory = await mkdtemp(join(tmpdir(), 'bloodline-links-'));
	try {
		await writeFile(join(directory, 'kinds.json'), JSON.stringify(kinds));
		const write = async (path, main) => {
			await mkdir(join(directory, path), { recursive: true });
			await writeFile(
				join(directory, path, 'index.html'),
				`<!doctype html><html><head><title>t</title></head><body><main>${main}</main></body></html>`
			);
		};
		await write('items/core', `<h1>Lightborn Wand Core</h1>${body}`);
		await write(
			'items/other',
			'<h1>Other</h1><p><a data-game href="/items/shaft/">Golden Talon Wand Shaft</a> and <a data-game href="/items/stone/">Arcane</a> and <a data-game href="/items/core/">Lightborn Wand Core</a></p>'
		);
		await action(await linkCoverage(directory));
	} finally {
		await rm(directory, { recursive: true, force: true });
	}
}

it('reports a known name that a row shows without a link', async () => {
	await site('<table><tr><td>Needs a Golden Talon Wand Shaft.</td></tr></table>', (result) => {
		expect(result.unlinked.get('/items/core/')).toEqual(['Golden Talon Wand Shaft']);
	});
});

it('reports game text that is only a linkable name, such as a header’s discipline', async () => {
	await site('<p class="identity"><span data-game>Golden Talon Wand Shaft</span></p>', (result) => {
		expect(result.unlinked.get('/items/core/')).toEqual(['Golden Talon Wand Shaft']);
	});
});

it('accepts a name its own row links, the page’s own name, part of a longer name and game text', async () => {
	await site(
		'<p>Needs a <a data-game href="/items/shaft/">Golden Talon Wand Shaft</a>. A Golden Talon Wand Shaft works.</p>' +
			'<p>Lightborn Wand Core is arcane, unlike Arcane Bolt.</p>' +
			'<p data-game>"A Golden Talon Wand Shaft, said the smith."</p>',
		(result) => {
			expect(result.unlinked.has('/items/core/')).toBe(false);
		}
	);
});

it('counts names without a page by their kind', async () => {
	await site(
		'<p>Sold by <span data-game data-kind="merchant">Brandt Fenric</span> and <span data-game data-kind="merchant">Brandt Fenric</span>.</p>',
		(result) => {
			expect(counts(result).plain).toEqual({ merchant: 1 });
			expect(result.unlinked.get('/items/core/')).toBeUndefined();
		}
	);
});
