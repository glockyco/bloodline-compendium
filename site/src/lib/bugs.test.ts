import { expect, it } from 'vitest';
import { bugRows } from './bugs.js';
import type { Fact } from './site-data.js';

const fact = (stat: Fact['stat'], gameShows: string, gameShowsIssue: string | null): Fact => ({
	effect: null,
	label: stat,
	number: 0,
	stat,
	text: null,
	gameShows,
	gameShowsIssue
});

it('puts each tooltip difference under the issue that explains it', () => {
	const armor = fact('armorRating', '9', 'armor-issue');
	const damage = fact('damage', '20', 'damage-issue');
	const rows = bugRows({
		facts: [damage, armor],
		notes: [
			{ issue: 'armor-issue', effect: 'Wearing it adds no armor.' },
			{ issue: 'damage-issue', effect: 'It deals less damage.' }
		]
	});

	expect(rows).toEqual([
		{
			kind: 'issue',
			issue: 'armor-issue',
			sentence: 'Wearing it adds no armor.',
			differences: [armor]
		},
		{
			kind: 'issue',
			issue: 'damage-issue',
			sentence: 'It deals less damage.',
			differences: [damage]
		}
	]);
});

it('gives a tooltip difference that no issue on the page explains a row of its own', () => {
	const unexplained = fact('damage', '2', null);
	const elsewhere = fact('armorRating', '4', 'not-noted');
	const rows = bugRows({
		facts: [unexplained, elsewhere],
		notes: [{ issue: 'point-blank', effect: 'Its projectile deals 100 damage.' }]
	});

	expect(rows).toEqual([
		{
			kind: 'issue',
			issue: 'point-blank',
			sentence: 'Its projectile deals 100 damage.',
			differences: []
		},
		{ kind: 'difference', fact: unexplained },
		{ kind: 'difference', fact: elsewhere }
	]);
});

it('has no rows for an item without issues or tooltip differences', () => {
	expect(bugRows({ facts: [{ ...fact('value', '', null), gameShows: null }], notes: [] })).toEqual(
		[]
	);
});
