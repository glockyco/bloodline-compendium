import type { ListCell, ListColumn, ListFacet } from '../site-data.js';

/**
 * A row of an entity list. `category` is the row's primary group, which decides the filters and columns that apply
 * to it. `placeSources` lists, for each value of the row's place facet, the ways to get it there.
 */
export interface ListRow {
	key: string;
	name: string;
	status: string;
	category: string;
	facets: Record<string, string[]>;
	cells: Record<string, ListCell>;
	placeSources: string[][];
	versionLabel?: string | null;
}

/** A column every row has, read from the row's own field of the same id, such as an item's type or value. */
export interface FixedColumn {
	id: string;
	numeric: boolean;
}

/**
 * How a list shows a fixed column: before or after the category columns, its text for a row, and whether that text
 * is the game's own wording.
 */
export interface FixedDisplay<R> {
	id: string;
	label: string;
	numeric: boolean;
	position: 'start' | 'end';
	text: (row: R) => string;
	game?: boolean;
}

/** The filters, columns and fixed columns of a list, which address state reads and writes. */
export interface ListShape {
	facets: ListFacet[];
	columns: ListColumn[];
	fixed: FixedColumn[];
}

/** What a kind's list offers: its shape, and how it narrows the columns it shows. */
export interface ListModel<R extends ListRow = ListRow> extends ListShape {
	/** Removes columns that the shown rows make pointless, applied after the category rules. */
	prune?: (shown: R[], columns: ListColumn[]) => ListColumn[];
}

/** A stat filter: a numeric column and its inclusive bounds as typed, where an empty bound is open. */
export interface StatFilter {
	column: string;
	min: string;
	max: string;
}

export interface ListState {
	name: string;
	facets: Record<string, string[]>;
	stats: StatFilter[];
	/** Whether a row must pass every stat filter or one of them. */
	match: 'all' | 'any';
	/** Shows items without a known way to get them while browsing. */
	reveal: boolean;
	sort: { column: string; direction: 'asc' | 'desc' };
}

/** Availability values that browsing leaves out until a reader asks for them. Name search finds them anyway. */
export const hiddenAvailability = ['noKnownSource', 'notInNormalPlay'];

export const defaultSort = { column: 'name', direction: 'asc' } as const;

const bound = (value: string): number | undefined =>
	value.trim() === '' || Number.isNaN(Number(value)) ? undefined : Number(value);

export function readState(
	params: Pick<URLSearchParams, 'get' | 'getAll'>,
	index: ListShape
): ListState {
	const facets: Record<string, string[]> = {};
	for (const facet of index.facets) {
		const values = params.getAll(facet.id).filter((value) => facet.values.includes(value));
		if (values.length) facets[facet.id] = values;
	}
	const numeric = new Set(
		index.columns.filter((column) => column.numeric).map((column) => column.id)
	);
	const stats = params
		.getAll('stat')
		.map((value) => {
			const [column = '', min = '', max = ''] = value.split(':');
			return { column, min, max };
		})
		.filter((filter) => numeric.has(filter.column));
	const column = params.get('sort') ?? defaultSort.column;
	const known =
		column === 'name' ||
		index.fixed.some((entry) => entry.id === column) ||
		index.columns.some((entry) => entry.id === column);
	const direction = params.get('dir');
	return {
		name: params.get('name') ?? '',
		facets,
		stats,
		match: params.get('match') === 'any' ? 'any' : 'all',
		reveal: params.get('unknown') === '1',
		sort: known
			? {
					column,
					direction:
						direction === 'desc'
							? 'desc'
							: direction === 'asc'
								? 'asc'
								: naturalDirection(column, index)
				}
			: defaultSort
	};
}

export function writeState(state: ListState, index: ListShape): URLSearchParams {
	const params = new URLSearchParams();
	if (state.name) params.set('name', state.name);
	for (const facet of index.facets)
		for (const value of state.facets[facet.id] ?? []) params.append(facet.id, value);
	for (const filter of state.stats)
		params.append('stat', `${filter.column}:${filter.min}:${filter.max}`);
	if (state.stats.length > 1 && state.match === 'any') params.set('match', 'any');
	if (state.reveal) params.set('unknown', '1');
	if (state.sort.column !== defaultSort.column) params.set('sort', state.sort.column);
	if (state.sort.direction !== naturalDirection(state.sort.column, index))
		params.set('dir', state.sort.direction);
	return params;
}

