<script lang="ts">
	import type { Snippet } from 'svelte';
	import { resolve } from '$app/paths';
	import SeoHead from '../SeoHead.svelte';
	import EntityArticle from '../ui/EntityArticle.svelte';
	import { entityPath, kindPath } from '../entities.js';
	import type { EntityKindDefinition, EntityReference } from '../site-data.js';
	/**
	 * The shell of every entity page: its metadata, the breadcrumbs from home through its kind's list, and the article
	 * that search reads. The kind registry names the list and its address, and the page supplies its body.
	 */
	let {
		entity,
		kinds,
		title,
		description,
		render,
		game = false,
		searchName = entity.name,
		icon = entity.icon ?? null,
		detail = null,
		children
	}: {
		entity: EntityReference;
		kinds: EntityKindDefinition[];
		/** The page title before the site's name, such as "Necromancy Discipline". */
		title: string;
		description: string;
		/** The hash of the entity's model render, which a shared link shows large. */
		render?: string;
		/** Whether the name is the game's own, which reads in the game's typeface. */
		game?: boolean;
		/** The name search shows, where it differs from the shown version's name. */
		searchName?: string;
		/** The icon search shows, where the page has one but its shared reference has none. */
		icon?: string | null;
		/** The context a search result shows after the kind, such as "Weapon · Sword". */
		detail?: string | null;
		children: Snippet;
	} = $props();
	const kind = $derived(kinds.find((entry) => entry.kind === entity.kind)!);
	const listPath = $derived(kindPath(entity.kind, kinds)!);
	const path = $derived(entityPath({ ...entity, fragment: null }, kinds)!);
</script>

<SeoHead
	title={title + ' | The Bloodline Compendium'}
	{description}
	{entity}
	{kind}
	{render}
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: kind.searchFilter, path: listPath },
		{ name: entity.name, path }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li><a href={listPath}>{kind.searchFilter}</a></li>
		{#if game}<li aria-current="page" data-game>{entity.name}</li>{:else}<li aria-current="page">
				{entity.name}
			</li>{/if}
	</ol>
</nav>
<EntityArticle {kind} name={searchName} {icon} {detail}>
	{@render children()}
</EntityArticle>
