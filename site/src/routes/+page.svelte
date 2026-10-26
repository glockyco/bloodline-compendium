<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	import { titleCase } from '#lib/labels.js';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
</script>

<SeoHead
	title="The Bloodline Compendium | Item Wiki and Reference"
	description="Find The Bloodline items, shops, recipes, drops and gathering sources in this unofficial item wiki, with clear explanations of what is still unknown."
	website
/>
<section class="hero">
	<h1>The Bloodline Compendium</h1>
	<p class="intro">
		The Bloodline is a single-player sandbox adventure RPG. Explore an open fantasy world, craft
		equipment and fight with weapons or magic. This unofficial item reference helps you find what
		you need and how to get it.
	</p>
	<a class="button" href={resolve('items/')}>Browse {formatNumber(data.count)} Items</a>
	<a class="steam-link" href="https://store.steampowered.com/app/1159290/The_Bloodline/"
		>The Bloodline on Steam</a
	>
	<ReleaseInfo release={data.release} />
</section>
<section aria-labelledby="browse">
	<h2 id="browse">Browse Items</h2>
	<div class="category-grid">
		{#each data.categories as category (category.slug)}<a
				class="panel category-card"
				href={resolve(`items/?category=${encodeURIComponent(category.slug)}`)}
				><h3>{titleCase(category.name)}</h3>
				<span>{formatNumber(category.count)} {category.count === 1 ? 'item' : 'items'}</span></a
			>{/each}
	</div>
</section>
<section class="notice" aria-labelledby="coverage-intro">
	<h2 id="coverage-intro">What is known and unknown</h2>
	<p>
		Every named inventory item has a page, including items without a known source. The item list
		hides those by default and lets you reveal them. Sources come from game data, not a complete
		playthrough, and some places may be unreachable.
	</p>
	<a href={resolve('coverage/')}>Explore {formatNumber(data.gaps)} known gaps</a>
</section>
