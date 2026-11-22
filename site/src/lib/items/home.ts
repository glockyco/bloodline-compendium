import type { CategorySummary, ItemIndex, ItemSummary, SourceKind } from '../site-data.js';

/** Browse categories shown large on the home page, in this order when they exist. */
const majorOrder = ['weapon', 'armor', 'weapon-part', 'material', 'food', 'quest-item'];
const majorCount = 6;
const iconCount = 5;

/** What each large category holds, each word matching several of its items. Categories without one list their types. */
const descriptions: Record<string, string> = {
	weapon: 'Swords, axes, maces, daggers, spears, staves, shields, bows, crossbows and guns',
	armor: 'Helmets, chest armor, sleeves, leg armor, boots, pauldrons, belts, amulets and rings',
	'weapon-part':
		'Blades, heads, guards, grips, pommels, shafts and wand parts for smithing weapons',
	material: 'Ores, ingots, logs, hides, gems, herbs, mushrooms, coins and engineering parts',
	food: 'Meals, drinks, fish and cooking ingredients',
	'quest-item': 'Keys and the goods quests ask for'
};

/**
 * Items whose icons read well at small sizes. A missing item falls back to the most valuable items with
 * distinct icons, so an update that renames one only changes which icon shows.
 */
const featuredIcons: Record<string, string[]> = {
	weapon: [
		'chapter-shield',
		'bloodforged-blade',
		'lonland-pistol',
		'frostvein-ultrasword',
		'boltbringer'
	],
	armor: [
		'celestial-knight-helm',
		'winter-warden-cuirass',
		'fairy-wings',
		'necklace-of-ascendant-grace',
		'golden-crown'
	],
	'weapon-part': [
		'astral-greatblade',
		'heavy-moonstone-axe-head',
		'gilded-greatmace-head',
		'moonstone-spear-head',
		'iron-warhammer-head'
	],
	material: ['ruby-gem', 'gold-ingot', 'frostvein-chunk', 'growgloom', 'deer-pelt'],
	food: ['fletcher-s-brew', 'apple-pie', 'fondue', 'queenfish', 'coastal-hotpot'],
	'quest-item': ['artificers-windmill-key', 'lions-mane-emblem', 'windstone', 'hunters-pack']
};

export interface HomeGroup {
	slug: string;
	name: string;
	count: number;
	description: string;
	icons: string[];
}

export interface HomeData {
	count: number;
	known: number;
	example: string;
	groups: HomeGroup[];
	minor: CategorySummary[];
}

const wordList = new Intl.ListFormat('en', { type: 'conjunction' });

function pickIcons(items: ItemSummary[], featured: string[] | undefined, count: number): string[] {
	const bySlug = new Map(items.map((item) => [item.slug, item]));
	const valuable = [...items].sort(
		(a, b) => (b.value ?? -1) - (a.value ?? -1) || a.name.localeCompare(b.name, 'en')
	);
	const icons: string[] = [];
	for (const item of [...(featured ?? []).map((slug) => bySlug.get(slug)), ...valuable]) {
		if (icons.length === count) break;
		if (item?.icon && !icons.includes(item.icon)) icons.push(item.icon);
	}
	return icons;
}

function describe(category: CategorySummary, items: ItemSummary[]): string {
	if (descriptions[category.slug]) return descriptions[category.slug];
	const types = [...new Set(items.map((item) => item.type))].slice(0, 6);
	return wordList.format(types);
}

/** The ways to get an item, as its How to Get filter values. */
const sourcesOf = (item: ItemSummary) => (item.facets.source ?? []) as SourceKind[];

/** The home page's browse entries, derived from the published item list. */
export function homeData(index: ItemIndex): HomeData {
	const items = index.items;
	// The item list hides items without a known source until asked, so counts that link to it count the same.
	const listed = (category: string) =>
		items.filter((item) => item.category === category && sourcesOf(item).length > 0).length;
	const ordered = [
		...majorOrder
			.map((slug) => index.categories.find((category) => category.slug === slug))
			.filter((category): category is CategorySummary => Boolean(category)),
		...[...index.categories]
			.filter((category) => !majorOrder.includes(category.slug))
			.sort((a, b) => b.count - a.count || a.name.localeCompare(b.name, 'en'))
	];
	const groups = ordered.slice(0, majorCount).map((category) => {
		const members = items.filter((item) => item.category === category.slug);
		return {
			slug: category.slug,
			name: category.plural,
			count: listed(category.slug),
			description: describe(category, members),
			icons: pickIcons(members, featuredIcons[category.slug], iconCount)
		};
	});
	return {
		count: items.length,
		known: items.filter((item) => sourcesOf(item).length > 0).length,
		example: (items.find((item) => item.slug === 'yew-shortbow') ?? items[0])?.name ?? '',
		groups,
		minor: ordered
			.slice(majorCount)
			.map((category) => ({ ...category, count: listed(category.slug) }))
			.filter((category) => category.count > 0)
	};
}
