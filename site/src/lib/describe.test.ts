import { expect, it } from 'vitest';
import { describeItem, isPlural, typePhrase } from './describe.js';
import fixture from '../../fixtures/site-data/items/lanternedge.json';
import type { Fact, ItemPage, Source } from './site-data.js';
const sword = fixture as ItemPage;
const shop = sword.sources.find((source) => source.kind === 'shop')!;
const fact = (stat: string, number: number | null, text: string | null = null) =>
	({ stat, number, text, label: stat, effect: null }) as Fact;
const item = (name: string, category: string, type: string, facts: Fact[] = []): ItemPage => ({
	...sword,
	name,
	category: { name: category, slug: category.toLowerCase() },
	type,
	facts,
	status: 'obtainable'
});

it('opens with what the item is and its damage, then names the first seller with its price', () => {
	expect(describeItem(sword, [shop])).toBe(
		'Lanternedge is a sword in The Bloodline. It deals 12 damage. Sold by Mira at the Glass Market in Glass Reach for 63 gold.'
	);
	expect(typePhrase(item('Short Sword', 'Weapon', 'One Handed Weapon'))).toBe(
		'a one-handed weapon'
	);
});

it('enumerates heterogeneous projectile hits instead of applying the first hit to the whole shot', () => {
	const gun = {
		...item('Rusty Scattergun', 'Weapon', 'Gun', [
			fact('bodyHitDamage', 60),
			fact('headHitDamage', 120),
			fact('projectilesPerShot', 7)
		]),
		projectileGroups: [
			{ count: 1, projectile: 'bullet', bodyHitDamage: 60, headHitDamage: 120 },
			{ count: 6, projectile: 'pellets', bodyHitDamage: 10, headHitDamage: 20 }
		]
	};
	const description = describeItem(gun, []);
	expect(description).toContain('60 damage, 120 on a headshot');
	expect(description).toContain('6 pellets');
	expect(description).toContain('10 damage each, 20 on a headshot');
	expect(description).not.toContain('7 pellets');
});

it('leaves conditional projectile groups out of the short description', () => {
	const crossbow = {
		...item('Ash Crossbow', 'Weapon', 'Crossbow', [
			fact('bodyHitDamage', 85),
			fact('headHitDamage', 185),
			fact('projectilesPerShot', 1)
		]),
		projectileGroups: [
			{ count: 1, projectile: 'bolt', bodyHitDamage: 85, headHitDamage: 185, condition: null },
			{
				count: 1,
				projectile: 'bolt',
				bodyHitDamage: 100,
				headHitDamage: 200,
				condition: 'Point blank'
			}
		]
	};
	const description = describeItem(crossbow, []);
	expect(description).toContain('85 damage, 185 on a headshot');
	expect(description).not.toContain('100');
	expect(description).not.toContain('200');
	expect(description).not.toContain('Point blank');
});

it('uses each group noun, preserves unknown damage and agrees with plural weapon names', () => {
	const weapon = {
		...item('Twin Bows', 'Weapon', 'Bow'),
		projectileGroups: [{ count: 3, projectile: 'arrows', bodyHitDamage: null, headHitDamage: 0 }]
	};
	const description = describeItem(weapon, []);
	expect(description).toContain('They fire 3 arrows');
	expect(description).toContain('Unknown damage each, 0 on a headshot');
	expect(description).not.toContain('null');
});

it('agrees with names whose head noun is plural', () => {
	const gauntlets = item('Iron Gauntlets', 'Armor', 'Arms', [
		fact('armorType', null, 'Medium'),
		fact('armorRating', 9)
	]);
	expect(describeItem(gauntlets, [])).toBe(
		'Iron Gauntlets are medium arm armor in The Bloodline. They add 9 armor.'
	);
	expect(typePhrase(item("Bandit's Daggers", 'Weapon', 'Dagger'))).toBe('daggers');
	for (const name of [
		'Steel Wand Focus',
		'Askillian Gladus',
		'Magnifying Lens',
		'Staff of Minor Flames'
	])
		expect(isPlural(name)).toBe(false);
	for (const name of [
		'Leather Pants',
		'Spaulders of the Rootfather',
		'Cloth Sleeves and Leather Bracers'
	])
		expect(isPlural(name)).toBe(true);
});

it('keeps the game name plain when a version has a label', () => {
	const soulstaff = { ...item('Soulstaff', 'Weapon', 'Staff'), versionLabel: '24 Damage' };
	expect(describeItem(soulstaff, [])).toBe('Soulstaff is a staff in The Bloodline.');
});

it('leaves out a zero number and names jewelry, support items and weapon parts', () => {
	expect(
		describeItem(item('Gravecoil Ring', 'Armor', 'Amulet', [fact('armorRating', 0)]), [])
	).toBe('Gravecoil Ring is jewelry in The Bloodline.');
	expect(typePhrase(item('Simple Quiver', 'Armor', 'Support'))).toBe('a support item');
	expect(typePhrase(item('Iron Ring Pommel', 'Weapon Part', 'Pommel'))).toBe(
		'a pommel for smithing weapons'
	);
	expect(typePhrase(item('Iron Axe Head', 'Weapon Part', 'Axe Head'))).toBe(
		'an axe head for smithing weapons'
	);
});

it('falls back to the status sentence of an item without a known way to get it', () => {
	expect(
		describeItem({ ...item('Leather Pants', 'Armor', 'Legs'), status: 'noKnownSource' }, [])
	).toBe(
		'Leather Pants are leg armor in The Bloodline. No known source. It may come from a quest or event.'
	);
	expect(
		describeItem({ ...item('Test Mesh Torso 2', 'Armor', 'Torso'), status: 'notInNormalPlay' }, [])
	).toBe(
		'Test Mesh Torso 2 is torso armor in The Bloodline. This item cannot be obtained in normal play. It only appears in test areas.'
	);
});

it('gives a first name that shares its place with the next one that place', () => {
	const crate = (name: string): Source => ({
		...shop,
		kind: 'containerLoot',
		name,
		price: null,
		places: [{ ...shop.places[0], name: 'Wolfden Priory' }]
	});
	expect(
		describeItem(item('Bandit Knife', 'Material', 'Material'), [crate('Chest'), crate('Crate')])
	).toBe('Bandit Knife is a material in The Bloodline. Found in Chest in Wolfden Priory.');
});
