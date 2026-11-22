<script lang="ts">
	import type { Snippet } from 'svelte';
	let {
		href,
		secondary = false,
		children
	}: {
		href: string;
		/** A second action beside the primary one, outlined instead of filled. */
		secondary?: boolean;
		children: Snippet;
	} = $props();
</script>

<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- callers pass hrefs they resolved. -->
<a class="button" class:secondary {href}>{@render children()}</a>

<style>
	.button {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-height: var(--size-control);
		padding: 0 var(--inset-loose);
		border: var(--border-width) solid var(--brand);
		border-radius: var(--radius-1);
		background: var(--brand);
		color: var(--text-on-brand);
		font-weight: var(--weight-strong);
		text-decoration: none;
	}
	.button:hover {
		background: var(--brand-hover);
		color: var(--text-on-brand);
	}
	.secondary {
		border-color: var(--border-control);
		background: var(--surface-panel);
		color: var(--text);
	}
	.secondary:hover {
		border-color: var(--brand);
		background: var(--surface-raised);
		color: var(--text);
	}
</style>
