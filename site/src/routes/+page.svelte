<script lang="ts">
	import { resolve } from '$app/paths';
	let { data } = $props();
</script>

<svelte:head
	><title>The Bloodline Compendium</title><meta
		name="description"
		content="Find items, their facts and their known sources in The Bloodline."
	/></svelte:head
>
<section class="hero">
	<p class="eyebrow">An adventurer's reference</p>
	<h1>The Bloodline<br />Compendium</h1>
	<p class="intro">Find what an item does, where it comes from, and what is still unknown.</p>
	<a class="button" href={resolve('items/')}>Explore {data.count} items</a>
</section>
<section aria-labelledby="browse">
	<h2 id="browse">Browse by category</h2>
	<div class="category-grid">
		{#each data.categories as category (category.slug)}<a
				class="panel category-card"
				href={resolve(`items/?category=${category.slug}`)}
				><h3>{category.name}</h3>
				<span>{category.count} items</span></a
			>{/each}
	</div>
</section>
<aside class="notice">
	<h2>A reference, not a promise</h2>
	<p>
		Every named inventory item is included, even when no source is known. Sources come from readable
		game data, not a complete playthrough. Some places may be unreachable.
	</p>
	<a href={resolve('coverage/')}>Explore {data.gaps} known gaps</a>
</aside>
