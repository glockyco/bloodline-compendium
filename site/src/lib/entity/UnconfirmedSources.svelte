<script lang="ts">
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import type { Source } from '#lib/site-data.js';
	/** The ways to get an entity that only the game files name, which no place or person in the world uses. */
	let {
		sources,
		name,
		idPrefix = ''
	}: {
		sources: Source[];
		/** The entity's name, as the section's sentence names it. */
		name: string;
		idPrefix?: string;
	} = $props();
	// Unconfirmed sources of one name and place share a row, which keeps each source's relation. One sentence says
	// why none is confirmed.
	const rows = $derived.by(() => {
		const key = (source: Source) =>
			source.name + '|' + source.places.map((place) => place.name).join(',');
		const listed = sources.filter((candidate) => candidate.unconfirmed);
		return listed
			.filter((source, index) => listed.findIndex((other) => key(other) === key(source)) === index)
			.map((source) => ({
				source,
				relationIds: listed
					.filter((other) => key(other) === key(source))
					.map((other) => other.relationId)
			}));
	});
	const sentence = $derived(
		`The game files list these ways to get ${name}, but no ${
			rows.some(({ source }) => source.kind === 'dialogue') ? 'place or person' : 'place'
		} in the world uses them.`
	);
</script>

{#if rows.length}<Section
		id={idPrefix + 'only-in-game-files'}
		title="Only in the Game Files"
		count={rows.length}
		line={sentence}
	>
		<RelationTable items={rows} label="Only in the Game Files" headings={['Source']} numeric={false}
			>{#snippet row({ source, relationIds }, index)}<tr
					id={`${idPrefix}unconfirmed-source-${index + 1}`}
					data-relation-id={relationIds.join(' ')}
					data-relation-direction="incoming"
					><td
						><span class="relation-name"
							><EntityLink entity={source.entity}>{source.name}</EntityLink></span
						>{#if source.places.length}<p class="source-places">
								{#each source.places as place, index (index)}{index ? ', ' : ''}<EntityLink
										entity={place}
									/>{/each}
							</p>{/if}</td
					></tr
				>{/snippet}</RelationTable
		>
	</Section>{/if}
