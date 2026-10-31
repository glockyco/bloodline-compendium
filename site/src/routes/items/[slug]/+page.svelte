<script lang="ts">
	import { onMount, tick } from 'svelte';
	import { resolve } from '$app/paths';
	import SourceRow from '#lib/SourceRow.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
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
		useHeadings,
		pricePhrase
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
		noKnownSource:
			'No way to get this item is known in this game version. A quest or event may still give it.',
		notInNormalPlay:
			'This item is not obtainable in normal play. Only test areas of the game hold it.'
	};
	// An item page exists only for a kind the registry gives pages.
	const kind = $derived(data.kinds.find((entry) => entry.kind === item.kind)!);
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
				// Places follow the order of their best source, so the likeliest place leads as the likeliest row would.
				const order = [...new Set(listed.map(placeOf))];
				const sources = byPlace
					? [...listed].sort((a, b) => order.indexOf(placeOf(a)) - order.indexOf(placeOf(b)))
					: listed;
				const showAmount = sources.some(
					(source) => source.amount && (source.amount.min !== 1 || source.amount.max !== 1)
				);
				const showContext = sources.some((source) => source.price || source.odds);
				// A note comes from accepting its quest, which has no place of its own, so such rows name none.
				const showPlaces = kind !== 'bountyNote';
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
					showPlaces,
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
	const sortedUses = $derived(
		[...item.uses].sort(
			(a, b) => a.item.name.localeCompare(b.item.name, 'en') || a.name.localeCompare(b.name, 'en')
		)
	);
	// A merchant that takes the item as payment sells something for it, which is not a recipe, so payments get their
	// own table with the merchant and the price.
	const uses = $derived(sortedUses.filter((use) => use.kind !== 'currencyShop'));
	const payments = $derived(sortedUses.filter((use) => use.kind === 'currencyShop'));
	const recipes = $derived(
		groups.filter((group) => group.recipe).flatMap((group) => group.sources)
	);
	const sourceGroups = $derived(groups.filter((group) => !group.recipe));
	// A phone shows the first ways to get the item until the reader asks for the rest, so the answer stays short
	// enough to lead the page with the game card right below it.
	const firstWays = 3;
	let mounted = $state(false);
	let allWays = $state(false);
	let routes = $state<HTMLElement>();
	onMount(() => {
		mounted = true;
	});
	async function showAllWays() {
		allWays = true;
		await tick();
		routes?.querySelectorAll<HTMLElement>('li > a')[firstWays]?.focus();
	}
	// A page with nothing beyond its answer keeps its issue notes beside the answer, so neither column is a lone
	// strip.
	const sparse = $derived(!recipes.length && !sortedUses.length && !sourceGroups.length);
	// The skill level a use needs shows in its own column, so the condition that states it does not repeat.
	const levelOf = (use: (typeof uses)[number]) =>
		use.conditions.find((condition) => condition.kind === 'proficiencyLevel')?.value ?? null;
	const otherConditions = (use: (typeof uses)[number]) =>
		use.conditions.filter((condition) => condition.kind !== 'proficiencyLevel');
	// A long list of uses filters by product name, skill, product type and the level a player has.
	const useFilters = $derived(
		uses.length > 10
			? {
					disciplines: [...new Set(uses.map((use) => use.discipline ?? useLabel(use.kind)))].sort(),
					types: [
						...new Set(uses.map((use) => use.outputType).filter((value) => value != null))
					].sort(),
					leveled: uses.some((use) => levelOf(use) != null)
				}
			: null
	);
	let useName = $state('');
	let useDiscipline = $state('');
	let useType = $state('');
	let useLevel = $state<number | null>(null);
	const shownUses = $derived.by(() => {
		const needle = useName.trim().toLocaleLowerCase('en');
		return uses.filter(
			(use) =>
				(!needle || use.item.name.toLocaleLowerCase('en').includes(needle)) &&
				(!useDiscipline || (use.discipline ?? useLabel(use.kind)) === useDiscipline) &&
				(!useType || use.outputType === useType) &&
				(useLevel == null || (levelOf(use) ?? 0) <= useLevel)
		);
	});
	// The type adds nothing when it only repeats the category, as for most materials.
	const showType = $derived(
		item.type.toLocaleLowerCase('en') !== item.category.name.toLocaleLowerCase('en')
	);
	function placeOf(source: Source): string {
		return source.places[0]?.name ?? (source.settlement ? 'Your settlement' : 'Location unknown');
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
{#snippet notes()}{#each item.notes as note (note.issue)}<IssueNote
			title={note.title}
			href={resolve('known-issues/') + '#' + note.issue}>{note.effect}</IssueNote
		>{/each}{/snippet}
<EntityArticle {kind}>
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
	<div class="detail-layout">
		<div class="answer" data-pagefind-ignore="all">
			<Section id="how-to-get-it" title="How to Get It">
				{#snippet action()}{#if sortedUses.length}<a href={uses.length ? '#used-for' : '#buys'}
							>Used for {formatNumber(sortedUses.length)}</a
						>{/if}{/snippet}
				{#if !groups.length}
					<p class="surface">
						{statusSentence[item.status]}
						{#if item.status === 'noKnownSource'}<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- the resolved page with its section. -->
							<a href={resolve('about/') + '#limits'}>What item pages cannot tell you</a>{/if}
					</p>
				{:else}
					<ul
						class="surface acquisition-routes"
						class:capped={mounted && !allWays && groups.length > firstWays + 1}
						bind:this={routes}
					>
						{#each groups as group, index (group.kind)}{@const summary = group.summary!}
							<li class:later={index >= firstWays}>
								<a href={group.recipe ? '#recipes' : '#source-' + group.kind}
									><span class="route-line"
										><span class="verb">{summary.verb}</span
										>{#each summary.entries as entry, position (entry.name)}{@const last =
												position ===
												summary.entries.length - 1}{#if position && summary.joiner === ' and '}<span
													class="more">and</span
												>{/if}<span class="entry"
												><strong data-game>{entry.name}</strong>{#if entry.place}&nbsp;<span
														class="where">in <span data-game>{entry.place}</span></span
													>{/if}{#if (!last && summary.joiner === ', ') || (last && summary.more && summary.joiner === ' and ')},{/if}</span
											>{/each}{#if summary.more}<span class="more"
												>and {formatNumber(summary.more)} more</span
											>{/if}{#if summary.places.length}<span data-game class="where-only"
												>{summary.places.join(' and ')}</span
											>{/if}</span
									>{#if summary.context}<small>{summary.context}</small
										>{/if}{#each summary.conditions as condition (condition)}<small class="when"
											>{condition}</small
										>{/each}</a
								>
							</li>
						{/each}
						{#if mounted && !allWays && groups.length > firstWays + 1}<li class="more-ways">
								<button type="button" onclick={showAllWays}
									>Show {groups.length - firstWays} more ways to get it</button
								>
							</li>{/if}
					</ul>
				{/if}
			</Section>
			{#if sparse}{@render notes()}{/if}
		</div>
		<aside class="item-side" aria-label="Item details">
			<ItemTooltip
				item={{ ...item, name: displayName }}
				issueHref={item.notes.length
					? resolve('known-issues/') + '#' + item.notes[0].issue
					: undefined}
			/>
			{#if !item.icon}<p class="muted">No icon is known for this item.</p>{/if}
			{#if !sparse}{@render notes()}{/if}
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
			{#if uses.length}<Section
					id="used-for"
					title="Used For"
					count={shownUses.length}
					line={shownUses.length < uses.length
						? `${formatNumber(shownUses.length)} of ${formatNumber(uses.length)} uses match the filters.`
						: undefined}
				>
					{#if useFilters}<div class="use-filters">
							<label
								><span class="field-label">Name</span><input
									type="search"
									bind:value={useName}
									placeholder="Find what it makes"
								/></label
							>
							{#if useFilters.disciplines.length > 1}<label
									><span class="field-label">Skill</span><select bind:value={useDiscipline}
										><option value="">All</option
										>{#each useFilters.disciplines as discipline (discipline)}<option
												value={discipline}>{discipline}</option
											>{/each}</select
									></label
								>{/if}
							{#if useFilters.types.length > 1}<label
									><span class="field-label">Type</span><select bind:value={useType}
										><option value="">All</option>{#each useFilters.types as type (type)}<option
												value={type}>{type}</option
											>{/each}</select
									></label
								>{/if}
							{#if useFilters.leveled}<label
									><span class="field-label">Up to level</span><input
										type="number"
										inputmode="numeric"
										min="0"
										bind:value={useLevel}
									/></label
								>{/if}
						</div>{/if}
					{#if !shownUses.length}<p class="surface muted">
							No use matches these filters. Change or clear a filter to see more.
						</p>{:else}<RelationTable
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
						>{/if}
				</Section>{/if}
			{#if payments.length}<Section id="buys" title="Buys" count={payments.length}>
					<RelationTable
						items={payments}
						label="Buys"
						headings={['Item', 'Merchant', 'Price']}
						numeric={[false, true]}
						>{#snippet row(use, index)}<tr
								id={`buys-${index + 1}`}
								data-relation-id={use.relationId}
								data-relation-direction="outgoing"
								><td
									><EntityLink entity={use.item} icon /><Conditions
										conditions={use.conditions}
									/></td
								><td>{use.name}</td><td class="numeric"
									>{pricePhrase(formatNumber(use.quantity), item)}</td
								></tr
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
								showPlaces={!group.byPlace && group.showPlaces}
								id={`source-${group.kind}-${index + 1}`}
							/>{/snippet}</RelationTable
					>
				</Section>
			{/each}
		</div>
	</div>
</EntityArticle>
