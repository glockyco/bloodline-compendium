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

const fresh = newCharacter(data);
const build: PlannerState = {
	...fresh,
	attributes: { level: 12, points: { ...fresh.attributes.points, Fortitude: 4 } },
	skills: {
		...fresh.skills,
		disciplines: { '/Invented/FireTree': 6 },
		unlocked: ['/Invented/Ember']
	},
	skillBars: {
		offhand: [null, null, null, null, null, null, null, null, null, '/Invented/Ember'],
		main: ['/Invented/Ember', null, null, null, null, null, null, null, null, null]
	}
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
		expect((await decodeBuild(value, renamed)).state.skills.unlocked).toEqual(['/Invented/Ember']);
	});

	it('keeps a key the payload lacks as unresolved and writes it back the same', async () => {
		const value = await encodeBuild(build);
		const without = { ...data, skills: [], disciplines: [{ ...data.disciplines[0], slots: [] }] };
		const decoded = await decodeBuild(value, without);
		expect(decoded.unresolved).toHaveLength(1);
		expect(decoded.state.skills.unlocked[0].startsWith(unresolvedPrefix)).toBe(true);
		expect(decoded.state.skillBars.main[0]).toBe(decoded.state.skills.unlocked[0]);
		expect((await decodeBuild(await encodeBuild(decoded.state), data)).state).toEqual(build);
	});

	it('opens a link from a later planner and passes its unknown sections on', async () => {
		const later = { ...build, later: { e: { h: 'Invented Helm' } } };
		const decoded = await decodeBuild(await encodeBuild(later), data);
		expect(decoded.state).toEqual(later);
		expect((await decodeBuild(await encodeBuild(decoded.state), data)).state).toEqual(later);
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
