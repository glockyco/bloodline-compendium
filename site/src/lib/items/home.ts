import type { CategorySummary, ItemIndex, ItemSummary, SourceKind } from '../site-data.js';

/** Item categories in the order the home page leads with, before the rest by size. */
const leadOrder = ['weapon', 'armor', 'weapon-part', 'material', 'food', 'quest-item'];

/**
 * The item whose icon stands for each category, chosen because it reads well at tile size. A missing item falls back
 * to the category's most valuable item with an icon, so an update that renames one only changes which icon shows.
 */
const featuredIcons: Record<string, string> = {
	weapon: 'bloodforged-blade',
	armor: 'celestial-knight-helm',
	'weapon-part': 'astral-greatblade',
	material: 'ruby-gem',
	food: 'apple-pie',
	'quest-item': 'artificers-windmill-key'
};

/** An item category on the home page: its list's filter, its plural name, its listed count and its icon. */
export interface HomeCategory {
	slug: string;
	name: string;
	count: number;
	icon: string | null;
}

export interface HomeData {
	count: number;
	known: number;
	example: string;
	categories: HomeCategory[];
}

function iconOf(items: ItemSummary[], featured: string | undefined): string | null {
	const chosen = items.find((item) => item.slug === featured && item.icon);
	if (chosen) return chosen.icon;
	return (
		[...items]
			.filter((item) => item.icon)
			.sort((a, b) => (b.value ?? -1) - (a.value ?? -1) || a.name.localeCompare(b.name, 'en'))[0]
			?.icon ?? null
	);
}

/** The ways to get an item, as its How to Get filter values. */
const sourcesOf = (item: ItemSummary) => (item.facets.source ?? []) as SourceKind[];

/** The home page's item categories, derived from the published item list. */
export function homeData(index: ItemIndex): HomeData {
	const items = index.items;
	// The item list hides items without a known source until asked, so counts that link to it count the same.
	const listed = (category: string) =>
		items.filter((item) => item.category === category && sourcesOf(item).length > 0);
	const ordered: CategorySummary[] = [
		...leadOrder
			.map((slug) => index.categories.find((category) => category.slug === slug))
			.filter((category): category is CategorySummary => Boolean(category)),
		...[...index.categories]
			.filter((category) => !leadOrder.includes(category.slug))
			.sort((a, b) => b.count - a.count || a.name.localeCompare(b.name, 'en'))
	];
	return {
		count: items.length,
		known: items.filter((item) => sourcesOf(item).length > 0).length,
		example: (items.find((item) => item.slug === 'yew-shortbow') ?? items[0])?.name ?? '',
		categories: ordered
			.map((category) => {
				const members = listed(category.slug);
				return {
					slug: category.slug,
					name: category.plural,
					count: members.length,
					icon: iconOf(members, featuredIcons[category.slug])
				};
			})
			.filter((category) => category.count > 0)
	};
}
