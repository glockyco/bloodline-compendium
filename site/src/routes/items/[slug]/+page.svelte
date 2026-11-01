<script lang="ts">
	import { resolve } from '$app/paths';
	import SourceRow from '#lib/SourceRow.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import Conditions from '#lib/Conditions.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { bugRows } from '#lib/bugs.js';
	import Section from '#lib/ui/Section.svelte';
	import ItemTooltip from '#lib/ItemTooltip.svelte';
	import ItemDetails from '#lib/ItemDetails.svelte';
	import OnHitEffects from '#lib/OnHitEffects.svelte';
	import { detailRows } from '#lib/game-card.js';
	import { sourceColumns, sourceHeadings, sourceIntros, titleCase } from '#lib/labels.js';
	import {
		headingCase,
		oddsLabel,
		recipeKinds,
		sortSources,
		useHeadings,
		pricePhrase
	} from '#lib/acquisition.js';
	import RecipeLine from '#lib/RecipeLine.svelte';
	import { formatNumber } from '#lib/format.js';
	import type { Source, SourceKind } from '#lib/site-data.js';
	import SeoHead from '#lib/SeoHead.svelte';
	import { describeItem, statusSentence } from '#lib/describe.js';
	let { data } = $props();
	const item = $derived(data.item);
	// An item page exists only for a kind the registry gives pages.
	const kind = $derived(data.kinds.find((entry) => entry.kind === item.kind)!);
	const itemsPath = $derived(kindPath(item.kind, data.kinds)!);
	const displayName = $derived(item.name + (item.qualifier ? ` (${item.qualifier})` : ''));
	// Details keep mixed projectile hits together beside the game card.
	const details = $derived(detailRows(item.facts, item.projectileGroups));
	const confirmedSources = $derived(item.sources.filter((source) => !source.unconfirmed));
	const unconfirmedSources = $derived(item.sources.filter((source) => source.unconfirmed));
	const bugs = $derived(bugRows(item));
	const groups = $derived(
		(Object.keys(sourceHeadings) as SourceKind[])
			.map((kind) => {
				const listed = sortSources(confirmedSources.filter((source) => source.kind === kind));
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
				const showSpawn =
					kind === 'containerLoot' &&
					sources.some((source) => source.spawn && source.spawn.min < 1);
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
						? `Chance ${headingCase(events[0])}`
						: 'Chance';
				return {
					kind,
					sources,
					byPlace,
					showPlaces,
					recipe: recipeKinds.has(kind),
					showAmount,
					showSpawn,
					showContext,
					sharedUnit,
					headings: [
						sourceColumns[kind],
						...(showAmount ? ['Quantity'] : []),
						...(showSpawn ? ['Spawns'] : []),
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
	const description = $derived(describeItem(item, confirmedSources));
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
			{#if details.length || (item.projectileGroups && item.projectileGroups.length > 1)}<Section
					id="details"
					title="Details"
				>
					<div class="surface detail-surface">
						<ItemDetails rows={details} projectileGroups={item.projectileGroups} wide />
					</div>
				</Section>{/if}
			<OnHitEffects effects={item.onHitEffects} />
			<!-- The ways to get an item follow as their own sections, and only an item without one says so here. -->
			{#if !groups.length}<Section id="how-to-get-it" title="How to Get It">
					<p class="surface">{statusSentence[item.status]}</p>
				</Section>{/if}
		</div>
		<aside class="item-side" aria-label="Game card">
			<ItemTooltip item={{ ...item, name: displayName }} details={false} />
			{#if !item.icon}<p class="muted">No icon is known for this item.</p>{/if}
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
			{#each sourceGroups as group (group.kind)}
				<Section
					class="source-group"
					id={'source-' + group.kind}
					title={sourceHeadings[group.kind]}
					count={group.sources.length}
					line={sourceIntros[group.kind]}
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
								showSpawn={group.showSpawn}
								showContext={group.showContext}
								showUnit={!group.sharedUnit}
								showPlaces={!group.byPlace && group.showPlaces}
								id={`source-${group.kind}-${index + 1}`}
							/>{/snippet}</RelationTable
					>
				</Section>
			{/each}
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
			{#if unconfirmedSources.length}<Section
					id="only-in-game-files"
					title="Only in the Game Files"
					count={unconfirmedSources.length}
				>
					<RelationTable
						items={unconfirmedSources}
						label="Only in the Game Files"
						headings={['Source', 'Missing Evidence']}
						numeric={false}
						>{#snippet row(source, index)}<tr
								id={`unconfirmed-source-${index + 1}`}
								data-relation-id={source.relationId}
								data-relation-direction="incoming"
								><td
									><span class="relation-name"
										><EntityLink entity={source.entity}>{source.name}</EntityLink></span
									>{#if source.places.length}<p class="source-places">
											{#each source.places as place, index (index)}{index ? ', ' : ''}<EntityLink
													entity={place}
												/>{/each}
										</p>{/if}</td
								><td
									>{#each source.notes as note, index (index)}<p>{note}</p>{/each}</td
								></tr
							>{/snippet}</RelationTable
					>
				</Section>{/if}
			{#if bugs.length}
				<Section id="bugs" title="Bugs" count={bugs.length}>
					<ul class="surface bug-rows">
						{#each bugs as row (row.kind === 'issue' ? row.issue : row.fact.stat)}
							<li>
								{#if row.kind === 'issue'}<span
										>{#each row.sentence.split(item.name) as part, index (index)}{#if index}<span
													data-game>{item.name}</span
												>{/if}{part}{/each}</span
									>{#each row.differences as fact (fact.stat)}<small
											>The game's tooltip shows {fact.tooltipLabel ?? fact.label}
											{fact.gameShows}</small
										>{/each}<small
										><!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- the resolved issue page with its section. -->
										<a href={resolve('known-issues/') + '#' + row.issue}>Read about this issue</a
										></small
									>
								{:else}<span
										>The game's tooltip shows {row.fact.tooltipLabel ?? row.fact.label}
										{row.fact.gameShows}</span
									>{#if row.fact.gameShowsIssue}<small
											><!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- the resolved issue page with its section. -->
											<a href={resolve('known-issues/') + '#' + row.fact.gameShowsIssue}
												>Read about this issue</a
											></small
										>{/if}
								{/if}
							</li>
						{/each}
					</ul>
				</Section>
			{/if}
		</div>
	</div>
</EntityArticle>
