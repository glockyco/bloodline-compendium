<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
	const professions = $derived(data.index.professions);
	const levels = (min: number | null | undefined, max: number | null | undefined) =>
		min == null || max == null
			? 'None'
			: min === max
				? formatNumber(min)
				: `${formatNumber(min)} to ${formatNumber(max)}`;
</script>

<SeoHead
	title="Professions | The Bloodline Compendium"
	description="Every profession in The Bloodline, where you work at it, the levels its recipes need and what it makes."
/>
<header class="page-heading list-title">
	<h1>Professions</h1>
	<p class="intro">
		What each profession makes, where you work at it and the levels its recipes need.
	</p>
</header>
<RelationTable
	items={professions}
	label="Professions"
	headings={['Profession', 'Made At', 'Levels', 'Items']}
	numeric={[false, true, true]}
	collapse={false}
	>{#snippet row(profession)}<tr
			><td
				><EntityLink
					entity={{
						kind: 'profession',
						key: profession.key,
						name: profession.name,
						slug: profession.slug,
						icon: profession.icon ?? null
					}}
					icon
				/></td
			><td class:absent={!profession.stations.length}
				>{#each profession.stations as station, index (station.key)}{index ? ', ' : ''}<EntityLink
						entity={station}
					/>{:else}None{/each}</td
			><td class="numeric" class:absent={profession.minLevel == null}
				>{levels(profession.minLevel, profession.maxLevel)}</td
			><td class="numeric">{formatNumber(profession.count)}</td></tr
		>{/snippet}</RelationTable
>
