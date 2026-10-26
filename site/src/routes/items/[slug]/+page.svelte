<script lang="ts">
	import { resolve } from '$app/paths';
	import SourceRow from '#lib/SourceRow.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Conditions from '#lib/Conditions.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import Section from '#lib/ui/Section.svelte';
	import ItemTooltip from '#lib/ItemTooltip.svelte';
	import IssueNote from '#lib/ui/IssueNote.svelte';
	import { sourceColumns, sourceHeadings, titleCase } from '#lib/labels.js';
	import {
		oddsLabel,
		recipeKinds,
		sortSources,
		sourceContext,
		summarize,
		useHeadings
	} from '#lib/acquisition.js';
	import RecipeLine from '#lib/RecipeLine.svelte';
	import { formatNumber } from '#lib/format.js';
	import { factValue } from '#lib/stats.js';
	import type { ItemStatus, Source, SourceKind } from '#lib/site-data.js';
	import SeoHead from '#lib/SeoHead.svelte';
	let { data } = $props();
	const item = $derived(data.item);
	const statusSentence: Record<ItemStatus, string> = {
		obtainable: '',
		noKnownSource: 'No way to get this item is known in this game version.',
		notInNormalPlay:
			'This item is not obtainable in normal play. Only test areas of the game hold it.'
	};
	const kind = $derived(data.kinds.find((entry) => entry.kind === item.kind));
	const itemsPath = $derived(kindPath(item.kind, data.kinds)!);
	const displayName = $derived(item.name + (item.qualifier ? ` (${item.qualifier})` : ''));
	const groups = $derived(
		(Object.keys(sourceHeadings) as SourceKind[])
			.map((kind) => {
				const listed = sortSources(item.sources.filter((source) => source.kind === kind));
				// Sources share a place heading where several stand in the same place. A source in several places
				// would sit under one of them while naming the others, so such groups list places in their rows.
				const places = listed.map(placeOf);
				const byPlace =
					listed.every((source) => source.places.length <= 1) &&
					new Set(places).size > 1 &&
					places.some((place, index) => places.indexOf(place) !== index);
				const sources = byPlace
					? [...listed].sort((a, b) => placeOf(a).localeCompare(placeOf(b), 'en'))
					: listed;
				const showAmount = sources.some(
					(source) => source.amount && (source.amount.min !== 1 || source.amount.max !== 1)
				);
				const showContext = sources.some((source) => source.price || source.odds);
				// A chance names its event once in the heading when every row shares it.
				const events = [
					...new Set(
						sources.flatMap((source) =>
							source.odds?.chance != null ? [oddsLabel(source.odds, source.kind)] : []
						)
					)
				];
				const sharedUnit = !sources.some((source) => source.price) && events.length === 1;
				const contextHeading = sources.some((source) => source.price)
					? 'Price'
					: sharedUnit
						? `Chance ${events[0]}`
						: 'Chance';
				return {
					kind,
					sources,
					byPlace,
					recipe: recipeKinds.has(kind),
					summary: sources.length ? summarize(kind, sources) : null,
					showAmount,
					showContext,
					sharedUnit,
					headings: [
						sourceColumns[kind],
						...(showAmount ? ['Quantity'] : []),
						...(showContext ? [contextHeading] : [])
					]
				};
			})
			.filter((group) => group.sources.length)
	);
	const uses = $derived(
		[...item.uses].sort(
			(a, b) => a.item.name.localeCompare(b.item.name, 'en') || a.name.localeCompare(b.name, 'en')
		)
	);
	const recipes = $derived(
		groups.filter((group) => group.recipe).flatMap((group) => group.sources)
	);
	const sourceGroups = $derived(groups.filter((group) => !group.recipe));
	// A long summary pushes the tooltip off a phone's first screen, so the tooltip leads there instead.
	const longSummary = $derived(groups.length > 3);
	const useFilters = $derived(
		uses.length > 10
			? {
					disciplines: [...new Set(uses.map((use) => use.discipline ?? useLabel(use.kind)))].sort(),
					categories: [
						...new Set(uses.map((use) => use.outputCategory).filter((value) => value != null))
					].sort()
				}
			: null
	);
	let useDiscipline = $state('');
	let useCategory = $state('');
	const shownUses = $derived(
		uses.filter(
			(use) =>
				(!useDiscipline || (use.discipline ?? useLabel(use.kind)) === useDiscipline) &&
				(!useCategory || use.outputCategory === useCategory)
		)
	);
	// The skill level a use needs shows in its own column, so the condition that states it does not repeat.
	const levelOf = (use: (typeof uses)[number]) =>
		use.conditions.find((condition) => condition.kind === 'proficiencyLevel')?.value ?? null;
	const otherConditions = (use: (typeof uses)[number]) =>
		use.conditions.filter((condition) => condition.kind !== 'proficiencyLevel');
	// The type adds nothing when it only repeats the category, as for most materials.
	const showType = $derived(
		item.type.toLocaleLowerCase('en') !== item.category.name.toLocaleLowerCase('en')
	);
	function placeOf(source: Source): string {
		return source.places[0]?.name ?? 'Location unknown';
	}
	function useLabel(kind: SourceKind): string {
		return useHeadings[kind] ?? sourceHeadings[kind];
	}
	const description = $derived(
		[
			`${displayName}: ${titleCase(item.type)} in The Bloodline.`,
			...item.facts
				.filter((fact) => factValue(fact) !== null)
				.slice(0, 2)
				.map((fact) => `${fact.label}: ${factValue(fact)}.`),
			item.sources.length
				? `${sourceHeadings[item.sources[0].kind]}: ${item.sources[0].name}${sourceContext(item.sources[0]) ? ` (${sourceContext(item.sources[0])})` : ''}.`
				: 'How to obtain it is unknown.'
		].join(' ')
	);
