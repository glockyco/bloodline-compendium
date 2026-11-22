<script lang="ts">
	import type { EntityReference } from '../site-data.js';
	import { formatNumber } from '../format.js';
	import EntityLink from '../ui/EntityLink.svelte';
	/**
	 * What a recipe uses up, each ingredient as its icon, quantity and name. A table row shows them at the inline icon
	 * size, so the row keeps the height of its neighbours.
	 */
	let {
		inputs,
		inline = false
	}: {
		/** Ingredients are items, so their references may leave out their kind. */
		inputs: {
			item: Pick<EntityReference, 'key' | 'name' | 'slug' | 'icon' | 'fragment'>;
			quantity: number;
			relationId?: string | null;
		}[];
		inline?: boolean;
	} = $props();
</script>

<ul class="ingredients" class:inline aria-label="Ingredients">
	{#each inputs as input, index (index)}<li
			data-relation-id={input.relationId}
			data-relation-direction={input.relationId ? 'incoming' : undefined}
		>
			<!-- The icon, quantity and name are one target, as a thumb aims at the whole ingredient. -->
			<EntityLink entity={{ kind: 'item', ...input.item }} icon
				><span class="quantity">{formatNumber(input.quantity)}</span>
				<span>{input.item.name}</span></EntityLink
			>
		</li>{/each}
</ul>

<style>
	.ingredients {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline) var(--gap-block);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.ingredients li {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
	}
	.ingredients:not(.inline) :global(a) {
		min-height: var(--size-control-compact);
	}
	.ingredients:not(.inline) :global(.entity-icon) {
		width: var(--size-icon-row);
		height: var(--size-icon-row);
		border-radius: var(--radius-1);
		background: var(--surface-slot);
	}
	.ingredients.inline {
		gap: var(--gap-line) var(--gap-block);
	}
	.quantity {
		font-weight: var(--weight-strong);
	}
</style>
