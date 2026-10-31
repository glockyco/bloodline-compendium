import type { Fact, ItemPage } from './site-data.js';

/** A row of an item page's Bugs section: a known issue with the tooltip values it explains, or a tooltip value that no issue on the page explains. */
export type BugRow =
	| { kind: 'issue'; issue: string; sentence: string; differences: Fact[] }
	| { kind: 'difference'; fact: Fact };

/** The Bugs rows of an item: its known issues first, each with the tooltip differences it explains, then every other tooltip difference. */
export function bugRows(item: Pick<ItemPage, 'facts' | 'notes'>): BugRow[] {
	const differences = item.facts.filter((fact) => fact.gameShows != null && fact.gameShows !== '');
	const noted = new Set(item.notes.map((note) => note.issue));
	return [
		...item.notes.map((note): BugRow => ({
			kind: 'issue',
			issue: note.issue,
			sentence: note.effect,
			differences: differences.filter((fact) => fact.gameShowsIssue === note.issue)
		})),
		...differences
			.filter((fact) => fact.gameShowsIssue == null || !noted.has(fact.gameShowsIssue))
			.map((fact): BugRow => ({ kind: 'difference', fact }))
	];
}
