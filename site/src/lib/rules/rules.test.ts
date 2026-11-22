import { describe, expect, it } from 'vitest';
import { experienceRequirement } from './experience.js';
import { attributeEffects } from './attributes.js';
import {
	attributeMismatches,
	levelGrantMismatches,
	proficiencyMismatches,
	rankCostMismatches,
	ruleMismatches
} from './check.js';
import { proficiencyPointsPerLevel } from './progression.js';

describe('experience requirement', () => {
	it('rounds half up to a multiple of 25', () => {
		// (5 + 3) / 4 = 2, 2 ^ 1.35 × 6 = 15.29, so 15 units.
		expect(experienceRequirement.evaluate({ level: 5 })).toMatchObject({ known: true, value: 375 });
	});

	it('is unknown without a level, and names the missing input', () => {
		expect(experienceRequirement.evaluate({})).toEqual({ known: false, missing: ['level'] });
		expect(experienceRequirement.evaluate({ level: Number.NaN })).toEqual({
			known: false,
			missing: ['level']
		});
	});

	it('explains its result in steps that end at the value', () => {
		const result = experienceRequirement.evaluate({ level: 5 });
		if (!result.known) throw new Error('expected a known result');
		expect(result.steps.at(-1)).toContain('375');
	});
});

describe('rule check', () => {
	const record = (required: number) => ({
		class: 'PlayerPassiveLevelStatComponent_C /Game/Level.Level:PersistentLevel.Player.Component',
		function: 'Set Required EXP',
		rows: [{ inputs: { 'Player Ref.Level': 5 }, outputs: { 'Player Ref.Required EXP': required } }]
	});

	it('passes when the game agrees', () => {
		expect(ruleMismatches(experienceRequirement, [record(375)])).toEqual([]);
	});

	it('reports a planted disagreement with both values', () => {
		expect(ruleMismatches(experienceRequirement, [record(400)])).toEqual([
			'experience-requirement for {"level":5}: rule 375, game 400'
		]);
	});
});

describe('proficiency points per level', () => {
	it('gives Necromancy 2, Staff only its first level, and Vampirism none', () => {
		expect(proficiencyPointsPerLevel('Necromancy', 4)).toMatchObject({ value: 2 });
		expect(proficiencyPointsPerLevel('Staff', 1)).toMatchObject({ value: 1 });
		expect(proficiencyPointsPerLevel('Staff', 2)).toMatchObject({ value: 0 });
		expect(proficiencyPointsPerLevel('Vampirism', 1)).toMatchObject({ value: 0 });
		expect(proficiencyPointsPerLevel('Pyromancy', 9)).toMatchObject({ value: 1 });
	});

	it('leaves Blood Magic unknown, because the Blood Levels setting decides it', () => {
		expect(proficiencyPointsPerLevel('Blood Magic', 2)).toMatchObject({ known: false });
	});

	it('compares single level ups only, and reports a wrong amount', () => {
		const step = (from: number, to: number, before: number, after: number) => ({
			changed: {
				'instance.Necromancy Level': [from, to] as [number, number],
				'instance.Necro Points': [before, after] as [number, number]
			}
		});
		const result = proficiencyMismatches([
			{
				args: { action: 'discipline' },
				steps: [step(2, 3, 0, 2), step(3, 9, 2, 14), step(9, 10, 14, 15)]
			}
		]);
		expect(result.compared).toBe(2);
		expect(result.problems).toEqual(['proficiency-points for Necromancy level 9: rule 2, game 1']);
	});
});

const ones = {
	Strength: 1,
	Intelligence: 1,
	Dexterity: 1,
	Endurance: 1,
	Fortitude: 1,
	Constitution: 1,
	Vitality: 1,
	Wisdom: 1,
	Willpower: 1
};

describe('attribute effects', () => {
	it('starts a new character at 150 health, 200 stamina and 100 mana', () => {
		expect(attributeEffects(ones)).toMatchObject({
			known: true,
			value: { health: 150, stamina: 200, mana: 100, healthRegen: 1, pickupStrength: 215 }
		});
	});

	it('adds each point and grows regeneration with the square root of the points above the first', () => {
		const result = attributeEffects({ ...ones, Constitution: 21, Vitality: 21, Endurance: 21 });
		if (!result.known) throw new Error('expected a known result');
		expect(result.value.health).toBe(150 + 20 * 15 + 20 * 10);
		expect(result.value.stamina).toBe(200 + 20 * 15 + 20 * 30);
		expect(result.value.healthRegen.toFixed(2)).toBe('1.31');
		expect(result.value.armorWeightReduction).toBe(28.41);
	});

	it('is unknown above the measured levels and names the attribute', () => {
		expect(attributeEffects({ ...ones, Strength: 22 })).toEqual({
			known: false,
			missing: ['Strength']
		});
	});

	it('compares the level up window at the precision it shows', () => {
		const step = (strength: number, pickup: number) => ({
			widget: {
				levels: { ...ones, Strength: strength },
				texts: { TextBlock_36: `   Pickup Strength: ${pickup}\r\n   Strength EXP bonus: +2.4%` }
			}
		});
		const result = attributeMismatches([
			{ args: { action: 'attributes' }, steps: [step(1, 215), step(1, 230)] }
		]);
		expect(result.compared).toBe(2);
		expect(result.problems).toEqual([
			'attributes at 1/1/1/1/1/1/1/1/1: pickupStrength rule 215, game 230'
		]);
	});
});

describe('character level grants against level ups', () => {
	it('compares each single level up with the grant and skips steps over several levels', () => {
		const step = (level: number, to: number, skill: number, points: number) => ({
			changed: {
				'instance.Player Level': [level, to] as [number, number],
				'instance.Skill Points': [0, skill] as [number, number],
				'instance.Level Up Points': [0, points] as [number, number]
			}
		});
		const result = levelGrantMismatches([
			{ args: { action: 'levels' }, steps: [step(1, 2, 1, 3), step(2, 5, 3, 9), step(5, 6, 1, 2)] }
		]);
		expect(result.compared).toBe(2);
		expect(result.problems).toEqual([
			'level-grant for level 5: rule 1 skill and 3 level up points, game 1 and 2'
		]);
	});
});

describe('passive rank costs against purchases', () => {
	it('compares the points each press took with the rank costs in order', () => {
		const press = (before: number, after: number) => ({
			changed: { 'instance.Necro Points': [before, after] as [number, number] }
		});
		const result = rankCostMismatches(
			[
				{
					args: { action: 'passive', tree: 'Tree', widget: 'Glow' },
					steps: [press(20, 18), press(18, 14), press(14, 9)]
				}
			],
			new Map([['Tree:Glow', { pointCost: 2, costIncrease: 2, ranks: 3 }]])
		);
		expect(result.compared).toBe(3);
		expect(result.problems).toEqual(['next-rank-cost for Glow rank 3: rule 6, game 5']);
	});
});
