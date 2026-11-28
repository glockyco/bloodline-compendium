<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
	// Creatures that can be met lead, by name, and those no area places follow.
	const creatures = $derived(
		data.index.creatures.toSorted(
			(a, b) => Number(a.unconfirmed) - Number(b.unconfirmed) || a.name.localeCompare(b.name, 'en')
		)
	);
</script>

<SeoHead
	title="Creatures | The Bloodline Compendium"
	description="Every creature in The Bloodline, with what it drops and where it lives."
/>
<header class="page-heading list-title">
	<h1>Creatures</h1>
	<p class="intro">
		Every creature, with how many different items it can drop and in how many areas it lives.
	</p>
</header>
<RelationTable
	items={creatures}
	label="Creatures"
	headings={['Creature', 'Drops', 'Areas']}
	collapse={false}
	>{#snippet row(creature)}<tr
			><td
				><EntityLink
					entity={{
						kind: 'creature',
						key: creature.key,
						name: creature.name,
						slug: creature.slug,
						icon: null,
						fragment: null
					}}
				/>{#if creature.unconfirmed}<p class="source-places">Not placed anywhere</p>{/if}</td
			><td class="numeric">{formatNumber(creature.drops)}</td><td class="numeric"
				>{formatNumber(creature.places)}</td
			></tr
		>{/snippet}</RelationTable
>
