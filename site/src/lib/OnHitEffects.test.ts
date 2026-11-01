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
		effect: 'Burning Ground',
		trigger: 'Melee Hit',
		chance: 0.1,
		amount: null,
		tickIntervalSeconds: null,
		durationSeconds: 3,
		behavior: null
	}
];
const text = (body: string) => body.replace(/<!--[\s\S]*?-->/g, '').replace(/\s+/g, ' ');

it('renders tick damage, duration, mana and ground effects without missing values', () => {
	const body = text(render(OnHitEffects, { props: { effects } }).body);
	expect(body).toContain('5 damage every 0.5 seconds for 4 seconds.');
	expect(body).toContain(`4 seconds. ${effects[0].behavior}`);
	expect(body).toContain('Restores 10 mana.');
	expect(body).toContain('Sets the ground under the enemy on fire for 3 seconds.');
	expect(body).not.toContain('null');
	expect(body).not.toContain('undefined');
	expect(body).not.toMatch(/class="[^"]*\bstacked\b/);
	expect(body.match(/scope="col"/g)).toHaveLength(3);
	expect(body.match(/scope="row"/g)).toHaveLength(effects.length);
});

it.each<[number, string]>([
	[0.2, '20% per hit'],
	[0.07, '7% per hit'],
	[1 / 6, '1 in 6 per hit'],
	[1, 'Every hit']
])('formats an on-hit chance of %s as %s', (chance, expected) => {
	const body = text(render(OnHitEffects, { props: { effects: [{ ...effects[0], chance }] } }).body);
	expect(body).toContain(`>${expected}</td>`);
});

it.each<[number, string]>([
	[1, 'per second'],
	[2, 'every 2 seconds']
])('preserves a tick interval of %s in the damage description', (tickIntervalSeconds, interval) => {
	const body = text(
		render(OnHitEffects, {
			props: { effects: [{ ...effects[0], tickIntervalSeconds }] }
		}).body
	);
	expect(body).toContain(`5 damage ${interval} for 4 seconds.`);
});

it('uses the aimed-shot condition for each Holy Noose effect', () => {
	const trigger = 'Every shot aimed at an undead enemy';
	const body = text(
		render(OnHitEffects, {
			props: { effects: effects.slice(0, 2).map((effect) => ({ ...effect, trigger, chance: 1 })) }
		}).body
	);
	expect(body.split(trigger)).toHaveLength(3);
	expect(body).not.toContain('Every hit');
});

it('preserves unknown amounts and durations without displaying null', () => {
	const body = text(
		render(OnHitEffects, {
			props: {
				effects: [
					{ ...effects[0], amount: null },
					{ ...effects[2], durationSeconds: null }
				]
			}
		}).body
	);
	expect(body.match(/Unknown/g)).toHaveLength(2);
	expect(body).not.toContain('null');
	expect(body).not.toContain('undefined');
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
	expect(preview.match(/scope="row"/g)).toHaveLength(effects.length);
	const card = render(ItemTooltip, { props: { item, details: false } }).body;
	expect(card).not.toContain('On-Hit Effects');
	const withoutEffects = render(ItemTooltip, {
		props: { item: { ...item, onHitEffects: null } }
	}).body;
	expect(withoutEffects).not.toContain('On-Hit Effects');
	expect(withoutEffects).not.toContain('<table');
});
