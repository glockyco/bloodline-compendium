import { describe, expect, it } from 'vitest';
import type { PlannerData } from '../site-data.js';
import {
	decodeBuild,
	digestIndex,
	encodeBuild,
	LinkError,
	maxLinkLength,
	unresolvedPrefix
} from './link.js';
import { newCharacter, type PlannerState } from './state.js';

const data: PlannerData = {
	build: 7,
	format: 1,
	disciplines: [
		{
			key: '/Invented/FireTree',
			slug: 'fire',
			name: 'Fire',
			icon: null,
			category: 'Magic',
			levelName: 'Pyromancy',
			slots: [
				{
					row: 1,
					skill: '/Invented/Ember',
					requiredLevel: 1,
					levelDiscipline: '/Invented/FireTree'
				}
			],
			passives: []
		}
	],
	skills: [{ key: '/Invented/Ember', slug: 'ember', name: 'Ember', icon: null, learned: false }]
};

const build: PlannerState = {
	...newCharacter(data),
	level: 12,
	disciplines: { '/Invented/FireTree': 6 },
	attributes: { ...newCharacter(data).attributes, Fortitude: 4 },
	skills: ['/Invented/Ember'],
	hotbar: ['/Invented/Ember', null, null, null, null, null, null, null, null, null]
};

async function raw(json: string) {
	const stream = new Blob([new TextEncoder().encode(json)])
		.stream()
		.pipeThrough(new CompressionStream('deflate-raw'));
	const bytes = new Uint8Array(await new Response(stream).arrayBuffer());
	return (
		'v1.' +
		btoa(String.fromCharCode(...bytes))
			.replace(/\+/g, '-')
			.replace(/\//g, '_')
			.replace(/=+$/, '')
	);
}

describe('build links', () => {
	it('round-trips a build and keeps the hotbar order', async () => {
		const value = await encodeBuild(build, 'https://example.org');
		expect(value.startsWith('v1.')).toBe(true);
		expect(`https://example.org/planner/?build=${value}`.length).toBeLessThanOrEqual(maxLinkLength);
		expect(await decodeBuild(value, data)).toEqual({ state: build, unresolved: [] });
	});

	it('keeps a renamed skill by its key', async () => {
		const value = await encodeBuild(build);
		const renamed = { ...data, skills: [{ ...data.skills[0], name: 'Cinder' }] };
		expect((await decodeBuild(value, renamed)).state.skills).toEqual(['/Invented/Ember']);
	});

	it('keeps a key the payload lacks as unresolved and writes it back the same', async () => {
		const value = await encodeBuild(build);
		const without = { ...data, skills: [], disciplines: [{ ...data.disciplines[0], slots: [] }] };
		const decoded = await decodeBuild(value, without);
		expect(decoded.unresolved).toHaveLength(1);
		expect(decoded.state.skills[0].startsWith(unresolvedPrefix)).toBe(true);
		expect(decoded.state.hotbar[0]).toBe(decoded.state.skills[0]);
		expect((await decodeBuild(await encodeBuild(decoded.state), data)).state).toEqual(build);
	});

	it('refuses other versions, oversized data and data no planner wrote', async () => {
		await expect(decodeBuild('v2.abc', data)).rejects.toMatchObject({ kind: 'version' });
		await expect(decodeBuild('v1.' + 'a'.repeat(2000), data)).rejects.toMatchObject({
			kind: 'size'
		});
		await expect(decodeBuild(await raw(' '.repeat(20000)), data)).rejects.toMatchObject({
			kind: 'size'
		});
		await expect(decodeBuild(await raw('{"b":1}'), data)).rejects.toMatchObject({
			kind: 'malformed'
		});
		await expect(decodeBuild('v1.!!', data)).rejects.toBeInstanceOf(LinkError);
	});

	it('fails publication when two keys share a digest', () => {
		const clash = {
			...data,
			skills: [
				{ ...data.skills[0], key: '/Invented/K1844' },
				{ ...data.skills[0], key: '/Invented/K110540' }
			]
		};
		expect(() => digestIndex(clash)).toThrow('share the link digest');
	});
});
