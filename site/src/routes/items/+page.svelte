<script lang="ts">
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { selectItems } from '#lib/items.js';
	let { data } = $props();
	let category = $state('');
	onMount(() => {
		category = new URL(window.location.href).searchParams.get('category') ?? '';
	});
	let sort = $state('name-asc');
	const items = $derived(selectItems(data.index.items, category, sort));
</script>

<svelte:head
	><title>Items | The Bloodline Compendium</title><meta
		name="description"
		content="Browse items by category, name and value."
	/></svelte:head
>
<p class="eyebrow">The item reference</p>
<h1>Items</h1>
<p class="intro">Equipment, ingredients and curios. Unknown values stay unknown.</p>
<div class="toolbar">
	<label
		>Category<select bind:value={category}
			><option value="">All categories</option
			>{#each data.index.categories as entry (entry.slug)}<option value={entry.slug}
					>{entry.name}</option
				>{/each}</select
		></label
	><label
		>Sort by<select bind:value={sort}
			><option value="name-asc">Name: A to Z</option><option value="name-desc">Name: Z to A</option
			><option value="value-asc">Value: low to high</option><option value="value-desc"
				>Value: high to low</option
			></select
		></label
	>
	<p role="status">{items.length} {items.length === 1 ? 'item' : 'items'}</p>
</div>
<ul class="item-list">
	{#each items as item (item.slug)}<li>
			<a class="item-row" href={resolve(`items/${item.slug}/`)}
				>{#if item.icon}<img
						src={'/icons/' + item.icon + '-48.webp'}
						alt=""
						width="48"
						height="48"
						loading="lazy"
					/>{:else}<span class="missing-icon">No icon</span>{/if}<span class="item-name"
					>{item.name}<small
						>{data.index.categories.find((c) => c.slug === item.category)?.name ??
							item.category}</small
					></span
				><span class="item-value">{item.value ?? 'Unknown'}<small>Value</small></span></a
			>
		</li>{/each}
</ul>
{#if !items.length}<p>No items in this category.</p>{/if}
