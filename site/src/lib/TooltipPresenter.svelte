<script lang="ts">
	import ItemTooltip from './ItemTooltip.svelte';
	import type { Preview } from './previews.js';
	/** Shows a preview in its kind's tooltip, as Afallon's presenter picks a tooltip per kind. */
	let { preview }: { preview: Preview } = $props();
</script>

{#if preview.kind === 'item'}<ItemTooltip item={preview} heading="p" />
	{#each preview.issues as issue (issue)}<p class="issue">Known game issue: {issue}</p>{/each}
	{#if preview.route}<p class="route">{preview.route}</p>{/if}{/if}

<style>
	.issue {
		margin: var(--gap-block) var(--inset) 0;
		color: var(--text-warning);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
	}
	.route {
		margin: var(--gap-block) var(--inset) 0;
		padding-top: var(--inset);
		border-top: var(--border-width) solid var(--border);
		color: var(--text-soft);
	}
</style>
