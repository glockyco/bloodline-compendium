import { render } from 'svelte/server';
import { expect, it } from 'vitest';
import ItemTooltip from './ItemTooltip.svelte';
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

it("lists a food's healing beside its other effects, even when another effect restores the same amount", () => {
	const item = { ...fixture, facts: [effect('Restore Mana', 100), restoration(100)] };
	const { body } = render(ItemTooltip, { props: { item } });
	expect(rows(body, 'dt')).toEqual(['Effects']);
	expect(rows(body, 'dd')).toEqual(['Restore Mana 100', 'Restore Health 100']);
});

it("leaves out a potion's restoration amount that repeats its restore effect", () => {
	const item = { ...fixture, facts: [effect('Restore Magic', 75), restoration(75)] };
	const { body } = render(ItemTooltip, { props: { item } });
	expect(rows(body, 'dd')).toEqual(['Restore Magic 75']);
});
