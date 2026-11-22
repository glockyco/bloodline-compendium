<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import LinkedText from '#lib/ui/LinkedText.svelte';
	let { data } = $props();
</script>

<SeoHead
	title="Mechanics | The Bloodline Compendium"
	description="How parts of The Bloodline work, such as skill points, proficiency points and the Blood Levels setting."
/>
<header class="page-heading list-title">
	<h1>Mechanics</h1>
	<p class="intro">How parts of the game work, each tested in the game.</p>
</header>
<div class="mechanic-table">
	<RelationTable
		items={data.index.mechanics}
		label="Mechanics"
		headings={['Mechanic', 'What It Covers']}
		numeric={false}
		collapse={false}
		>{#snippet row(mechanic)}<tr
				><td
					><EntityLink
						entity={{
							kind: 'mechanic',
							key: mechanic.key,
							name: mechanic.name,
							slug: mechanic.slug,
							icon: null
						}}
					/></td
				><td><LinkedText text={mechanic.intro} /></td></tr
			>{/snippet}</RelationTable
	>
</div>

<style>
	/* A mechanic's name stays on one line, and its description takes the rest of the row. */
	.mechanic-table :global(td:first-child) {
		white-space: nowrap;
	}
</style>
