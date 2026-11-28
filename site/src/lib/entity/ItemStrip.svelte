<script lang="ts">
	import EntityLink from '../ui/EntityLink.svelte';
	import IconSlot from '../ui/IconSlot.svelte';
	import type { EntityReference } from '../site-data.js';
	/**
	 * A thing's items as icons in their slots, each a link with the item's preview on hover, as an inventory reads. A
	 * long row shows its first items and opens the rest on demand, so a merchant's stock does not take over its card.
	 */
	let {
		items,
		shown = 12,
		label
	}: { items: EntityReference[]; shown?: number; label: string } = $props();
	let open = $state(false);
	const visible = $derived(open ? items : items.slice(0, shown));
</script>

<ul class="strip" aria-label={label}>
	{#each visible as item (item.key)}<li>
			<EntityLink entity={item} class="strip-link"
				><IconSlot icon={item.icon ?? null} size="list" /><span class="sr-only">{item.name}</span
				></EntityLink
			>
		</li>{/each}
	{#if items.length > shown}<li>
			<button type="button" class="more" aria-expanded={open} onclick={() => (open = !open)}
				>{open ? 'Fewer' : `+${items.length - shown}`}</button
			>
		</li>{/if}
</ul>

<style>
	.strip {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-line);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.strip :global(.strip-link) {
		display: block;
		border-radius: var(--radius-1);
	}
	.strip :global(.strip-link:hover [data-ui='slot']) {
		border-color: var(--accent);
	}
	.more {
		min-width: var(--size-icon-list);
		height: var(--size-icon-list);
		padding: 0 var(--inset-cell);
		border: var(--marker-width) solid var(--slot-edge);
		border-radius: var(--radius-1);
		background: var(--surface-slot);
		color: var(--accent);
		font-size: var(--type-meta);
	}
</style>