</script>

<SeoHead
	title={displayName + ' | The Bloodline Compendium'}
	{description}
	entity={{ kind: item.kind, key: item.key, name: item.name, slug: item.slug, icon: item.icon }}
	{kind}
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Items', path: itemsPath },
		{ name: displayName, path: entityPath(item, data.kinds)! }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li><a href={itemsPath}>Items</a></li>
		<li aria-current="page" data-game>{displayName}</li>
	</ol>
</nav>
<article data-pagefind-body data-pagefind-filter={`kind:${kind?.searchFilter ?? ''}`}>
	<div class="item-heading">
		<h1 data-pagefind-meta="title" data-game>{displayName}</h1>
		<p class="identity">
			<a
				href={kindPath(
					item.kind,
					data.kinds,
					`?category=${encodeURIComponent(item.category.slug)}`
				)!}>{titleCase(item.category.name)}</a
			>{#if showType}<span aria-hidden="true">·</span>
				<span data-game>{titleCase(item.type)}</span>{/if}
		</p>
	</div>
	<div class="detail-layout" class:long-summary={longSummary}>
		<div class="answer" data-pagefind-ignore="all">
			<Section id="how-to-get-it" title="How to Get It">
				{#snippet action()}{#if uses.length}<a href="#used-for"
							>Used for {formatNumber(uses.length)}</a
						>{/if}{/snippet}
				{#if !groups.length}
					<p class="surface">{statusSentence[item.status]}</p>
				{:else}
					<ul class="surface acquisition-routes">
						{#each groups as group (group.kind)}{@const summary = group.summary!}
							<li>
								<a href={group.recipe ? '#recipes' : '#source-' + group.kind}
									><span class="route-line"
										><span class="verb">{summary.verb}</span>{#if summary.names}<strong data-game
												>{summary.names}</strong
											>{/if}{#if summary.more}<span class="more"
												>and {formatNumber(summary.more)} more</span
											>{/if}{#if summary.places.length}<span class="where"
												>in <span data-game>{summary.places.join(', ')}</span></span
											>{/if}</span
									>{#if summary.context}<small>{summary.context}</small
										>{/if}{#each summary.conditions as condition (condition)}<small class="when"
											>{condition}</small
										>{/each}</a
								>
							</li>
						{/each}
					</ul>
				{/if}
			</Section>
		</div>
		<aside class="item-side" aria-label="Item details">
			<ItemTooltip
				item={{ ...item, name: displayName }}
				issueHref={item.notes.length
					? resolve('known-issues/') + '#' + item.notes[0].issue
					: undefined}
			/>
			{#if !item.icon}<p class="muted">No icon is known for this item.</p>{/if}
			{#each item.notes as note (note.issue)}
				<IssueNote title={note.title} href={resolve('known-issues/') + '#' + note.issue}
					>{note.effect}</IssueNote
				>
			{/each}
		</aside>
		<div class="item-relations" data-pagefind-ignore="all">
			{#if recipes.length}<div class="recipes">
					<Section
						id="recipes"
						title={recipes.length === 1 ? 'Recipe' : 'Recipes'}
						count={recipes.length > 1 ? recipes.length : undefined}
					>
						{#each recipes as source, index (source.relationId)}<RecipeLine
								{source}
								id={`recipe-${index + 1}`}
							/>{/each}
					</Section>
				</div>{/if}
			{#if uses.length}<Section id="used-for" title="Used For" count={uses.length}>
					{#if useFilters}<div class="use-filters">
							<label
								>Skill <select bind:value={useDiscipline}
									><option value="">All</option
									>{#each useFilters.disciplines as discipline (discipline)}<option
											value={discipline}>{discipline}</option
										>{/each}</select
								></label
							>
							{#if useFilters.categories.length > 1}<label
									>Makes <select bind:value={useCategory}
										><option value="">All</option
										>{#each useFilters.categories as category (category)}<option value={category}
												>{category}</option
											>{/each}</select
									></label
								>{/if}
						</div>{/if}
					<RelationTable
						items={shownUses}
						label="Used for"
						headings={['Makes', 'Skill', 'Needed']}
						numeric={[false, true]}
						>{#snippet row(use, index)}<tr
								id={`use-${index + 1}`}
								data-relation-id={use.relationId}
								data-relation-direction="outgoing"
								><td
									><EntityLink entity={use.item} icon /><Conditions
										conditions={otherConditions(use)}
									/></td
								><td
									>{use.discipline ?? useLabel(use.kind)}{#if levelOf(use) != null}<span
											class="muted">{` ${formatNumber(levelOf(use)!)}`}</span
										>{/if}</td
								><td class="numeric">{formatNumber(use.quantity)}</td></tr
							>{/snippet}</RelationTable
					>
				</Section>{/if}
			{#each sourceGroups as group (group.kind)}
				<Section
					class="source-group"
					id={'source-' + group.kind}
					title={sourceHeadings[group.kind]}
					count={group.sources.length}
				>
					<RelationTable
						items={group.sources}
						label={sourceHeadings[group.kind]}
						headings={group.headings}
						>{#snippet row(
							source,
							index
						)}{#if group.byPlace && (index === 0 || placeOf(group.sources[index - 1]) !== placeOf(source))}<tr
									class="place-heading"
									><th colspan={group.headings.length} scope="colgroup" data-game
										>{placeOf(source)}</th
									></tr
								>{/if}<SourceRow
								{source}
								showAmount={group.showAmount}
								showContext={group.showContext}
								showUnit={!group.sharedUnit}
								showPlaces={!group.byPlace}
								id={`source-${group.kind}-${index + 1}`}
							/>{/snippet}</RelationTable
					>
				</Section>
			{/each}
		</div>
	</div>
</article>
