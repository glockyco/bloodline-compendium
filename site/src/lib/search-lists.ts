import { hiddenAvailability, writeState, type ListState } from './list.js';
import type { ItemIndex, ItemSummary } from './site-data.js';

/**
 * A search page that leads a category or type word to the filtered item list. Its words carry the highest weight,
 * so that the list ranks above the single items whose names share them.
 */
export interface ListRecord {
	url: string;
	title: string;
	/** The nouns its item names end with, in the plural, as players search for them. */
	words: string[];
	summary: string;
}

/** Filters whose values name a kind of item, such as Dagger or Head, as opposed to effects, sources or places. */
const typeFacets = ['weaponType', 'armorSlot', 'armorClass', 'weaponPart'];

/** Type filters whose items share name nouns. Armor classes span every slot, so boots would find all three. */
const nounFacets = ['weaponType', 'armorSlot', 'weaponPart'];

/** A noun names a type's list when at least this many of its items end their name with it. */
const nounMinimum = 3;

/**
 * Search records for every category and for every type within one, linking to the item list with that filter, so
 * that a search for swords or boots finds a list next to single items. A type's record also carries the nouns its
 * item names end with, split from compounds such as Ultrasword, so that the words players use find it.
 */
export function listRecords(index: ItemIndex, listPath: string): ListRecord[] {
	const browsed = index.items.filter((item) => !hiddenAvailability.includes(item.status));
	const nouns = new Set(browsed.map(lastWord));
	const category = index.facets.find((facet) => facet.id === 'category');
	const records: ListRecord[] = [];
	for (const slug of category?.values ?? []) {
		const plural = category?.labels?.[slug] ?? slug;
		const items = browsed.filter((item) => item.category === slug);
		if (!items.length) continue;
		records.push({
			url: listUrl(index, listPath, { category: [slug] }),
			title: plural,
			words: [],
			summary: `All ${items.length} ${plural.toLocaleLowerCase('en')} in the item list.`
		});
	}
	for (const facet of index.facets.filter((entry) => typeFacets.includes(entry.id))) {
		for (const value of facet.values) {
			const items = browsed.filter((item) => item.facets[facet.id]?.includes(value));
			if (!items.length) continue;
			const categories = [...new Set(items.map((item) => item.category))];
			const plural = categories.map((slug) => category?.labels?.[slug] ?? slug).join(' and ');
			const label = facet.labels?.[value] ?? value;
			records.push({
				url: listUrl(index, listPath, { category: categories, [facet.id]: [value] }),
				title: `${plural}: ${label}`,
				words: nounFacets.includes(facet.id) ? nounsOf(items, nouns) : [],
				summary: `All ${items.length} ${plural.toLocaleLowerCase('en')} of the ${facet.label.toLocaleLowerCase('en')} ${label} in the item list.`
			});
		}
	}
	return records;
}

function lastWord(item: ItemSummary): string {
	return (item.name.toLocaleLowerCase('en').match(/[a-z]+/g) ?? []).at(-1) ?? '';
}

/**
 * The nouns at least `nounMinimum` items end their name with, in the plural, a compound counting for the noun it ends
 * with and Dagger and Daggers counting as one.
 */
function nounsOf(items: ItemSummary[], nouns: Set<string>): string[] {
	const counts = new Map<string, number>();
	for (const item of items) {
		const word = lastWord(item);
		if (!word) continue;
		const found = new Set([word]);
		for (const noun of nouns)
			if (noun.length >= 3 && noun !== word && word.endsWith(noun)) found.add(noun);
		for (const noun of new Set([...found].map(plural)))
			counts.set(noun, (counts.get(noun) ?? 0) + 1);
	}
	return [...counts]
		.filter(([, count]) => count >= nounMinimum)
		.sort(([a, left], [b, right]) => right - left || a.localeCompare(b, 'en'))
		.map(([noun]) => noun);
}

function listUrl(index: ItemIndex, listPath: string, facets: Record<string, string[]>): string {
	const state: ListState = {
		name: '',
		facets,
		stats: [],
		match: 'all',
		reveal: false,
		sort: { column: 'name', direction: 'asc' }
	};
	return `${listPath}?${writeState(state, index)}`;
}

const escape = (text: string) =>
	text.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');

/** The record as a page for Pagefind, which reads the weights and the title from its markup. */
export function listPage(record: ListRecord): string {
	const words = record.words.length
		? `<p data-pagefind-weight="10">Names include ${escape(record.words.join(', '))}.</p>`
		: '';
	// Pagefind ranks title words highest, so the title names the most common nouns, as players search for them.
	const nouns = record.words.slice(0, 3);
	const title = nouns.length
		? `${record.title}, such as ${nouns.length > 1 ? nouns.slice(0, -1).join(', ') + ' and ' + nouns.at(-1) : nouns[0]}`
		: record.title;
	return (
		`<html lang="en"><body><main data-pagefind-body>` +
		`<h1 data-pagefind-meta="title" data-pagefind-weight="10">${escape(title)}</h1>` +
		`${words}<p>${escape(record.summary)}</p></main></body></html>`
	);
}

/** A name noun in the plural. Nouns such as boots and daggers already are. */
function plural(word: string): string {
	if (/[^s]s$/.test(word)) return word;
	if (word.endsWith('fe')) return word.slice(0, -2) + 'ves';
	return /(ss|x|z|ch|sh)$/.test(word) ? word + 'es' : word + 's';
}
