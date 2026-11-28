import type { RelationCondition, SourceEventKind, SourceKind } from './site-data.js';
export function titleCase(value: string): string {
	return value
		.replace(/([a-z])([A-Z])/g, '$1 $2')
		.replace(/[_-]+/g, ' ')
		.toLowerCase()
		.replace(/\b[a-z]/g, (letter) => letter.toUpperCase());
}

/** The one name of each way to get an item: its item-page heading, filter, Home row and search description. */
export const sourceHeadings: Record<SourceKind, string> = {
	// Only skills and spellsongs come from these, and a skill tree is how most skills are learned, so it leads.
	skillTree: 'Skill Tree',
	skillBook: 'Skill Book',
	noteSheet: 'Note Sheet',
	event: 'Event',
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
	debuffKill: 'Harvested from Kills',
	pickup: 'Picked Up',
	dialogue: 'Given in Dialogue',
	bountyNote: 'Obtained from Bounty Notes',
	settlementDelivery: 'Delivered by Settlements',
	stable: 'Sold at Stables',
	contract: 'From Contracts',
	seasonalGift: 'Seasonal Gifts',
	archaeology: 'Dug Up',
	chiseling: 'Chiseled',
	reward: 'Rewarded',
	// Random loot names no place or chance, so it follows the ways that do.
	randomLoot: 'Found as Random Loot'
};

export const sourceIntros: Partial<Record<SourceKind, string>> = {
	settlementDelivery:
		'Completed buildings in your settlement add resources every game week. Collect them under Resources Ready in the settlement overview.',
	skillTree:
		'A skill costs 1 [skill point](mechanic:skill-points) once its discipline reaches the required level. Clicking it again refunds the point.',
	debuffKill:
		'Each of these applies a debuff. A creature that dies while the debuff lasts gives you 1 of this item.'
};
/** What is known of a way that stands somewhere but that no area places, for one row and for several alike. */
export const placeUnknownReasons: Partial<Record<SourceKind, string>> = {
	enemyDrop: 'No fixed spawn in any area.',
	containerLoot: 'No fixed spot in any area.',
	dialogue: 'Not placed in any area.'
};
/** What a row calls the events that bring its giver, for one event and for several. */
export const eventLabels: Record<SourceEventKind, [string, string]> = {
	worldMap: ['World map event:', 'World map events:'],
	calendar: ['Calendar event:', 'Calendar events:']
};
/** The level a source kind's rows need, shown as its own column, such as the discipline level of a skill tree. */
export const sourceLevels: Partial<
	Record<SourceKind, { condition: RelationCondition['kind']; heading: string }>
> = {
	skillTree: { condition: 'disciplineLevel', heading: 'Required Level' }
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
	debuffKill: 'Applied by',
	pickup: 'Location',
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
	chiseling: 'Station',
	reward: 'Reward',
	skillTree: 'Discipline',
	skillBook: 'Location',
	noteSheet: 'Location',
	event: 'Event'
};
