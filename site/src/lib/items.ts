import type { ItemSummary } from './site-data.js';

export interface ItemFilters {
	name: string;
	categories: string[];
	types: string[];
	sources: string[];
	showUnknown: boolean;
	sort: string;
}
export const itemSorts = [
	'name-asc',
	'name-desc',
	'type-asc',
	'type-desc',
	'value-asc',
	'value-desc'
];

export function readFilters(
	params: Pick<URLSearchParams, 'get' | 'getAll'> = new URLSearchParams()
): ItemFilters {
	return {
		name: params.get('name') ?? '',
		categories: params.getAll('category'),
		types: params.getAll('type'),
		sources: params.getAll('source'),
		showUnknown: params.get('unknown') === '1',
		sort: itemSorts.includes(params.get('sort') ?? '') ? params.get('sort')! : 'name-asc'
	};
}

export function matchesItem(item: ItemSummary, filters: ItemFilters): boolean {
	return (
		(filters.showUnknown || item.sourceKinds.length > 0) &&
		(!filters.name ||
			`${item.name} ${item.qualifier ?? ''}`
				.toLocaleLowerCase('en')
				.includes(filters.name.trim().toLocaleLowerCase('en'))) &&
		(!filters.categories.length || filters.categories.includes(item.category)) &&
		(!filters.types.length || filters.types.includes(item.type)) &&
		(!filters.sources.length || item.sourceKinds.some((kind) => filters.sources.includes(kind)))
	);
}

export function filterItems(items: ItemSummary[], filters: ItemFilters): ItemSummary[] {
	return items
		.filter((item) => matchesItem(item, filters))
		.sort((a, b) => {
			let difference: number;
			if (filters.sort.startsWith('value-')) {
				if (a.value === null) return b.value === null ? a.name.localeCompare(b.name, 'en') : 1;
				if (b.value === null) return -1;
				difference = a.value - b.value;
			} else if (filters.sort.startsWith('type-')) {
				difference = a.type.localeCompare(b.type, 'en');
			} else {
				difference = a.name.localeCompare(b.name, 'en');
			}
			return (
				(filters.sort.endsWith('-desc') ? -difference : difference) ||
				a.name.localeCompare(b.name, 'en') ||
				(a.qualifier ?? '').localeCompare(b.qualifier ?? '', 'en') ||
				a.slug.localeCompare(b.slug, 'en')
			);
		});
}
