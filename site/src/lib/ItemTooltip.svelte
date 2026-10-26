<script lang="ts">
	import type { Fact } from './site-data.js';
	import { titleCase } from './labels.js';

	interface TooltipItem {
		name: string;
		icon: string | null;
		description: string | null;
		facts: Fact[];
	}
	let { item, heading = 'h2' }: { item: TooltipItem; heading?: 'h2' | 'p' } = $props();

	/**
	 * The game's tooltip shows two stat lines beside the icon, under these labels. Other facts follow
	 * below the description, in the order the catalog publishes them.
	 */
	const primaryLabels: Record<string, string> = {
		'Armor type': 'Type',
		'Armor rating': 'Armor',
		'Weapon type': 'Type',
		Damage: 'Damage',
		'Attack speed': 'Speed',
		'Restoration amount': 'Heals'
	};
	const primary = $derived(item.facts.filter((fact) => fact.label in primaryLabels));
	const rest = $derived(item.facts.filter((fact) => !(fact.label in primaryLabels)));
	function shown(fact: Fact) {
		if (fact.value === null) return 'Unknown';
		return fact.label === 'Value' ? `${fact.value} gold` : fact.value;
	}
</script>

<div class="tooltip">
	<svelte:element this={heading} class="name">{item.name}</svelte:element>
	<div class="body">
		<div class="slot">
			{#if item.icon}<img src="/icons/{item.icon}-160.webp" alt="" width="80" height="80" />{/if}
		</div>
		{#if primary.length}<dl class="primary">
				{#each primary as fact (fact.label)}<div>
						<dt>{primaryLabels[fact.label]}</dt>
						<dd class:unknown={fact.value === null}>{shown(fact)}</dd>
					</div>{/each}
			</dl>{/if}
	</div>
	<p class="description">{item.description ?? 'Description unknown.'}</p>
	{#if rest.length}<dl class="rest">
			{#each rest as fact (fact.label)}<div>
					<dt>{titleCase(fact.label)}</dt>
					<dd class:unknown={fact.value === null}>{shown(fact)}</dd>
				</div>{/each}
		</dl>{/if}
</div>

<style>
	.tooltip {
		display: grid;
		gap: var(--space-4);
		padding: var(--space-5);
		border: var(--marker-width) solid var(--tooltip-frame);
		border-radius: var(--radius-1);
		background: radial-gradient(var(--tooltip-surface), var(--tooltip-surface-edge));
		box-shadow: var(--shadow-raised);
		color: var(--tooltip-text);
		font-family: var(--font-game);
	}
	/* The cream ribbon with notched ends that carries the item name in the game. */
	.name {
		margin: 0;
		padding: var(--space-1) var(--space-6);
		background: var(--tooltip-ribbon);
		color: var(--tooltip-ink);
		font-family: var(--font-game);
		font-size: var(--type-2);
		font-weight: var(--weight-regular);
		line-height: var(--leading-snug);
		text-align: center;
		text-wrap: balance;
		clip-path: polygon(0 0, 100% 0, 96% 50%, 100% 100%, 0 100%, 4% 50%);
	}
	.body {
		display: grid;
		grid-template-columns: var(--size-icon-title) minmax(0, 1fr);
		align-items: center;
		gap: var(--space-4);
	}
	.slot {
		display: grid;
		place-items: center;
		aspect-ratio: 1;
		border-radius: var(--radius-1);
		background: var(--tooltip-slot);
	}
	.slot img {
		width: 90%;
		height: 90%;
		object-fit: contain;
	}
	dl {
		display: grid;
		gap: var(--space-1);
		margin: 0;
	}
	dl div {
		display: flex;
		justify-content: space-between;
		gap: var(--space-4);
	}
	.primary {
		font-size: var(--type-2);
	}
	dt {
		color: var(--tooltip-muted);
	}
	dd {
		margin: 0;
		text-align: right;
		font-variant-numeric: tabular-nums;
	}
	.description {
		margin: 0;
		color: var(--tooltip-muted);
		line-height: var(--leading-snug);
	}
	.rest {
		padding-top: var(--space-3);
		border-top: var(--border-width) solid var(--tooltip-frame);
	}
	.unknown {
		color: var(--text-warning);
	}
</style>
