import type { ItemSummary } from './site-data.js';
export function selectItems(items: ItemSummary[], category: string, sort: string) {
	return items
		.filter((item) => !category || item.category === category)
		.sort((a, b) => {
			if (sort === 'value-asc' || sort === 'value-desc') {
				if (a.value === null) return b.value === null ? a.name.localeCompare(b.name, 'en') : 1;
				if (b.value === null) return -1;
				const difference = sort === 'value-asc' ? a.value - b.value : b.value - a.value;
				if (difference) return difference;
			}
			return sort === 'name-desc'
				? b.name.localeCompare(a.name, 'en')
				: a.name.localeCompare(b.name, 'en');
		});
}
