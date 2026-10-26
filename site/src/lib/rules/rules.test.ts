import { describe, expect, it } from 'vitest';
import { experienceRequirement } from './experience.js';
import { ruleMismatches } from './check.js';

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
