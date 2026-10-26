<script lang="ts">
	import { page } from '$app/state';
	import type { EntityReference } from '#lib/site-data.js';
	import { entityPath } from '#lib/entities.js';
	import type { Snippet } from 'svelte';
	let {
		entity,
		class: className,
		children
	}: { entity: EntityReference; class?: string; children?: Snippet } = $props();
	const path = $derived(entityPath(entity, page.data.kinds));
</script>

{#if path}<a
		href={path}
		class={className}
		data-game
		data-preview={entity.kind === 'item' && entity.slug ? entity.slug : undefined}
		>{#if children}{@render children()}{:else}{entity.name}{/if}</a
	>{:else}<span class={className} data-game
		>{#if children}{@render children()}{:else}{entity.name}{/if}</span
	>{/if}
