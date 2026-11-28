<script lang="ts">
	import { page } from '$app/state';
	import type { EntityReference } from '#lib/site-data.js';
	import { entityPath } from '#lib/entities.js';
	import { previewKinds, previewPath } from '#lib/entity/previews.js';
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

<!-- A name without a page carries its kind, which a link's address already shows, so the link coverage check can
count it. A name is its text, so only an entity shown with other text carries its name. -->
{#if path}<a
		href={path}
		class={[className, shownIcon && 'with-icon']}
		data-game
		data-name={children ? entity.name : undefined}
		data-preview={previewKinds.has(entity.kind) && (entity.slug ?? entity.host?.slug)
			? previewPath(entity.kind, (entity.slug ?? entity.host?.slug)!, entity.fragment ?? null)
			: undefined}>{@render content()}</a
	>{:else}<span
		class={[className, shownIcon && 'with-icon']}
		data-game
		data-kind={entity.kind}
		data-name={children ? entity.name : undefined}>{@render content()}</span
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
