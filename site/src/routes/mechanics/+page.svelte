<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid, { balancedColumns } from '#lib/ui/CardGrid.svelte';
	import { entityPath } from '#lib/entities.js';
	import { plainText } from '#lib/links.js';
	let { data } = $props();
</script>

<SeoHead
	title="Mechanics | The Bloodline Compendium"
	description="Discipline levels, skill points, proficiency points and the Blood Levels setting in The Bloodline."
/>
<header class="page-heading list-title">
	<h1>Mechanics</h1>
	<p class="intro">
		How disciplines level up, what skill and proficiency points buy, and what the Blood Levels
		setting changes.
	</p>
</header>
<CardGrid rows={2} columns={balancedColumns(data.index.mechanics.length)} label="Mechanics">
	{#each data.index.mechanics as mechanic (mechanic.key)}<Card
			href={entityPath(
				{
					kind: 'mechanic',
					key: mechanic.key,
					name: mechanic.name,
					slug: mechanic.slug,
					icon: null
				},
				data.kinds
			)!}
		>
			<h2 class="card-head">{mechanic.name}</h2>
			<p class="card-text">{plainText(mechanic.intro)}</p>
		</Card>{/each}
</CardGrid>

<style>
	.card-head {
		margin: 0;
		font-size: var(--type-sub);
		line-height: var(--leading-heading);
	}
	.card-text {
		margin: 0;
		color: var(--text-muted);
	}
</style>
