import type { SourceKind, GapKind } from './site-data.js';
export const sourceLabels: Record<SourceKind, string> = {
	shop: 'Shop',
	currencyShop: 'Currency shop',
	smithing: 'Smithing',
	crafting: 'Crafting',
	cooking: 'Cooking',
	containerLoot: 'Container loot',
	randomLoot: 'Random loot',
	enemyDrop: 'Enemy drop',
	pickup: 'Pickup',
	dialogue: 'Dialogue grant',
	fishing: 'Fishing',
	gathering: 'Gathering',
	farming: 'Farming',
	bountyNote: 'Bounty note',
	settlementDelivery: 'Settlement delivery'
};
export const gaps: Record<GapKind, { title: string; explanation: string }> = {
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
			'These places have an internal identifier but no readable in-game name. Sources may show that identifier instead.'
	},
	unreadObject: {
		title: 'Unread objects and packages',
		explanation:
			'These parts of the game data could not be read completely. Their contents are absent, so items, sources or places may be missing from this reference.'
	}
};
