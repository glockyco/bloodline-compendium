<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { EntityKindDefinition } from '#lib/site-data.js';
	/**
	 * The article of an entity's page. It marks the page for search with its kind's filter from the kind registry,
	 * so every kind with pages is searchable by kind, and the publication gate rejects an entity page without it.
	 */
	let { kind, children }: { kind: EntityKindDefinition; children: Snippet } = $props();
</script>

<article data-pagefind-body data-pagefind-filter={`kind:${kind.searchFilter}`}>
	{@render children()}
</article>
