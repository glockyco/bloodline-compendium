import { render } from 'svelte/server';
import { expect, it } from 'vitest';
import ItemTooltip from './ItemTooltip.svelte';
import ItemDetails from './ItemDetails.svelte';
import { detailRows } from './game-card.js';
import fixture from '../../fixtures/site-data/items/mosswoven-coat.json';
import type { Fact, ItemPage } from './site-data.js';

it('shows applied values without tooltip differences or issue links', () => {
	const damage: Fact = {
		stat: 'damage',
		label: 'Damage',
		number: 10,
		text: null,
		effect: null,
		gameShows: '20'
	};
	const item = { ...fixture, facts: [...(fixture as ItemPage).facts, damage] };
	const { body } = render(ItemTooltip, { props: { item } });
	expect(body).toContain('Damage: 10');
	expect(body).not.toContain("The game's tooltip shows");
	expect(body).not.toContain('known-issues/');
});

it('omits unexplained tooltip differences from previews', () => {
	const item = {
		...fixture,
		facts: (fixture as ItemPage).facts.map((fact) => ({ ...fact, gameShowsIssue: null }))
	};
	const { body } = render(ItemTooltip, { props: { item } });
	expect(body).toContain('ARMOR: 0');

	expect(body).not.toContain("The game's tooltip shows");
	expect(body).not.toContain('<a ');
});

const effect = (name: string, power: number): Fact => ({
	stat: 'effect',
	label: 'Effect 1',
	number: null,
	text: null,
	effect: { name, power, seconds: 0 }
});
const restoration = (number: number): Fact => ({
	stat: 'restorationAmount',
	label: 'Restoration amount',
	number,
	text: null,
	effect: null
});
// The labels and values of the rows below the game card, in order.
const rows = (body: string, tag: 'dt' | 'dd') =>
	[...body.matchAll(new RegExp(`<${tag}[^>]*>([^<]*)</${tag}>`, 'g'))].map((match) => match[1]);

it('lists effects in one row and leaves out the restoration amount they already carry', () => {
	const item = {
		...fixture,
		facts: [effect('Restore Mana', 100), restoration(100), effect('Restore Health', 100)]
	};
	const { body } = render(ItemTooltip, { props: { item } });
	expect(rows(body, 'dt')).toEqual(['Effects']);
	expect(rows(body, 'dd')).toEqual(['Restore Mana 100', 'Restore Health 100']);
});

const rangedFact = (stat: Fact['stat'], label: string, number: number): Fact => ({
	stat,
	label,
	number,
	text: null,
	effect: null,
	unit: stat === 'projectilesPerShot' ? null : 'per projectile'
});

it('shows ranged applied hit facts in the card, details and link previews without generic damage', () => {
	const facts = [
		rangedFact('bodyHitDamage', 'Body Hit Damage', 8),
		rangedFact('headHitDamage', 'Head Hit Damage', 16),
		rangedFact('projectilesPerShot', 'Projectiles Per Shot', 3),
		rangedFact('chargedBodyHitDamage', 'Fully Charged Body Hit Damage', 24)
	];
	const item = { ...fixture, facts };
	const preview = render(ItemTooltip, { props: { item } }).body;
	expect(preview).toContain('Body Hit Damage: 8 per projectile');
	expect(preview).not.toContain('>Damage:');
	expect(rows(preview, 'dt')).toEqual([
		'Body Hit Damage',
		'Head Hit Damage',
		'Projectiles Per Shot',
		'Fully Charged Body Hit Damage'
	]);
	expect(rows(preview, 'dd')).toEqual([
		'8 per projectile',
		'16 per projectile',
		'3',
		'24 per projectile'
	]);
	const aside = render(ItemTooltip, { props: { item, details: false } }).body;
	expect(aside).toContain('Body Hit Damage: 8 per projectile');
	expect(rows(aside, 'dt')).toEqual([]);
});

