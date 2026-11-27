import { expect, test } from 'vitest';
import { scopeReasons } from './reason-scope.js';

const scope = (rows: Record<string, string[]>) =>
	scopeReasons(Object.entries(rows), ([, reasons]) => reasons);

test('moves a reason that every row carries to the line and keeps the rest beside its row', () => {
	const scoped = scope({ pile: ['No spot.'], stone: ['No spot.', 'Method unknown.'] });
	expect(scoped.lines).toEqual(['No spot.']);
	expect(scoped.rows.map(({ row, reasons, band }) => [row[0], reasons, band])).toEqual([
		['pile', [], null],
		['stone', ['Method unknown.'], null]
	]);
});

test('keeps the reason of a single row beside it', () => {
	const scoped = scope({ grove: ['No spot.'] });
	expect(scoped.lines).toEqual([]);
	expect(scoped.rows[0].reasons).toEqual(['No spot.']);
});

test('heads rows that share their reasons with one band after the rows that stand alone', () => {
	const scoped = scope({ a: ['Shared.'], lone: ['Own.'], b: ['Shared.'], bare: [] });
	expect(scoped.lines).toEqual([]);
	expect(scoped.rows.map(({ row, reasons, band }) => [row[0], reasons, band])).toEqual([
		['lone', ['Own.'], null],
		['bare', [], null],
		['a', [], ['Shared.']],
		['b', [], null]
	]);
});
