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
