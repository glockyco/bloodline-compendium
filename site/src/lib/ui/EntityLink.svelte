<script lang="ts">
	import { resolve } from '$app/paths';
	/** A reference to anything the compendium names. Kinds without pages render their name without a link. */
	interface EntityReference {
		name: string;
		slug: string | null;
	}
	let { entity, kind = 'item' }: { entity: EntityReference; kind?: 'item' } = $props();
	const routes = { item: 'items' } as const;
</script>

{#if entity.slug}<a href={resolve(`${routes[kind]}/${entity.slug}/`)} data-game>{entity.name}</a
	>{:else}<span data-game>{entity.name}</span>{/if}
