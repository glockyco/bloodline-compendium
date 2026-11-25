import { expect, it } from 'vitest';
import { bugRows } from './bugs.js';
import type { Fact } from '../site-data.js';

const fact = (
	stat: Fact['stat'],
	tooltipLabel: string,
	gameShows: string,
	gameShowsIssue: string | null
): Fact => ({
	effect: null,
	label: stat,
	tooltipLabel,
	number: 0,
	stat,
	text: null,
	gameShows,
	gameShowsIssue
});

it('names the tooltip values an issue explains in its sentence, as the game prints them', () => {
	const armor = fact('armorRating', 'Armor', '9', 'armor-issue');
	const type = fact('armorType', 'Type', 'Light', 'armor-issue');
	const damage = fact('damage', 'Damage', '20', 'damage-issue');
	const rows = bugRows({
		facts: [damage, armor, type],
		notes: [
			{ issue: 'armor-issue', effect: 'Wearing it adds no armor.' },
			{ issue: 'damage-issue', effect: 'It deals less damage.' }
		]
	});

	expect(rows.map((row) => row.sentence)).toEqual([
		'Wearing it adds no armor, although the tooltip shows ARMOR: 9 and Type: Light.',
		'It deals less damage, although the tooltip shows Damage: 20.'
	]);
});

it('keeps the workaround outside the effect and its tooltip comparison', () => {
	const rows = bugRows({
		facts: [fact('armorRating', 'Armor', '7', 'knee-issue')],
		notes: [
			{
				issue: 'knee-issue',
				effect: 'Equipping it leaves Armor unchanged.',
				workaround: 'Take off another piece of armor.'
			}
		]
	});
	expect(rows).toEqual([
		{
			kind: 'issue',
			issue: 'knee-issue',
			sentence: 'Equipping it leaves Armor unchanged, although the tooltip shows ARMOR: 7.',
			workaround: 'Take off another piece of armor.'
		}
	]);
});

it('gives a tooltip difference that no issue on the page explains a row of its own', () => {
	const unexplained = fact('damage', 'Damage', '2', null);
	const rows = bugRows({
		facts: [unexplained],
		notes: [{ issue: 'point-blank', effect: 'Its projectile deals 100 damage.' }]
	});

	expect(rows).toEqual([
		{ kind: 'issue', issue: 'point-blank', sentence: 'Its projectile deals 100 damage.' },
		{
			kind: 'difference',
			fact: unexplained,
			sentence: 'The tooltip shows Damage: 2, but the game applies 0.'
		}
	]);
});

it('quotes a tooltip value read as a whole line as it stands', () => {
	const rows = bugRows({
		facts: [],
		notes: [
			{
				issue: 'armor-without-row',
				effect: 'Wearing it adds no armor.',
				tooltipValues: { 'Armor type': 'Type: Light', Damage: '15' }
			}
		]
	});
	expect(rows[0].sentence).toBe(
		'Wearing it adds no armor, although the tooltip shows Type: Light and Damage: 15.'
	);
});

it('has no rows for an item without issues or tooltip differences', () => {
	expect(
		bugRows({ facts: [{ ...fact('value', '', '', null), gameShows: null }], notes: [] })
	).toEqual([]);
});
