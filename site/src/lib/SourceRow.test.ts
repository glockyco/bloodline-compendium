import { render } from 'svelte/server';
import { expect, it, vi } from 'vitest';
import SourceRow from './SourceRow.svelte';
import fixture from '../../fixtures/site-data/items/lanternedge.json';
import type { ItemPage, Source } from './site-data.js';

vi.mock('$app/state', () => ({ page: { data: { kinds: [] } } }));

const container = (fixture as ItemPage).sources.find((source) => source.kind === 'containerLoot')!;
const spawnCell = (spawn: Source['spawn']) => {
	const { body } = render(SourceRow, {
		props: { source: { ...container, spawn }, showSpawn: true, id: 'spawn-test' }
	});
	return body.match(/<td class="numeric spawn-value[^"]*">([\s\S]*?)<\/td>/)?.[1];
};

it('shows Always for containers without spawn odds or with a guaranteed spawn', () => {
	expect(spawnCell(undefined)).toContain('Always');
	expect(spawnCell(null)).toContain('Always');
	expect(spawnCell({ min: 1, max: 1 })).toContain('Always');
});

it('renders fixed and ranged container spawn chances', () => {
	expect(spawnCell({ min: 0.25, max: 0.25 })).toContain('25%');
	expect(spawnCell({ min: 0.25, max: 1 })).toContain('25% to 100%');
	expect(spawnCell({ min: 0, max: 0 })).toContain('0%');
});

it('renders both loot chance bounds without adding a spawn cell by default', () => {
	const { body } = render(SourceRow, {
		props: {
			source: {
				...container,
				odds: { ...container.odds!, chance: 0.125, highestChance: 0.75 }
			},
			showContext: true,
			id: 'chance-test'
		}
	});
	expect(body).toContain('12.5% to 75%');
	expect(body).toContain('per container');
	expect(body).not.toContain('spawn-value');
});

it.each([undefined, null])('renders a fixed chance when the upper bound is %s', (highestChance) => {
	const { body } = render(SourceRow, {
		props: {
			source: { ...container, odds: { ...container.odds!, chance: 0.25, highestChance } },
			showContext: true,
			id: 'fixed-chance-test'
		}
	});
	expect(body).toContain('25%');
	expect(body).not.toContain('25% to');
});
