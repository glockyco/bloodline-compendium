<script lang="ts">
	import type { Snippet } from 'svelte';
	let { href, children }: { href: string; children: Snippet } = $props();
</script>

<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- callers pass hrefs they resolved. -->
<a class="button" {href}>{@render children()}</a>

<style>
	.button {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-height: var(--size-control);
		padding: 0 var(--space-5);
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
</style>
