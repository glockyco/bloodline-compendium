<script lang="ts">
	import SourceRow from '#lib/items/SourceRow.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import Section from '#lib/ui/Section.svelte';
	import {
		placeUnknownReasons,
		sourceColumns,
		sourceHeadings,
		sourceIntros,
		sourceLevels
	} from '#lib/labels.js';
	import {
		headingCase,
		oddsLabel,
		placedKinds,
		recipeKinds,
		sortSources
	} from '#lib/items/acquisition.js';
	import type { Source, SourceKind } from '#lib/site-data.js';
	/**
	 * The confirmed ways to get an entity, one section per kind of source in a table. Recipes are not among them,
	 * because an item page shows them as recipe lines of their own.
	 */
	let {
		sources,
		idPrefix = ''
	}: {
		sources: Source[];
		idPrefix?: string;
	} = $props();
	const confirmedSources = $derived(sources.filter((source) => !source.unconfirmed));
	const groups = $derived(
		(Object.keys(sourceHeadings) as SourceKind[])
			.filter((kind) => !recipeKinds.has(kind))
			.map((kind) => {
				const listed = merged(
					sortSources(confirmedSources.filter((source) => source.kind === kind))
				);
				// Sources share a place heading where several stand in the same place. A source in several places
				// would sit under one of them while naming the others, so such groups list places in their rows.
				const places = listed.map(placeOf);
				// Picked Up rows are their places, so they never share a place heading.
				const byPlace =
					kind !== 'pickup' &&
					listed.every((source) => source.places.length <= 1) &&
					new Set(places).size > 1 &&
					places.some((place, index) => places.indexOf(place) !== index);
				// Places follow the order of their best source, so the likeliest place leads as the likeliest row would.
				const order = [...new Set(listed.map(placeOf))];
				const placed = byPlace
					? [...listed].sort((a, b) => order.indexOf(placeOf(a)) - order.indexOf(placeOf(b)))
					: listed;
				// Several ways without a place share one heading at the end, which says once why they have none. Where
				// places head the rows, one such way gets the heading too.
				const lost = placed.filter((source) => placeOf(source) === placeUnknown);
				const band =
					placeUnknownReasons[kind] && (lost.length > 1 || (byPlace && lost.length === 1))
						? lost.length
						: 0;
				const sorted = band
					? [...placed.filter((source) => !lost.includes(source)), ...lost]
					: placed;
				const showAmount = sorted.some(
					(source) => source.amount && (source.amount.min !== 1 || source.amount.max !== 1)
				);
				const showContext = sorted.some((source) => source.price || source.odds);
				const showSpawn =
					kind === 'containerLoot' && sorted.some((source) => source.spawn && source.spawn.min < 1);
				// A note comes from accepting its quest, which has no place of its own, so such rows name none.
				const showPlaces = kind !== 'bountyNote';
				// A chance names its event once in the heading when every row shares it.
				const events = [
					...new Set(
						sorted.flatMap((source) =>
							source.odds?.chance != null ? [oddsLabel(source.odds, source.kind)] : []
						)
					)
				];
				const sharedUnit = !sorted.some((source) => source.price) && events.length === 1;
				const contextHeading = sorted.some((source) => source.price)
					? 'Price'
					: sharedUnit
						? `Chance ${headingCase(events[0])}`
						: 'Chance';
				return {
					kind,
					sources: sorted,
					byPlace,
					bandFrom: band ? sorted.length - band : null,
					showPlaces,
					showAmount,
					showSpawn,
					showContext,
					sharedUnit,
					headings: [
						sourceColumns[kind],
						...(sourceLevels[kind] ? [sourceLevels[kind].heading] : []),
						...(showAmount ? ['Quantity'] : []),
						...(showSpawn ? ['Spawns'] : []),
						...(showContext ? [contextHeading] : [])
					]
				};
			})
			.filter((group) => group.sources.length)
	);
	/**
	 * Rows that read the same apart from their place show as one row with every place, carrying each relation. A
	 * resource with a page gives an item from spots that are always that resource and from spots drawn from a table,
	 * by relations that differ only in notes the resource page shows. A conversation that generated characters hold
	 * in several towns, such as a Civilian's, is one way to get the item.
	 */
	function merged(listed: Source[]): (Source & { alsoRelationIds?: string[] })[] {
		const rows: (Source & { alsoRelationIds?: string[] })[] = [];
		const said = (source: Source) =>
			JSON.stringify([source.conditions, source.notes, source.price, source.quotes, source.amount]);
		for (const source of listed) {
			const same =
				source.kind === 'gathering' && source.entity?.slug != null
					? rows.find(
							(row) =>
								row.entity?.key === source.entity?.key &&
								row.odds?.chance === source.odds?.chance &&
								JSON.stringify(row.conditions) === JSON.stringify(source.conditions)
						)
					: source.kind === 'dialogue' && source.entity?.slug == null
						? rows.find(
								(row) =>
									row.kind === 'dialogue' && row.name === source.name && said(row) === said(source)
							)
						: undefined;
			if (!same) {
				rows.push(source);
				continue;
			}
			const index = rows.indexOf(same);
			rows[index] = {
				...same,
				places: [
					...same.places,
					...source.places.filter((place) => !same.places.some((other) => other.key === place.key))
				],
				alsoRelationIds: [...(same.alsoRelationIds ?? []), source.relationId]
			};
		}
		return rows;
	}
	const placeUnknown = 'Location unknown';
	function placeOf(source: Source): string | null {
		if (source.places[0]) return source.places[0].name;
		if (source.settlement) return 'Your settlement';
		// Random loot and crops stand nowhere in particular, so only ways that stand somewhere miss a place.
		return placedKinds.has(source.kind) ? placeUnknown : null;
	}
</script>

{#each groups as group (group.kind)}
	<Section
		class="source-group"
		id={idPrefix + 'source-' + group.kind}
		title={sourceHeadings[group.kind]}
		count={group.sources.length}
		line={sourceIntros[group.kind]}
	>
		<RelationTable
			items={group.sources}
			label={sourceHeadings[group.kind]}
			headings={group.headings}
			>{#snippet row(source, index)}{@const lost =
					group.bandFrom != null && index >= group.bandFrom}{#if index === group.bandFrom}<tr
						class="group-heading"
						><th colspan={group.headings.length} scope="colgroup"
							>{placeUnknown}<span class="band-reason">{placeUnknownReasons[group.kind]}</span></th
						></tr
					>{:else if !lost && group.byPlace && (index === 0 || placeOf(group.sources[index - 1]) !== placeOf(source))}<tr
						class="group-heading"
						><th colspan={group.headings.length} scope="colgroup" data-game>{placeOf(source)}</th
						></tr
					>{/if}<SourceRow
					{source}
					showAmount={group.showAmount}
					showSpawn={group.showSpawn}
					showContext={group.showContext}
					showUnit={!group.sharedUnit}
					showPlaces={!lost && !group.byPlace && group.showPlaces}
					placeFirst={group.kind === 'pickup'}
					levelCondition={sourceLevels[group.kind]?.condition}
					id={`${idPrefix}source-${group.kind}-${index + 1}`}
					alsoRelationIds={source.alsoRelationIds}
				/>{/snippet}</RelationTable
		>
	</Section>
{/each}

<style>
	.band-reason {
		display: block;
		font-weight: var(--weight-regular);
	}
</style>
