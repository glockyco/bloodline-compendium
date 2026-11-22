<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { EntityKindDefinition } from '#lib/site-data.js';
	import { kindLabel } from '#lib/entities.js';
	/**
	 * The article of an entity's page. It marks the page for search with its kind's filter from the kind registry,
	 * so every kind with pages is searchable by kind, and the publication gate rejects an entity page without it. It
	 * gives search what a result shows: the name, the kind, a line of context such as an item's category, and the icon.
	 */
	let {
		kind,
		name,
		detail = null,
		icon = null,
		children
	}: {
		kind: EntityKindDefinition;
		name: string;
		/** The context a search result shows after the kind, such as "Weapon · Sword". */
		detail?: string | null;
		icon?: string | null;
		children: Snippet;
	} = $props();
</script>

<article data-pagefind-body data-pagefind-filter={`kind:${kind.searchFilter}`}>
	<span hidden data-pagefind-meta={`title:${name}`}></span>
	<span hidden data-pagefind-meta={`kind:${kindLabel(kind.kind)}`}></span>
	{#if detail}<span hidden data-pagefind-meta={`detail:${detail}`}></span>{/if}
	{#if icon}<span hidden data-pagefind-meta={`icon:${icon}`}></span>{/if}
	{@render children()}
</article>
