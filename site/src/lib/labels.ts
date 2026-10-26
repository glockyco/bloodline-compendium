import type { SourceKind, GapKind } from './site-data.js';
export function titleCase(value: string): string {
	return value
		.replace(/([a-z])([A-Z])/g, '$1 $2')
		.replace(/[_-]+/g, ' ')
		.toLowerCase()
		.replace(/\b[a-z]/g, (letter) => letter.toUpperCase());
}

export const sourceHeadings: Record<SourceKind, string> = {
	shop: 'Sold by',
	currencyShop: 'Sold for special currency',
	smithing: 'Made by smithing',
	crafting: 'Crafted',
	cooking: 'Cooked',
	containerLoot: 'Found in containers',
	randomLoot: 'Found as random loot',
	enemyDrop: 'Dropped by',
	pickup: 'Picked up',
	dialogue: 'Given in dialogue',
	fishing: 'Fished',
	gathering: 'Gathered',
	farming: 'Grown',
	bountyNote: 'Obtained from bounty notes',
	settlementDelivery: 'Delivered by settlements'
};
/** Short names for each way of getting an item, for filters and overviews. */
export const sourceNames: Record<SourceKind, string> = {
	shop: 'Merchants',
	currencyShop: 'Currency traders',
	smithing: 'Smithing',
	crafting: 'Crafting',
	cooking: 'Cooking',
	containerLoot: 'Containers',
	randomLoot: 'Random loot',
	enemyDrop: 'Creatures',
	pickup: 'World pickups',
	dialogue: 'Conversations',
	fishing: 'Fishing',
	gathering: 'Gathering',
	farming: 'Farming',
	bountyNote: 'Bounty notes',
	settlementDelivery: 'Settlements'
};
export const gaps: Record<GapKind, { title: string; explanation: string }> = {
	unknownCondition: {
		title: 'Unknown relation conditions',
		explanation:
			'These sources or uses depend on conditions the readable data does not determine. The conditions remain unknown rather than being treated as unrestricted.'
	},
	unresolvedReference: {
		title: 'Unresolved entity references',
		explanation:
			'These references have a readable label but no known game identifier. They remain unlinked until their identity can be resolved.'
	},
	itemWithoutSource: {
		title: 'Items without a known source',
		explanation:
			'We do not know how to obtain these items. They stay in the reference, but their pages cannot yet point to a shop, recipe, drop or other source.'
	},
	sourceWithoutPlace: {
		title: 'Sources without a known place',
		explanation:
			'We know what gives these items, but not where to find it. A source is not proof that its location is reachable.'
	},
	itemWithoutIcon: {
		title: 'Items without an icon',
		explanation: 'No readable icon was found for these items. No substitute image is used.'
	},
	unknownFact: {
		title: 'Unknown item facts',
		explanation: 'The readable data does not determine these values. Unknown does not mean zero.'
	},
	placeWithoutName: {
		title: 'Places without a known name',
		explanation:
			'These places have no known in-game name. Their locations remain unknown in this reference.'
	},
	unreadObject: {
		title: 'Unread objects and packages',
		explanation:
			'These parts of the game data could not be read completely. Their contents are absent, so items, sources or places may be missing from this reference.'
	},
	tooltipDifference: {
		title: 'Differences from in-game tooltips',
		explanation:
			"The game's own tooltip shows another value for these items than this reference, and we have not confirmed which one the game applies. Where the game's code shows the applied value, this reference shows that one."
	}
};
