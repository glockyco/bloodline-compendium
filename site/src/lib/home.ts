import type { CategorySummary, ItemIndex, ItemPage, ItemSummary, SourceKind } from './site-data.js';
import { sourceNames } from './labels.js';

/** Browse categories shown large on the home page, in this order when they exist. */
const majorOrder = ['weapon', 'armor', 'weapon-part', 'material', 'food', 'quest-item'];
const majorCount = 6;
const iconCount = 5;
const wayIconCount = 3;

/** What each large category holds, listed by its most common types. Categories without one list their types. */
const descriptions: Record<string, string> = {
	weapon: 'Swords, axes, daggers, spears, shields, bows and firearms',
	armor: 'Helmets, chest armor, cloaks, trousers, boots and amulets',
	'weapon-part': 'Blades, guards, pommels and grips for smithing weapons',
	material: 'Ores, ingots, gems, herbs, hides and coins',
	food: 'Meals, drinks, fish and cooking ingredients',
	'quest-item': 'Keys, letters, emblems and quest deliveries'
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
	'quest-item': ['artificers-windmill-key', 'lions-mane-emblem', 'windstone', 'hunters-pack'],
	shop: ['golden-crown', 'ash-lute', 'heavy-iron-plate-cuirass'],
	smithing: ['astral-greatblade', 'heavy-moonstone-axe-head', 'astral-guard'],
	containerLoot: ['godsgift-rune', 'troedian-ore', 'portal-coin'],
	pickup: ['boltbringer', 'sorrowsong', 'thurstons-grimoire'],
	randomLoot: ['gold-shilling', 'portal-coin', 'gold-ingot'],
	enemyDrop: ['the-copper-crown', 'gold-shilling', 'ethereal-archstone'],
	cooking: ['fondue', 'druid-s-stew', 'carrot-cake'],
	dialogue: ['godsgift-rune', 'exquisite-spice', 'troedian-metal'],
	settlementDelivery: ['gold-ingot', 'ruby-gem', 'amethyst-gem'],
	gathering: ['glowing-hibiscus', 'candyberry', 'growgloom'],
	fishing: ['queenfish'],
	farming: ['carrot', 'potato', 'apple'],
	currencyShop: ['winter-warden-cuirass', 'froststrider-hood', 'winter-warden-helm'],
	bountyNote: ['a-walk-in-the-woods-1', 'the-golden-goblet', 'the-jeweled-dagger']
};

export interface HomeGroup {
	slug: string;
	name: string;
	count: number;
	description: string;
	icons: string[];
}

export interface HomeWay {
	key: string;
	label: string;
	text: string;
	kinds: SourceKind[];
	count: number;
	icons: string[];
}

export interface HomeData {
	count: number;
	known: number;
	example: string;
	groups: HomeGroup[];
	minor: CategorySummary[];
	ways: HomeWay[];
}

function plural(count: number, one: string, many: string): string {
	return `${count.toLocaleString('en')} ${count === 1 ? one : many}`;
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

/** The home page's browse entries and source overview, derived from the published site data. */
export function homeData(index: ItemIndex, pages: ItemPage[]): HomeData {
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

	const givers = new Map<SourceKind, Set<string>>();
	const currencies = new Map<string, number>();
	for (const page of pages)
		for (const source of page.sources) {
			if (source.unconfirmed) continue;
			if (!givers.has(source.kind)) givers.set(source.kind, new Set());
			givers.get(source.kind)!.add(source.name);
			if (source.kind === 'currencyShop' && source.price?.currency)
				currencies.set(
					source.price.currency.name,
					(currencies.get(source.price.currency.name) ?? 0) + 1
				);
		}
	const giverCount = (kind: SourceKind) => givers.get(kind)?.size ?? 0;
	// The most used currency names the way, so the line stays short however many currencies exist.
	const currencyList = [...currencies]
		.sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0], 'en'))
		.map(([name]) => name);
	const texts: Partial<Record<SourceKind, string>> = {
		shop: `Sold by ${plural(giverCount('shop'), 'merchant', 'merchants')}`,
		currencyShop:
			currencyList.length > 2
				? `Bought with ${currencyList[0]} and ${plural(currencyList.length - 1, 'other currency', 'other currencies')}`
				: `Bought with ${wordList.format(currencyList)}`,
		smithing: `Made from ${plural(giverCount('smithing'), 'smithing recipe', 'smithing recipes')}`,
		crafting: `Made from ${plural(giverCount('crafting'), 'crafting recipe', 'crafting recipes')}`,
		cooking: `Cooked from ${plural(giverCount('cooking'), 'recipe', 'recipes')}`,
		containerLoot: `Found in ${plural(giverCount('containerLoot'), 'kind of container', 'kinds of container')}`,
		randomLoot: 'Rolled from general container loot',
		enemyDrop: `Dropped by ${plural(giverCount('enemyDrop'), 'kind of creature', 'kinds of creature')}`,
		pickup: 'Placed in the world',
		dialogue: `Given by ${plural(giverCount('dialogue'), 'person', 'people')} in conversation`,
		settlementDelivery: `Delivered by ${plural(giverCount('settlementDelivery'), 'settlement building', 'settlement buildings')}`,
		bountyNote: `Given with ${plural(giverCount('bountyNote'), 'bounty note', 'bounty notes')}`,
		gathering: 'Gathered from plants, ore and other nodes',
		fishing: 'Caught by fishing',
		farming: 'Harvested from planted seeds'
	};
	const candidates: { key: string; label: string; text: string; kinds: SourceKind[] }[] = (
		Object.keys(texts) as SourceKind[]
	).map((kind) => ({ key: kind, label: sourceNames[kind], text: texts[kind]!, kinds: [kind] }));
	const ways = candidates
		.map((way) => {
			const members = items.filter((item) =>
				sourcesOf(item).some((kind) => way.kinds.includes(kind))
			);
			return {
				...way,
				count: members.length,
				icons: pickIcons(members, featuredIcons[way.key], wayIconCount)
			};
		})
		.filter((way) => way.count > 0)
		.sort((a, b) => b.count - a.count || a.label.localeCompare(b.label, 'en'));

	return {
		count: items.length,
		known: items.filter((item) => sourcesOf(item).length > 0).length,
		example: (items.find((item) => item.slug === 'yew-shortbow') ?? items[0])?.name ?? '',
		groups,
		minor: ordered
			.slice(majorCount)
			.map((category) => ({ ...category, count: listed(category.slug) }))
			.filter((category) => category.count > 0),
		ways
	};
}