/** A column's first-click order: A to Z for text, high to low for numbers. */
export function naturalDirection(column: string, index: ListShape): 'asc' | 'desc' {
	return index.fixed.find((entry) => entry.id === column)?.numeric ||
		index.columns.find((entry) => entry.id === column)?.numeric
		? 'desc'
		: 'asc';
}

function statMatches(item: ListRow, filter: StatFilter): boolean {
	const value = item.cells[filter.column]?.number;
	if (value == null) return false;
	const minimum = bound(filter.min);
	const maximum = bound(filter.max);
	return (minimum === undefined || value >= minimum) && (maximum === undefined || value <= maximum);
}

/**
 * Whether a name holds the search text, or each searched word starts a word of the name or is one letter edit
 * away from one, because the game misspells some names, such as Steel Rapier Gaurd.
 */
export function nameMatches(name: string, needle: string): boolean {
	const text = name.toLocaleLowerCase('en');
	if (text.includes(needle)) return true;
	const words = text.split(/[^\p{L}\p{N}']+/u).filter(Boolean);
	return needle
		.split(/\s+/)
		.every(
			(part) =>
				words.some((word) => word.startsWith(part)) ||
				(part.length >= 4 && words.some((word) => oneEditApart(word, part)))
		);
}

/** Whether two words differ by one inserted, removed or changed letter, or two swapped neighbours. */
function oneEditApart(a: string, b: string): boolean {
	if (Math.abs(a.length - b.length) > 1 || a === b) return a === b;
	let i = 0;
	while (i < a.length && i < b.length && a[i] === b[i]) i++;
	const restA = a.slice(i);
	const restB = b.slice(i);
	if (a.length === b.length)
		return (
			restA.slice(1) === restB.slice(1) ||
			(restA[0] === restB[1] && restA[1] === restB[0] && restA.slice(2) === restB.slice(2))
		);
	return a.length > b.length ? restA.slice(1) === restB : restA === restB.slice(1);
}

/**
 * A row's values for a facet under the selection of the facets it depends on: with ways to get it selected, only the
 * places where it is got those ways, and with places selected, only the ways to get it there.
 */
export function facetValues(item: ListRow, facet: string, state: ListState): string[] {
	const values = item.facets[facet] ?? [];
	const places = (item.facets.place ?? []).map(
		(place, index) => [place, item.placeSources[index] ?? []] as const
	);
	if (facet === 'place' && state.facets.source?.length) {
		const sources = state.facets.source;
		return places
			.filter(([, kinds]) => kinds.some((kind) => sources.includes(kind)))
			.map(([place]) => place);
	}
	if (facet === 'source' && state.facets.place?.length) {
		const selected = state.facets.place;
		return [
			...new Set(places.filter(([place]) => selected.includes(place)).flatMap(([, kinds]) => kinds))
		];
	}
	return values;
}

/**
 * Whether a row passes the list's state. `skip` leaves one facet out, so that facet's counts show what each of its
 * values would add.
 */
export function matches<R extends ListRow>(
	item: R,
	state: ListState,
	index: ListModel<R>,
	skip?: string
): boolean {
	const needle = state.name.trim().toLocaleLowerCase('en');
	if (needle && !nameMatches(`${item.name} ${item.versionLabel ?? ''}`, needle)) return false;
	// A skipped facet's selection neither filters nor scopes the facets that depend on it.
	const scoping = skip ? { ...state, facets: { ...state.facets, [skip]: [] } } : state;
	for (const facet of index.facets) {
		if (facet.id === skip) continue;
		const selected = state.facets[facet.id] ?? [];
		if (!selected.length) continue;
		const values = facetValues(item, facet.id, scoping);
		if (!selected.some((value) => values.includes(value))) return false;
	}
	// Browsing shows items with a known way to get them first, and a name search finds every item.
	if (
		!state.reveal &&
		!needle &&
		skip !== 'availability' &&
		!state.facets.availability?.length &&
		hiddenAvailability.includes(item.status)
	)
		return false;
	if (state.stats.length) {
		const passed = state.stats.map((filter) => statMatches(item, filter));
		if (state.match === 'all' ? passed.includes(false) : !passed.includes(true)) return false;
	}
	return true;
}

/** A column's sort value: the name, a fixed column's own field, or the row's cell. */
function sortValue(item: ListRow, column: string): number | string | null {
	if (column === 'name') return item.name;
	const cell = item.cells[column];
	if (cell) return cell.number ?? cell.text;
	const own = (item as unknown as Record<string, unknown>)[column];
	return typeof own === 'number' || typeof own === 'string' ? own : null;
}

/** Sorts rows by a column, with rows that lack a value last in both directions. */
export function sortItems<R extends ListRow>(items: R[], sort: ListState['sort']): R[] {
	const sign = sort.direction === 'asc' ? 1 : -1;
	return [...items].sort((a, b) => {
		const left = sortValue(a, sort.column);
		const right = sortValue(b, sort.column);
		if (left === null || right === null) {
			if (left !== right) return left === null ? 1 : -1;
		} else {
			const difference =
				typeof left === 'number' && typeof right === 'number'
					? left - right
					: String(left).localeCompare(String(right), 'en');
			if (difference) return sign * difference;
		}
		// Words that rank alike, such as Average and Standard speed, stay grouped.
		const words = (a.cells[sort.column]?.text ?? '').localeCompare(
			b.cells[sort.column]?.text ?? '',
			'en'
		);
		return words || a.name.localeCompare(b.name, 'en');
	});
}

export interface FacetGroup {
	facet: ListFacet;
	options: { value: string; label: string; count: number }[];
}

/**
 * The filters to show: every general filter, and a filter for some categories once one of them is selected, the list
 * holds only their items, or the filter has a selection. Values without a matching item stay out unless selected.
 */
export function facetGroups<R extends ListRow>(
	items: R[],
	shown: R[],
	state: ListState,
	index: ListModel<R>,
	label: (facet: ListFacet, value: string) => string
): FacetGroup[] {
	const selectedCategories = state.facets.category ?? [];
	return index.facets
		.filter((facet) => {
			if (!facet.categories.length || state.facets[facet.id]?.length) return true;
			if (selectedCategories.some((category) => facet.categories.includes(category))) return true;
			return shown.length > 0 && shown.every((item) => facet.categories.includes(item.category));
		})
		.map((facet) => {
			const counts: Record<string, number> = {};
			const scoping = { ...state, facets: { ...state.facets, [facet.id]: [] } };
			for (const item of items) {
				if (!matches(item, state, index, facet.id)) continue;
				for (const value of facetValues(item, facet.id, scoping))
					counts[value] = (counts[value] ?? 0) + 1;
			}
			const selected = state.facets[facet.id] ?? [];
			return {
				facet,
				options: facet.values
					.filter((value) => counts[value] || selected.includes(value))
					.map((value) => ({ value, label: label(facet, value), count: counts[value] ?? 0 }))
			};
		})
		.filter((group) => group.options.length > 0);
}

/**
 * The columns after the name and the fixed columns: those for the selected categories or for a list of one
 * category's rows, narrowed by the kind's own rule, and the column of every stat filter.
 */
export function shownColumns<R extends ListRow>(
	shown: R[],
	state: ListState,
	index: ListModel<R>
): ListColumn[] {
	const selected = state.facets.category ?? [];
	const filtered = new Set(state.stats.map((filter) => filter.column));
	const candidates = index.columns.filter(
		(column) =>
			column.categories.length > 0 &&
			(selected.length
				? selected.every((category) => column.categories.includes(category))
				: shown.length > 0 && shown.every((item) => column.categories.includes(item.category)))
	);
	const kept = new Set(index.prune ? index.prune(shown, candidates) : candidates);
	return index.columns.filter((column) => filtered.has(column.id) || kept.has(column));
}
