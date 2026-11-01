import { render } from 'svelte/server';
import { expect, it } from 'vitest';
import OnHitEffects from './OnHitEffects.svelte';
import ItemTooltip from './ItemTooltip.svelte';
import type { ItemPage } from './site-data.js';

const effects: NonNullable<ItemPage['onHitEffects']> = [
	{
		effect: 'Bleed',
		trigger: 'Melee Hit',
		chance: 0.25,
		amount: 5,
		tickIntervalSeconds: 0.5,
		durationSeconds: 4,
		behavior: 'Refreshes Duration'
	},
	{
		effect: 'Restore Mana',
		trigger: 'Melee Hit',
		chance: 1,
		amount: 10,
		tickIntervalSeconds: null,
		durationSeconds: null,
		behavior: null
	},
	{
		effect: 'Fire Ground',
		trigger: 'Melee Hit',
		chance: 0.1,
		amount: null,
		tickIntervalSeconds: null,
		durationSeconds: 3,
		behavior: null
	}
];
const text = (body: string) => body.replace(/<!--[\s\S]*?-->/g, '').replace(/\s+/g, ' ');

it('shows on-hit triggers, fractional chances, tick damage, instant mana and refresh behavior', () => {
	const body = text(render(OnHitEffects, { props: { effects } }).body);
	for (const heading of ['Effect', 'When', 'Chance', 'Amount', 'Duration']) {
		expect(body).toContain(`>${heading}</th>`);
	}
	expect(body).toContain('On-Hit Effects');
	expect(body).toContain('>Bleed</th>');
	expect(body).toContain('>Melee Hit</td>');
	expect(body).toContain('>25%</td>');
	expect(body).toContain('5 Damage Every 0.5 s');
	expect(body).toMatch(/>\s*4 s\s*<small>Refreshes Duration<\/small>\s*<\/td>/);
	expect(body).toContain('>100%</td>');
	expect(body).toContain('10 Mana Instantly');
	expect(body).toMatch(/>\s*Instant\s*<\/td>/);
	expect(body).toContain('>Fire Ground</th>');
	expect(body).toMatch(/>\s*Unknown\s*<\/td>/);
	expect(body).toMatch(/>\s*3 s\s*<\/td>/);
	expect(body).not.toContain('null');
	expect(body).not.toContain('undefined');
	// The fifth column keeps its heading when the relation table stacks on phones.
	expect(body).toContain('--label-5:');
});

it('leaves the no-effects layout unchanged for absent, null and empty effects', () => {
	for (const effects of [undefined, null, []]) {
		const { body } = render(OnHitEffects, { props: { effects } });
		expect(body.replace(/<!--[\s\S]*?-->/g, '').trim()).toBe('');
	}
});

it('includes on-hit effects in previews but not the page game card', () => {
	const item = {
		name: 'Effect Blade',
		icon: null,
		description: null,
		facts: [],
		onHitEffects: effects
	};
	const preview = text(render(ItemTooltip, { props: { item } }).body);
	expect(preview).toContain('On-Hit Effects');
	expect(preview).toContain('>Bleed</th>');
	expect(preview).toContain('10 Mana Instantly');
	const card = render(ItemTooltip, { props: { item, details: false } }).body;
	expect(card).not.toContain('On-Hit Effects');
	const withoutEffects = render(ItemTooltip, {
		props: { item: { ...item, onHitEffects: null } }
	}).body;
	expect(withoutEffects).not.toContain('On-Hit Effects');
	expect(withoutEffects).not.toContain('<table');
});
