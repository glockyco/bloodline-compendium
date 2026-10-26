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
	containerLoot: 'Found in Containers',
	randomLoot: 'Found as Random Loot',
	enemyDrop: 'Dropped by',
	pickup: 'Picked Up',
	dialogue: 'Given in Dialogue',
	fishing: 'Fished',
	gathering: 'Gathered',
	farming: 'Grown',
	bountyNote: 'Obtained from Bounty Notes',
	settlementDelivery: 'Delivered by Settlements',
	stable: 'Sold at Stables',
	contract: 'From Contracts',
	seasonalGift: 'Seasonal Gifts',
	archaeology: 'Dug Up',
	reward: 'Rewarded'
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
