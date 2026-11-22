import { formatNumber } from '../format.js';
import type { Fact, IssueNote } from '../site-data.js';

/** A row of an entity page's Bugs section: a known issue's sentence about the entity, or a tooltip value that no issue on the page explains. */
export type BugRow =
	| { kind: 'issue'; issue: string; sentence: string }
	| { kind: 'difference'; fact: Fact; sentence: string };

/** A tooltip value as the game prints it, such as "ARMOR: 4" or "Damage: 20". */
function tooltipText(label: string, value: string): string {
	return `${label === 'Armor' ? 'ARMOR' : label}: ${value}`;
}

function joinAnd(parts: string[]): string {
	return parts.length < 2
		? (parts[0] ?? '')
		: `${parts.slice(0, -1).join(', ')} and ${parts.at(-1)}`;
}

/** The Bugs rows of an entity: its known issues first, each naming the tooltip values it explains, then every other tooltip difference. */
export function bugRows(item: { facts: Fact[]; notes: IssueNote[] }): BugRow[] {
	const differences = item.facts.filter((fact) => fact.gameShows != null && fact.gameShows !== '');
	const noted = new Set(item.notes.map((note) => note.issue));
	return [
		...item.notes.map((note): BugRow => {
			const shown = [
				...differences
					.filter((fact) => fact.gameShowsIssue === note.issue)
					.map((fact) => tooltipText(fact.tooltipLabel ?? fact.label, fact.gameShows!)),
				// A tooltip value read as a whole line, such as "Type: Light", carries its own label.
				...Object.entries(note.tooltipValues ?? {}).map(([label, value]) =>
					/^[^:]+: /.test(value) ? value : tooltipText(label, value)
				)
			];
			// The game card shows applied values, so the sentence names the tooltip's value as the contrast.
			const sentence = shown.length
				? `${note.effect.replace(/\.$/, '')}, although the tooltip shows ${joinAnd(shown)}.`
				: note.effect;
			return { kind: 'issue', issue: note.issue, sentence };
		}),
		...differences
			.filter((fact) => fact.gameShowsIssue == null || !noted.has(fact.gameShowsIssue))
			.map((fact): BugRow => {
				const applied = fact.number != null ? formatNumber(fact.number) : (fact.value ?? 'none');
				return {
					kind: 'difference',
					fact,
					sentence: `The tooltip shows ${tooltipText(fact.tooltipLabel ?? fact.label, fact.gameShows!)}, but the game applies ${applied}.`
				};
			})
	];
}
