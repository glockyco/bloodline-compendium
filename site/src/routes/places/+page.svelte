<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
	const places = $derived(data.index.places.toSorted((a, b) => a.name.localeCompare(b.name, 'en')));
</script>

<SeoHead
	title="Places | The Bloodline Compendium"
	description="Every place in The Bloodline, with its merchants, creatures, containers and the items each gives."
/>
<header class="page-heading list-title">
	<h1>Places</h1>
	<p class="intro">
		Every place, with how many merchants, creatures and other things there give items.
	</p>
</header>
<RelationTable items={places} label="Places" headings={['Place', 'Things']} collapse={false}
	>{#snippet row(place)}<tr
			><td
				><EntityLink
					entity={{
						kind: 'place',
						key: place.key,
						name: place.name,
						slug: place.slug,
						icon: null,
						fragment: null
					}}
				/></td
			><td class="numeric">{formatNumber(place.things)}</td></tr
		>{/snippet}</RelationTable
>
