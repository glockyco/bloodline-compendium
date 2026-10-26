<script lang="ts">
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import '@pagefind/component-ui/css';
	let {
		variant = 'header',
		placeholder = 'Search items'
	}: { variant?: 'header' | 'hero'; placeholder?: string } = $props();
	onMount(() => {
		void import('@pagefind/component-ui');
	});
</script>

<div class="search {variant}" role="search">
	<pagefind-searchbox
		bundle-path="/pagefind/"
		{placeholder}
		aria-label="Search items"
		shortcut="/"
		max-results="8"
		hide-shortcut
	></pagefind-searchbox>
	<noscript
		>Search needs JavaScript. Browse the <a href={resolve('items/')}>item list</a> instead.</noscript
	>
</div>

<style>
	.search {
		--pf-text: var(--text);
		--pf-text-secondary: var(--text-soft);
		--pf-text-muted: var(--text-muted);
		--pf-background: var(--surface-raised);
		--pf-border: var(--border-control);
		--pf-border-focus: var(--accent);
		--pf-skeleton: var(--surface-hover);
		--pf-skeleton-shine: var(--surface-slot);
		--pf-hover: var(--surface-hover);
		--pf-mark: var(--text);
		--pf-scroll-shadow: transparent;
		--pf-shadow-sm: none;
		--pf-shadow-md: var(--shadow-raised);
		--pf-shadow-lg: var(--shadow-raised);
		--pf-outline-focus: var(--focus);
		--pf-outline-width: var(--focus-width);
		--pf-outline-offset: var(--focus-offset);
		--pf-font: var(--font-body);
		--pf-border-radius: var(--radius-1);
		--pf-dropdown-z-index: var(--layer-search);
		--pf-searchbox-max-width: 100%;
		--pf-dropdown-max-height: var(--size-filter-sheet);
		--pf-searchbox-dropdown-max-height: var(--size-filter-sheet);
		--pf-input-height: var(--size-control);
		--pf-input-font-size: var(--type-1);
		--pf-summary-font-size: var(--type-1);
		--pf-result-title-font-size: var(--type-1);
		--pf-result-excerpt-font-size: var(--type-1);
		width: 100%;
	}
	.hero {
		--pf-input-height: var(--size-control-large);
		--pf-border-radius: var(--radius-2);
		box-shadow: var(--shadow-raised);
		border-radius: var(--radius-2);
	}
</style>
