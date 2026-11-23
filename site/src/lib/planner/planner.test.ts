import { describe, expect, it } from 'vitest';
import type { PlannerData } from '../site-data.js';
import { evaluate } from './evaluate.js';
import {
	buyRank,
	newCharacter,
	placeOnHotbar,
	setAttribute,
	setBloodMagicPoints,
	setDisciplineLevel,
	setLevel,
	toggleLearned,
	toggleSkill,
	type Edit
} from './state.js';

const fire = '/Invented/FireTree';
const blood = '/Invented/BloodTree';
const data: PlannerData = {
	build: 1,
	format: 1,
	disciplines: [
		{
			key: fire,
			slug: 'fire',
			name: 'Fire',
			icon: null,
			category: 'Magic',
			levelName: 'Pyromancy',
			slots: [
				{ row: 1, skill: 'Ember', requiredLevel: 1, levelDiscipline: fire },
				{ row: 5, skill: 'Blaze', requiredLevel: 5, levelDiscipline: fire }
			],
			passives: [
				{
					key: `${fire}:Glow`,
					anchor: 'passive-glow',
					name: 'Glow',
					icon: null,
					requiredLevel: 1,
					levelDiscipline: fire,
					pointCost: 1,
					costIncrease: 1,
					ranks: 2
				}
			]
		},
		{
			key: blood,
			slug: 'blood',
			name: 'Blood',
			icon: null,
			category: 'Magic',
			levelName: 'Blood Magic',
			slots: [],
			passives: [
				{
					key: `${blood}:Thirst`,
					anchor: 'passive-thirst',
					name: 'Thirst',
					icon: null,
					requiredLevel: 1,
					levelDiscipline: blood,
					pointCost: 2,
					costIncrease: 2,
					ranks: 3
				}
			]
		}
	],
	skills: [
		{ key: 'Ember', slug: 'ember', name: 'Ember', icon: null, learned: false },
		{ key: 'Blaze', slug: 'blaze', name: 'Blaze', icon: null, learned: false },
		{ key: 'Tome', slug: 'tome', name: 'Tome', icon: null, learned: true }
	]
};

/** Applies edits in order and fails on the first one the planner refuses. */
function apply(...edits: ((state: Edit['state']) => Edit)[]) {
	return edits.reduce((state, edit) => {
		const result = edit(state);
		if (result.refused) throw new Error(result.refused);
		return result.state;
	}, newCharacter(data));
}

describe('planner', () => {
	it('keeps skill and level up points apart, and grants each per character level', () => {
		const at10 = evaluate(
			apply((s) => setLevel(s, 10)),
			data
		);
		const at11 = evaluate(
			apply((s) => setLevel(s, 11)),
			data
		);
		expect(at11.skillPoints.remaining! - at10.skillPoints.remaining!).toBe(1);
		expect(at11.levelUpPoints.remaining! - at10.levelUpPoints.remaining!).toBe(3);
	});

	it('spends a passive rank from its own discipline only', () => {
		const state = apply(
			(s) => setLevel(s, 5),
			(s) => setDisciplineLevel(s, fire, 3),
			(s) => buyRank(s, data, `${fire}:Glow`)
		);
		const result = evaluate(state, data);
		expect(result.proficiency[fire]).toEqual({ earned: 2, spent: 1, remaining: 1 });
		expect(result.skillPoints.spent).toBe(0);
		expect(result.levelUpPoints.spent).toBe(0);
	});

	it('refuses a skill above the planned level and names the level it needs', () => {
		const state = apply((s) => setLevel(s, 5));
		const result = toggleSkill(state, data, 'Blaze');
		expect(result.refused).toBe('Needs Fire level 5.');
		expect(result.state.skills.unlocked).toEqual([]);
	});

	it('refuses a rank beyond the last and changes no balance', () => {
		const state = apply(
			(s) => setDisciplineLevel(s, fire, 10),
			(s) => buyRank(s, data, `${fire}:Glow`),
			(s) => buyRank(s, data, `${fire}:Glow`)
		);
		const result = buyRank(state, data, `${fire}:Glow`);
		expect(result.refused).toBe('Every rank is bought.');
		expect(evaluate(result.state, data)).toEqual(evaluate(state, data));
	});

	it('spends entered Blood Magic points, which no level sets', () => {
		const state = apply(
			(s) => setBloodMagicPoints(s, 4),
			(s) => buyRank(s, data, `${blood}:Thirst`)
		);
		expect(evaluate(state, data).proficiency[blood]).toEqual({ earned: 4, spent: 2, remaining: 2 });
		expect(evaluate(newCharacter(data), data).proficiency[blood].remaining).toBeNull();
	});

	it('shows 175 health after one Fortitude point at the start', () => {
		const state = apply(
			(s) => setLevel(s, 2),
			(s) => setAttribute(s, 'Fortitude', 2)
		);
		const result = evaluate(state, data);
		expect(result.effects?.health).toBe(175);
		expect(result.levelUpPoints.remaining).toBe(2);
		expect(setAttribute(state, 'Fortitude', 0).refused).toBeDefined();
	});

	it('keeps a skill that a lowered level no longer allows, marks it and shows the build as invalid', () => {
		const state = apply(
			(s) => setLevel(s, 5),
			(s) => setDisciplineLevel(s, fire, 5),
			(s) => toggleSkill(s, data, 'Blaze'),
			(s) => setDisciplineLevel(s, fire, 3)
		);
		const result = evaluate(state, data);
		expect(state.skills.unlocked).toContain('Blaze');
		expect(result.diagnostics).toContainEqual({
			kind: 'level',
			entry: 'Blaze',
			discipline: fire,
			needs: 5
		});
		expect(result.valid).toBe(false);
	});

	it('keeps a skill on the hotbar slot it was placed on, and a learned skill costs no skill point', () => {
		const state = apply(
			(s) => toggleLearned(s, data, 'Tome'),
			(s) => placeOnHotbar(s, 2, 'Tome')
		);
		expect(state.skillBars.main[2]).toBe('Tome');
		expect(evaluate(state, data).skillPoints.spent).toBe(0);
		expect(placeOnHotbar(state, 3, 'Ember').refused).toBeDefined();
	});

	it('lets a skill sit on both bars, and a refunded skill leaves both', () => {
		const state = apply(
			(s) => setLevel(s, 2),
			(s) => toggleSkill(s, data, 'Ember'),
			(s) => placeOnHotbar(s, 0, 'Ember'),
			(s) => placeOnHotbar(s, 4, 'Ember', 'offhand')
		);
		expect(state.skillBars.main[0]).toBe('Ember');
		expect(state.skillBars.offhand[4]).toBe('Ember');
		const refunded = apply(
			() => ({ state }),
			(s) => toggleSkill(s, data, 'Ember')
		);
		expect(refunded.skillBars.main).not.toContain('Ember');
		expect(refunded.skillBars.offhand).not.toContain('Ember');
	});
});
