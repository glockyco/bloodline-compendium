<script lang="ts">
	import type { Snippet } from 'svelte';
	let { href, children }: { href?: string; children: Snippet } = $props();
</script>

<aside class="note" aria-label="Known game issue">
	<p class="label">Known game issue</p>
	<div class="body">{@render children()}</div>
	{#if href}
		<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- callers pass hrefs they resolved. -->
		<a {href}>Read about this issue</a>
	{/if}
</aside>

<style>
	.note {
		display: grid;
		gap: var(--gap-inline);
		padding: var(--inset);
		border: var(--border-width) solid var(--border-control);
		border-left: var(--focus-width) solid var(--text-warning);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
	}
	p {
		margin: 0;
	}
	.label {
		color: var(--text-warning);
		font-weight: var(--weight-strong);
	}
	.body {
		color: var(--text-soft);
	}
</style>
