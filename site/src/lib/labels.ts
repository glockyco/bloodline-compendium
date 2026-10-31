import type { SourceKind } from './site-data.js';
export function titleCase(value: string): string {
	return value
		.replace(/([a-z])([A-Z])/g, '$1 $2')
		.replace(/[_-]+/g, ' ')
		.toLowerCase()
		.replace(/\b[a-z]/g, (letter) => letter.toUpperCase());
}

export const sourceHeadings: Record<SourceKind, string> = {
	shop: 'Sold by',
	currencyShop: 'Sold for Special Currency',
	smithing: 'Made by Smithing',
	crafting: 'Crafted',
	cooking: 'Cooked',
	// Gathering, fishing and farming give an item again and again, so they lead the ways found in the world.
	gathering: 'Gathered',
	fishing: 'Fished',
	farming: 'Grown',
	containerLoot: 'Found in Containers',
	enemyDrop: 'Dropped by',
	pickup: 'Picked Up',
	dialogue: 'Given in Dialogue',
	bountyNote: 'Obtained from Bounty Notes',
	settlementDelivery: 'Delivered by Settlements',
	stable: 'Sold at Stables',
	contract: 'From Contracts',
	seasonalGift: 'Seasonal Gifts',
	archaeology: 'Dug Up',
	reward: 'Rewarded',
	// Random loot names no place or chance, so it follows the ways that do.
	randomLoot: 'Found as Random Loot'
};

export const sourceIntros: Partial<Record<SourceKind, string>> = {
	settlementDelivery:
		'Completed buildings in your settlement add resources every game week. Collect them under Resources Ready in the settlement overview.'
};
/** What a source table's first column lists for each way of getting an item. */
export const sourceColumns: Record<SourceKind, string> = {
	shop: 'Merchant',
	currencyShop: 'Merchant',
	smithing: 'Recipe',
	crafting: 'Recipe',
	cooking: 'Recipe',
	containerLoot: 'Container',
	randomLoot: 'Loot',
	enemyDrop: 'Creature',
	pickup: 'Item',
	dialogue: 'Person',
	fishing: 'Fishing',
	gathering: 'Resource',
	farming: 'Crop',
	bountyNote: 'Bounty Note',
	settlementDelivery: 'Building',
	stable: 'Stable',
	contract: 'Contract',
	seasonalGift: 'Gift',
	archaeology: 'Find',
	reward: 'Reward'
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
	settlementDelivery: 'Settlements',
	stable: 'Stables',
	contract: 'Contracts',
	seasonalGift: 'Seasonal gifts',
	archaeology: 'Archaeology',
	reward: 'Rewards'
};
