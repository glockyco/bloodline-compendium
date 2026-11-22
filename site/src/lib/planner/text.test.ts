import { describe, expect, it } from 'vitest';
import type { PlannerData } from '../site-data.js';
import { newCharacter, placeOnHotbar, toggleLearned, type PlannerState } from './state.js';
import { buildText } from './text.js';

const data: PlannerData = {
	build: 7,
	format: 1,
	disciplines: [
		{
			key: 'Fire',
			slug: 'fire',
			name: 'Fire',
			icon: null,
			category: 'Magic',
			levelName: 'Pyromancy',
			slots: [],
			passives: []
		}
	],
	skills: ['Ember', 'Blaze', 'Spark'].map((name) => ({
		key: name,
		slug: name.toLowerCase(),
		name,
		icon: null,
		learned: true
	}))
};

describe('build text', () => {
	it('lists the skill bar in order with each page, then the build link', () => {
		let state: PlannerState = { ...newCharacter(data), level: 12, disciplines: { Fire: 9 } };
		for (const [slot, skill] of [
			[4, 'Spark'],
			[0, 'Ember'],
			[2, 'Blaze']
		] as const)
			state = placeOnHotbar(toggleLearned(state, data, skill).state, slot, skill).state;
		const text = buildText(
			state,
			data,
			'https://example.org/planner/?build=v1.x',
			(slug) => `https://example.org/skills/${slug}/`
		);
		expect(text.split('\n')).toEqual([
			'The Bloodline build, game build 7',
			'Level 12',
			'Attributes: all at 1',
			'Disciplines: Fire 9',
			'Skill bar:',
			'1. Ember: https://example.org/skills/ember/',
			'3. Blaze: https://example.org/skills/blaze/',
			'5. Spark: https://example.org/skills/spark/',
			'Build: https://example.org/planner/?build=v1.x'
		]);
	});
});
