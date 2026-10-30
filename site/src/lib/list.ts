import type { ItemIndex, ItemSummary, ListColumn, ListFacet } from './site-data.js';

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
	index: ItemIndex
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
		['name', 'type', 'value'].includes(column) ||
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

export function writeState(state: ListState, index: ItemIndex): URLSearchParams {
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
export function naturalDirection(column: string, index: ItemIndex): 'asc' | 'desc' {
	return column === 'value' || index.columns.find((entry) => entry.id === column)?.numeric
		? 'desc'
		: 'asc';
}

function statMatches(item: ItemSummary, filter: StatFilter): boolean {
	const value = item.cells[filter.column]?.number;
	if (value == null) return false;
	const minimum = bound(filter.min);
	const maximum = bound(filter.max);
	return (minimum === undefined || value >= minimum) && (maximum === undefined || value <= maximum);
}

/**
 * An item's values for a facet under the selection of the facets it depends on: with ways to get it selected, only the
 * places where it is got those ways, and with places selected, only the ways to get it there.
 */
export function facetValues(item: ItemSummary, facet: string, state: ListState): string[] {
	const values = item.facets[facet] ?? [];
	const places = Object.entries(item.placeSources);
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
 * Whether an item passes the list's state. `skip` leaves one facet out, so that facet's counts show what each of its
 * values would add.
 */
export function matches(
	item: ItemSummary,
	state: ListState,
	index: ItemIndex,
	skip?: string
): boolean {
	const needle = state.name.trim().toLocaleLowerCase('en');
	if (needle && !`${item.name} ${item.qualifier ?? ''}`.toLocaleLowerCase('en').includes(needle))
		return false;
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

function sortValue(item: ItemSummary, column: string): number | string | null {
	if (column === 'name') return item.name;
	if (column === 'type') return item.type;
	if (column === 'value') return item.value;
	const cell = item.cells[column];
	return cell ? (cell.number ?? cell.text) : null;
}

/** Sorts rows by a column, with rows that lack a value last in both directions. */
export function sortItems(items: ItemSummary[], sort: ListState['sort']): ItemSummary[] {
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
export function facetGroups(
	items: ItemSummary[],
	shown: ItemSummary[],
	state: ListState,
	index: ItemIndex,
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
 * The columns after name, type and value: those for the selected categories or for a list of one category's items,
 * and the column of every stat filter.
 */
export function shownColumns(
	shown: ItemSummary[],
	state: ListState,
	index: ItemIndex
): ListColumn[] {
	const selected = state.facets.category ?? [];
	const filtered = new Set(state.stats.map((filter) => filter.column));
	return index.columns.filter(
		(column) =>
			filtered.has(column.id) ||
			(column.categories.length > 0 &&
				(selected.length
					? selected.every((category) => column.categories.includes(category))
					: shown.length > 0 && shown.every((item) => column.categories.includes(item.category))))
	);
}
