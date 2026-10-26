<script lang="ts">
	import type { Snippet } from 'svelte';
	let { href, children }: { href?: string; children: Snippet } = $props();
</script>

<!-- Each direct child of the card body is one shared row of the surrounding CardGrid. -->
<li class="card" data-ui="card">
	{#if href}
		<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- callers pass hrefs they resolved. -->
		<a class="body" {href}>{@render children()}</a>
	{:else}<div class="body">{@render children()}</div>{/if}
</li>

<style>
	.card {
		display: grid;
		grid-row: span var(--card-rows);
		grid-template-rows: subgrid;
		margin-bottom: var(--gap-block);
	}
	.body {
		display: grid;
		grid-row: span var(--card-rows);
		grid-template-rows: subgrid;
		gap: var(--gap-block);
		min-width: 0;
		padding: var(--inset);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-2);
		background: var(--surface-panel);
		color: var(--text);
		text-decoration: none;
		transition:
			border-color var(--duration-hover),
			background-color var(--duration-hover);
	}
	a.body:hover {
		border-color: var(--border-control);
		background: var(--surface-raised);
		color: var(--text);
	}
</style>
