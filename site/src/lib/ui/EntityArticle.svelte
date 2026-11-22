<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { EntityKindDefinition } from '#lib/site-data.js';
	import { kindLabel } from '#lib/entities.js';
	/**
	 * The article of an entity's page. It marks the page for search with its kind's filter from the kind registry,
	 * so every kind with pages is searchable by kind, and the publication gate rejects an entity page without it. Its
	 * search title names the kind, because the search box shows only a result's title and excerpt, and an item and a
	 * discipline can share a name.
	 */
	let { kind, name, children }: { kind: EntityKindDefinition; name: string; children: Snippet } =
		$props();
</script>

<article
	data-pagefind-body
	data-pagefind-filter={`kind:${kind.searchFilter}`}
	data-pagefind-meta={`title:${name} · ${kindLabel(kind.kind)}`}
>
	{@render children()}
</article>
