<script lang="ts">
	import type { Harvest } from '../site-data.js';
	import RelationTable from '../RelationTable.svelte';
	import Section from '../ui/Section.svelte';
	import EntityLink from '../ui/EntityLink.svelte';
	import LinkedText from '../ui/LinkedText.svelte';
	/**
	 * The items creatures give when they die with the debuff a skill or item applies, on the page of what applies it.
	 * Each row carries its relation, as the item's own Harvested from Kills row does.
	 */
	let { harvests, idPrefix = '' }: { harvests: Harvest[]; idPrefix?: string } = $props();
</script>

{#if harvests.length}<Section
		id={idPrefix + 'harvests'}
		title="Harvested from Kills"
		count={harvests.length}
		line="A creature that dies while it has the debuff a row names gives you 1 of that row's item."
	>
		<RelationTable items={harvests} label="Harvested from kills" headings={['Item']} stackPairs
			>{#snippet row(harvest, index)}<tr
					id={`${idPrefix}harvest-${index + 1}`}
					data-relation-id={harvest.relationId}
					data-relation-direction="outgoing"
					><td
						><EntityLink entity={harvest.item} icon />
						<p class="source-places"><LinkedText text={harvest.text} /></p></td
					></tr
				>{/snippet}</RelationTable
		>
	</Section>{/if}
