<script lang="ts">
	import RelationTable from '#lib/RelationTable.svelte';
	import Conditions from '#lib/Conditions.svelte';
	import Ingredients from '#lib/items/Ingredients.svelte';
	import { sourceHeadings } from '#lib/labels.js';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import type { Source } from '#lib/site-data.js';
	import { scopeReasons } from './reason-scope.js';
	/** The ways to get an entity that only the game files name, each with the reason it is not a way in the world. */
	let { sources, idPrefix = '' }: { sources: Source[]; idPrefix?: string } = $props();
	// How much a spot gives before it regrows is no reason, unless it gives nothing.
	const reasonsOf = (source: Source) => [
		...source.notes.filter((note) => !/^Gives up to [1-9]/.test(note)),
		...source.conditions
			.filter((condition) => condition.kind === 'unknown')
			.map((condition) => condition.label + '.')
	];
	const requirements = (source: Source) =>
		source.conditions.filter((condition) => condition.kind !== 'unknown');
	// Sources that read the same share a row, which keeps each source's relation.
	const scoped = $derived.by(() => {
		const rows: { key: string; source: Source; relationIds: string[] }[] = [];
		for (const source of sources) {
			if (!source.unconfirmed) continue;
			const key = JSON.stringify([
				source.kind,
				source.name,
				source.places.map((place) => place.name),
				requirements(source),
				reasonsOf(source)
			]);
			const row = rows.find((other) => other.key === key);
			if (row) row.relationIds.push(source.relationId);
			else rows.push({ key, source, relationIds: [source.relationId] });
		}
		return scopeReasons(rows, ({ source }) => reasonsOf(source));
	});
</script>

{#if scoped.rows.length}<Section
		id={idPrefix + 'only-in-game-files'}
		title="Only in the Game Files"
		count={scoped.rows.length}
		line={scoped.lines.join(' ')}
	>
		<RelationTable
			items={scoped.rows}
			label="Only in the Game Files"
			headings={['Interaction']}
			numeric={false}
			>{#snippet row({ row: { source, relationIds }, reasons, band }, index)}{#if band}<tr
						class="group-heading"
						><th scope="colgroup"
							>{#each band as reason (reason)}<span class="band-reason">{reason}</span>{/each}</th
						></tr
					>{/if}<tr
					id={`${idPrefix}unconfirmed-source-${index + 1}`}
					data-relation-id={relationIds.join(' ')}
					data-relation-direction="incoming"
					><td
						><p class="method">{sourceHeadings[source.kind]}</p>
						<span class="relation-name"
							><EntityLink entity={source.entity}>{source.name}</EntityLink></span
						>
						<Conditions conditions={requirements(source)} />
						<!-- Nobody has tried a recipe that its station does not list, so its ingredients let a reader try. -->
						{#if source.inputs.length}<Ingredients inputs={source.inputs} inline />{/if}
						{#if source.places.length}<p class="source-places">
								{#each source.places as place, index (index)}{index ? ', ' : ''}<EntityLink
										entity={place}
									/>{/each}
							</p>{/if}{#each reasons as reason (reason)}<p class="source-places">
								{reason}
							</p>{/each}</td
					></tr
				>{/snippet}</RelationTable
		>
	</Section>{/if}

<style>
	.method {
		margin: 0;
		color: var(--text-muted);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
	}
	.band-reason {
		display: block;
	}
</style>
