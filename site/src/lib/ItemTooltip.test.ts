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
	effect: null
});

it('preserves each projectile group and unknown damage in page Details and previews', () => {
	const projectileGroups = [
		{ count: 1, projectile: 'bullet', bodyHitDamage: 60, headHitDamage: 120 },
		{ count: 6, projectile: 'pellets', bodyHitDamage: 10, headHitDamage: 20 },
		{ count: 2, projectile: 'projectiles', bodyHitDamage: null, headHitDamage: null }
	];
	const item = {
		...fixture,
		projectileGroups,
		facts: [
			rangedFact('bodyHitDamage', 'Damage', 60),
			rangedFact('headHitDamage', 'Headshot', 120),
			rangedFact('projectilesPerShot', 'Projectiles per Shot', 9)
		]
	};
	const preview = render(ItemTooltip, { props: { item } }).body;
	const pageDetails = render(ItemDetails, {
		props: { rows: detailRows(item.facts, projectileGroups), projectileGroups, wide: true }
	}).body;
	for (const body of [preview, pageDetails]) {
		const cells = [...body.replace(/<!--[\s\S]*?-->/g, '').matchAll(/<td[^>]*>([^<]*)<\/td>/g)].map(
			(match) => match[1].replace(/\s+/g, ' ').trim()
		);
		expect(cells).toEqual([
			'1 bullet',
			'60',
			'120',
			'6 pellets',
			'10 each',
			'20 each',
			'2 projectiles',
			'Unknown',
			'Unknown'
		]);
		expect(rows(body, 'dt')).toEqual([]);
	}
	const singleGroup = render(ItemTooltip, {
		props: { item: { ...item, projectileGroups: projectileGroups.slice(0, 1) } }
	}).body;
	expect(singleGroup).toContain('<table');
	expect(singleGroup.replace(/\s+/g, ' ')).not.toContain('6 pellets');
});

it('keeps a conditional bolt and charge damage separate from the ordinary projectile group', () => {
	const projectileGroups = [
		{ count: 1, projectile: 'bolt', bodyHitDamage: 85, headHitDamage: 185, condition: null },
		{
			count: 1,
			projectile: 'bolt',
			bodyHitDamage: 100,
			headHitDamage: 200,
			condition: 'Point blank'
		}
	];
	const item = {
		...fixture,
		projectileGroups,
		facts: [
			rangedFact('bodyHitDamage', 'Damage', 85),
			rangedFact('chargedBodyHitDamage', 'Fully charged', 110.5)
		]
	};
	const preview = render(ItemTooltip, { props: { item } }).body;
	const pageDetails = render(ItemDetails, {
		props: {
			rows: detailRows(item.facts, projectileGroups),
			projectileGroups,
			chargedBodyHitDamage: 110.5,
			wide: true
		}
	}).body;
	for (const body of [preview, pageDetails]) {
		expect(body.replace(/<!--[\s\S]*?-->/g, '').replace(/\s+/g, ' ')).toContain(
			'Point blank: 1 extra bolt'
		);
		expect(body).toContain('110.5');
		expect(rows(body, 'dt')).toEqual([]);
	}
	const uncharged = render(ItemTooltip, { props: { item: { ...item, facts: [] } } }).body;
	expect(uncharged).not.toContain('110.5');
	const unknownCharge = render(ItemTooltip, {
		props: {
			item: {
				...item,
				facts: [{ ...rangedFact('chargedBodyHitDamage', 'Fully charged', 110.5), number: null }]
			}
		}
	}).body;
	expect(unknownCharge).toContain('Unknown');
	expect(unknownCharge).not.toContain('110.5');
});
it('shows shield block facts in details and previews without inventing damage', () => {
	const facts: Fact[] = [
		{
			stat: 'blockStaminaCost',
			label: 'Blocked Hits',
			number: 50,
			unit: '%',
			text: null,
			effect: null
		},
		{
			stat: 'extraFullBlocks',
			label: 'Free Blocks',
			number: 6,
			text: null,
			effect: null
		}
	];
	const preview = render(ItemTooltip, { props: { item: { ...fixture, facts } } }).body;
	const details = render(ItemDetails, { props: { rows: detailRows(facts), wide: true } }).body;
	for (const body of [preview, details]) {
		expect(body).not.toContain('Damage');
	}
});
