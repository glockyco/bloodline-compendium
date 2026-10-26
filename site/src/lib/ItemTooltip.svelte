<script lang="ts">
	import type { Fact } from './site-data.js';
	import { titleCase } from './labels.js';
	import { factValue } from './stats.js';

	interface TooltipItem {
		name: string;
		icon: string | null;
		description: string | null;
		facts: Fact[];
	}
	let { item, heading = 'h2' }: { item: TooltipItem; heading?: 'h2' | 'p' } = $props();

	// The game's tooltip shows its stat lines beside the icon under the vocabulary's tooltip labels.
	// Other facts follow below the description, in the order the catalog publishes them.
	const primary = $derived(item.facts.filter((fact) => fact.tooltipLabel));
	const rest = $derived(item.facts.filter((fact) => !fact.tooltipLabel));
</script>

<div class="tooltip" data-ui="tooltip">
	<svelte:element this={heading} class="name" data-game>{item.name}</svelte:element>
	<div class="body">
		<div class="slot">
			{#if item.icon}<img src="/icons/{item.icon}-160.webp" alt="" width="80" height="80" />{/if}
		</div>
		{#if primary.length}<dl class="primary">
				{#each primary as fact (fact.label)}<div>
						<dt>{fact.tooltipLabel}</dt>
						<dd class:unknown={factValue(fact) === null} data-game>
							{factValue(fact) ?? 'Unknown'}
						</dd>
					</div>{/each}
			</dl>{/if}
	</div>
	<p class="description" data-game>{item.description ?? 'Description unknown.'}</p>
	{#if rest.length}<dl class="rest">
			{#each rest as fact (fact.label)}<div>
					<dt>{titleCase(fact.label)}</dt>
					<dd class:unknown={factValue(fact) === null} data-game>
						{factValue(fact) ?? 'Unknown'}
					</dd>
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
		min-width: 0;
		margin: 0;
	}
	/* A long value moves below its label instead of leaving the frame. */
	dl div {
		display: flex;
		flex-wrap: wrap;
		justify-content: space-between;
		gap: 0 var(--space-4);
	}
	.primary {
		font-size: var(--type-2);
	}
	dt {
		color: var(--tooltip-muted);
	}
	dd {
		min-width: 0;
		margin: 0 0 0 auto;
		text-align: right;
		overflow-wrap: anywhere;
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
