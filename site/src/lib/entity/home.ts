import { kindPath } from '../entities.js';
import type { EntityKind, EntityKindDefinition } from '../site-data.js';

/** A tile of a home section, linking a list filter or a page. */
export interface HomeTile {
	href: string;
	icon: string | null;
	name: string;
	meta: string;
	/** Whether the name is the game's own, which reads in the game's typeface. */
	game: boolean;
}

/** A kind's section on the home page: its tiles and a link to all of them. */
export interface HomeSection {
	kind: EntityKind;
	/** The section's heading id, from the kind's route, such as "items-heading". */
	id: string;
	title: string;
	all: { href: string; text: string };
	tiles: HomeTile[];
}

/** What a kind supplies for its home section: the registry gives the rest. */
export type HomeSectionBody = Omit<HomeSection, 'kind' | 'id' | 'title'>;

/**
 * The home page's sections in the registry's order, one for each kind with pages that offers one, headed by the
 * kind's list label.
 */
export function homeSections(
	kinds: EntityKindDefinition[],
	offered: Partial<
		Record<
			EntityKind,
			(kinds: EntityKindDefinition[]) => Omit<HomeSection, 'kind' | 'id' | 'title'>
		>
	>
): HomeSection[] {
	return kinds
		.filter((kind) => kind.hasPages && offered[kind.kind])
		.map((kind) => ({
			kind: kind.kind,
			id: kind.route + '-heading',
			title: kind.searchFilter,
			...offered[kind.kind]!(kinds)
		}));
}

/** The hero's buttons: a list of each of the given kinds, labelled from the registry. */
export function homeActions(
	kinds: EntityKindDefinition[],
	shown: EntityKind[]
): { href: string; label: string }[] {
	return shown.flatMap((shownKind) => {
		const kind = kinds.find((entry) => entry.kind === shownKind && entry.hasPages);
		const href = kind && kindPath(kind.kind, kinds);
		return kind && href ? [{ href, label: 'Browse ' + kind.searchFilter }] : [];
	});
}