it('omits charged hit details when the weapon has no charged fact', () => {
	const item = {
		...fixture,
		facts: [
			rangedFact('bodyHitDamage', 'Body Hit Damage', 8),
			rangedFact('headHitDamage', 'Head Hit Damage', 16),
			rangedFact('projectilesPerShot', 'Projectiles Per Shot', 1)
		]
	};
	const { body } = render(ItemTooltip, { props: { item } });
	expect(body).not.toContain('Fully Charged Body Hit Damage');
});

it('shows every projectile group in page Details and previews without treating the first group as every hit', () => {
	const projectileGroups = [
		{ count: 1, bodyHitDamage: 60, headHitDamage: 120 },
		{ count: 6, bodyHitDamage: 10, headHitDamage: 20 },
		{ count: 2, bodyHitDamage: null, headHitDamage: null }
	];
	const item = {
		...fixture,
		projectileGroups,
		facts: [
			rangedFact('bodyHitDamage', 'Body Hit Damage', 60),
			rangedFact('headHitDamage', 'Head Hit Damage', 120),
			rangedFact('projectilesPerShot', 'Projectiles Per Shot', 9)
		]
	};
	const preview = render(ItemTooltip, { props: { item } }).body;
	const pageDetails = render(ItemDetails, {
		props: { rows: detailRows(item.facts, projectileGroups), projectileGroups, wide: true }
	}).body;
	for (const body of [preview, pageDetails]) {
		expect(body).toMatch(/<caption[^>]*>Projectile Hits<\/caption>/);
		expect(body).toContain('>Projectiles</th>');
		expect(body).toContain('>Body Hit Damage</th>');
		expect(body).toContain('>Head Hit Damage</th>');
		const cells = [...body.replace(/<!--[\s\S]*?-->/g, '').matchAll(/<td[^>]*>([^<]*)<\/td>/g)].map(
			(match) => match[1].trim()
		);
		expect(cells).toEqual([
			'1',
			'60 per projectile',
			'120 per projectile',
			'6',
			'10 per projectile',
			'20 per projectile',
			'2',
			'Unknown',
			'Unknown'
		]);
		expect(rows(body, 'dt')).toEqual(['Projectiles Per Shot']);
		expect(rows(body, 'dd')).toEqual(['9']);
	}
	expect(preview).toContain('Body Hit Damage: 60 per projectile');
	const singleGroup = render(ItemTooltip, {
		props: { item: { ...item, projectileGroups: projectileGroups.slice(0, 1) } }
	}).body;
	expect(singleGroup).not.toContain('<table>');
	expect(rows(singleGroup, 'dt')).toEqual([
		'Body Hit Damage',
		'Head Hit Damage',
		'Projectiles Per Shot'
	]);
});

it('keeps a conditional crossbow bolt separate from the unconditional shot count', () => {
	const projectileGroups = [
		{ count: 1, bodyHitDamage: 85, headHitDamage: 185, condition: null },
		{ count: 1, bodyHitDamage: 100, headHitDamage: 200, condition: 'At close range, also' }
	];
	const item = {
		...fixture,
		projectileGroups,
		facts: [
			rangedFact('bodyHitDamage', 'Body Hit Damage', 85),
			rangedFact('headHitDamage', 'Head Hit Damage', 185),
			rangedFact('projectilesPerShot', 'Projectiles Per Shot', 1)
		]
	};
	const preview = render(ItemTooltip, { props: { item } }).body;
	const pageDetails = render(ItemDetails, {
		props: { rows: detailRows(item.facts, projectileGroups), projectileGroups, wide: true }
	}).body;
	for (const body of [preview, pageDetails]) {
		expect(body).toContain('>At close range, also</span>');
		expect(body).toContain('100 per projectile');
		expect(body).toContain('200 per projectile');
		expect(rows(body, 'dt')).toEqual(['Projectiles Per Shot']);
		expect(rows(body, 'dd')).toEqual(['1']);
	}
});
