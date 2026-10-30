import { expect, it } from 'vitest';
import { listPage, listRecords } from './search-lists.js';
import type { ItemIndex, ItemSummary } from './site-data.js';

const weapon = (name: string, type: string, status = 'obtainable'): ItemSummary =>
	({
		slug: name.toLowerCase().replaceAll(' ', '-'),
		name,
		category: 'weapon',
		type,
		status,
		facets: { category: ['weapon'], weaponType: [type] },
		cells: {}
	}) as unknown as ItemSummary;

const index = {
	categories: [],
	columns: [],
	facets: [
		{
			id: 'category',
			label: 'Category',
			categories: [],
			values: ['weapon'],
			labels: { weapon: 'Weapons' }
		},
		{
			id: 'weaponType',
			label: 'Weapon Type',
			categories: ['weapon'],
			values: ['One Handed Weapon', 'Two Handed Weapon', 'Dagger', 'Spear'],
			labels: {}
		}
	],
	items: [
		weapon('Iron Sword', 'One Handed Weapon'),
		weapon('Steel Sword', 'One Handed Weapon'),
		weapon('Bone Sword', 'One Handed Weapon'),
		weapon('Iron Mace', 'One Handed Weapon'),
		weapon('Iron Ultrasword', 'Two Handed Weapon'),
		weapon('Steel Ultrasword', 'Two Handed Weapon'),
		weapon('Greatsword', 'Two Handed Weapon'),
		weapon('Test Dagger', 'Dagger', 'notInNormalPlay'),
		weapon('Iron Daggers', 'Spear'),
		weapon('Rose Dagger', 'Spear'),
		weapon('Bone Daggers', 'Spear')
	]
} as unknown as ItemIndex;

it('leads category and type words to the filtered item list', () => {
	const records = listRecords(index, '/items/');
	expect(records.map((record) => [record.title, record.url])).toEqual([
		['Weapons', '/items/?category=weapon'],
		['Weapons: One Handed Weapon', '/items/?category=weapon&weaponType=One+Handed+Weapon'],
		['Weapons: Two Handed Weapon', '/items/?category=weapon&weaponType=Two+Handed+Weapon'],
		['Weapons: Spear', '/items/?category=weapon&weaponType=Spear']
	]);
	// Compounds such as Ultrasword name the sword lists, and a noun on fewer than three items names none.
	const words = Object.fromEntries(records.map((record) => [record.title, record.words]));
	expect(words['Weapons: One Handed Weapon']).toEqual(['swords']);
	expect(words['Weapons: Two Handed Weapon']).toEqual(['swords']);
	// Dagger and Daggers are one noun, and the hidden Test Dagger leaves its type without a list.
	expect(words['Weapons: Spear']).toEqual(['daggers']);
	expect(listPage(records[1])).toContain('>Weapons: One Handed Weapon, such as swords</h1>');
	expect(listPage(records[1])).toContain('<p data-pagefind-weight="10">Names include swords.</p>');
});
