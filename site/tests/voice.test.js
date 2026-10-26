import { expect, it } from 'vitest';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { voiceGate } from '../../scripts/voice-gate.mjs';

async function page(body, action) {
	const directory = await mkdtemp(join(tmpdir(), 'bloodline-voice-'));
	try {
		await writeFile(
			join(directory, 'index.html'),
			`<!doctype html><html><head><title>A title; with a semicolon</title></head><body>${body}</body></html>`
		);
		await action(directory);
	} finally {
		await rm(directory, { recursive: true, force: true });
	}
}

it('accepts plain site text and leaves the head alone', async () => {
	await page('<p>Sold by 90 merchants.</p>', async (directory) => {
		expect((await voiceGate(directory)).pages).toBe(1);
	});
});

it.each([
	['filler', '<p>Simply browse the list.</p>', 'Filler'],
	['a cute label', '<h2>Lying around</h2>', 'Cute labels'],
	['a defensive aside', '<p>Items we cannot trace yet.</p>', 'Defensive'],
	['an em dash', '<p>Items \u2014 all of them.</p>', 'dashes'],
	['a semicolon', '<p>Sold by merchants; found in chests.</p>', 'Semicolons']
])('rejects %s in the site text', async (_, body, reason) => {
	await page(body, async (directory) => {
		await expect(voiceGate(directory)).rejects.toThrow(reason);
	});
});

it('leaves game text marked with data-game alone', async () => {
	await page(
		'<p data-game>"Just toss these at undead!"</p><p>Given in conversation.</p>',
		async (directory) => {
			expect((await voiceGate(directory)).pages).toBe(1);
		}
	);
});
