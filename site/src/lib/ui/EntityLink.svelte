<script lang="ts">
	import { page } from '$app/state';
	import type { EntityReference } from '#lib/site-data.js';
	import { entityPath } from '#lib/entities.js';
	import { previewKinds, previewPath } from '#lib/previews.js';
	import type { Snippet } from 'svelte';
	let {
		entity,
		class: className,
		icon = false,
		children
	}: {
		entity: EntityReference;
		class?: string;
		/** Shows the entity's icon before its name, inside the same link, because players recognize items by it. */
		icon?: boolean;
		children?: Snippet;
	} = $props();
	const path = $derived(entityPath(entity, page.data.kinds));
	const shownIcon = $derived(icon && entity.icon ? entity.icon : null);
</script>

{#snippet content()}{#if shownIcon}<img
			class="entity-icon"
			src="/icons/{shownIcon}-48.webp"
			alt=""
			width="24"
			height="24"
			loading="lazy"
		/>{/if}{#if children}{@render children()}{:else}<span>{entity.name}</span>{/if}{/snippet}

{#if path}<a
		href={path}
		class={[className, shownIcon && 'with-icon']}
		data-game
		data-preview={previewKinds.has(entity.kind) && entity.slug
			? previewPath(entity.kind, entity.slug)
			: undefined}>{@render content()}</a
	>{:else}<span class={[className, shownIcon && 'with-icon']} data-game>{@render content()}</span
	>{/if}

<style>
	/* The icon and name are one target, and only the name wraps. */
	.with-icon {
		display: inline-flex;
		align-items: center;
		gap: var(--gap-inline);
		vertical-align: middle;
		text-decoration: none;
	}
	a.with-icon:hover {
		text-decoration: underline;
	}
	.entity-icon {
		flex: none;
		width: var(--size-icon-inline);
		height: var(--size-icon-inline);
		object-fit: contain;
	}
</style>
