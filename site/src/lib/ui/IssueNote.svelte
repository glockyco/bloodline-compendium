<script lang="ts">
	import type { Snippet } from 'svelte';
	let { title, href, children }: { title: string; href?: string; children: Snippet } = $props();
</script>

<aside class="note" aria-label="Known game issue">
	<p class="label">Known game issue</p>
	<p class="title">{title}</p>
	<div class="body">{@render children()}</div>
	{#if href}
		<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- callers pass hrefs they resolved. -->
		<a {href}>Read about this issue</a>
	{/if}
</aside>

<style>
	.note {
		display: grid;
		gap: var(--space-2);
		padding: var(--space-4);
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
	.title {
		font-weight: var(--weight-strong);
	}
	.body {
		color: var(--text-soft);
	}
</style>
