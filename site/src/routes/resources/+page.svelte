<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import Section from '#lib/ui/Section.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid from '#lib/ui/CardGrid.svelte';
	import Tabs from '#lib/ui/Tabs.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import { entityPath } from '#lib/entities.js';
	import { formatNumber, plural } from '#lib/format.js';
	let { data } = $props();
	// Categories in the order a player levels them, the gathering professions first.
	const order = ['Woodcutting', 'Mining', 'Foraging', 'Farming', 'Gathering'];
	const groups = $derived(
		order
			.map((category) => ({
				category,
				// Each group runs from the lowest level it needs to the highest, as a player unlocks them.
				resources: data.index.resources
					.filter((resource) => resource.category === category)
					.toSorted((a, b) => (a.level ?? 0) - (b.level ?? 0) || a.name.localeCompare(b.name, 'en'))
			}))
			.filter((group) => group.resources.length)
	);
	const groupTabs = $derived(
		groups.map((group) => ({
			id: group.category.toLowerCase(),
			label: titles[group.category],
			count: group.resources.length
		}))
	);
	const titles: Record<string, string> = {
		Woodcutting: 'Trees',
		Mining: 'Deposits',
		Foraging: 'Mushrooms and Herbs',
		Farming: 'Seeds You Plant',
		Gathering: 'Bushes and Other Spots'
	};
</script>

<SeoHead
	title="Resources | The Bloodline Compendium"
	description="Every tree, deposit, mushroom, herb and crop in The Bloodline, with what it gives, the level it needs and where it grows."
/>
<header class="page-heading list-title">
	<h1>Resources</h1>
	<p class="intro">
		Trees, deposits, mushrooms, herbs and crops, with what each gives, the level it needs and where
		it grows.
	</p>
</header>
{#snippet grid(group: (typeof groups)[number], tabbed: boolean)}
	<CardGrid rows={2} columns={3} label={titles[group.category]}>
		{#each group.resources as resource (resource.key)}<Card
				href={entityPath(
					{
						kind: 'resource',
						key: resource.key,
						name: resource.name,
						slug: resource.slug,
						icon: null
					},
					data.kinds
				)!}
			>
				<svelte:element this={tabbed ? 'h2' : 'h3'} class="card-head"
					><IconSlot icon={resource.icon ?? null} />{resource.name}</svelte:element
				>
				<p class="card-line">
					{resource.level
						? `Needs ${resource.profession} level ${formatNumber(resource.level)}, gives`
						: 'Gives'}
					{plural(resource.loot, 'item', 'items')}
				</p>
			</Card>{/each}
	</CardGrid>
{/snippet}

<Tabs tabs={groupTabs} label="Resource groups" initial={groupTabs[0].id}>
	{#snippet panel(id, tabbed)}{@const group = groups.find(
			(entry) => entry.category.toLowerCase() === id
		)!}{#if tabbed}{@render grid(group, true)}{:else}<Section
				id="{id}-heading"
				title={titles[group.category]}
				count={group.resources.length}>{@render grid(group, false)}</Section
			>{/if}{/snippet}
</Tabs>

<style>
	.card-head {
		display: flex;
		align-items: center;
		gap: var(--gap-block);
		margin: 0;
		font-size: var(--type-sub);
		line-height: var(--leading-heading);
	}
	.card-line {
		margin: 0;
		color: var(--text-muted);
	}
</style>
