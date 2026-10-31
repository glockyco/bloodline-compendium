import { render } from 'svelte/server';
import { expect, it } from 'vitest';
import ItemTooltip from './ItemTooltip.svelte';
import fixture from '../../fixtures/site-data/items/mosswoven-coat.json';
import type { Fact, ItemPage } from './site-data.js';

it('links each tooltip difference only to the issue that explains that fact', () => {
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
	const links = body.match(/<a\b[^>]*>[\s\S]*?<\/a>/g) ?? [];

	expect(links).toHaveLength(1);
	expect(links[0]).toContain('/known-issues/#invented-coat-armor');
	expect(links[0]).toContain('Armor 8');
	expect(links[0]).not.toContain('Damage');
	expect(body).toContain("The game's tooltip shows Damage");
});

it('keeps a tooltip difference unlinked when no known issue explains it', () => {
	const item = {
		...fixture,
		facts: (fixture as ItemPage).facts.map((fact) => ({ ...fact, gameShowsIssue: null }))
	};
	const { body } = render(ItemTooltip, { props: { item } });

	expect(body).toContain("The game's tooltip shows Armor");
	expect(body).not.toContain('<a ');
});
