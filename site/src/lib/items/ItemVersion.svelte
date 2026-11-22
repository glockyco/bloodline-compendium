<script lang="ts">
	import SourceSections from '#lib/entity/SourceSections.svelte';
	import UnconfirmedSources from '#lib/entity/UnconfirmedSources.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Conditions from '#lib/Conditions.svelte';
	import BugsSection from '#lib/entity/BugsSection.svelte';
	import Section from '#lib/ui/Section.svelte';
	import ItemTooltip from '#lib/items/ItemTooltip.svelte';
	import ItemDetails from '#lib/items/ItemDetails.svelte';
	import ItemModel from '#lib/items/ItemModel.svelte';
	import OnHitEffects from '#lib/items/OnHitEffects.svelte';
	import { detailRows } from '#lib/items/game-card.js';
	import { sourceHeadings } from '#lib/labels.js';
	import { recipeKinds, sortSources, useHeadings, pricePhrase } from '#lib/items/acquisition.js';
	import RecipeLine from '#lib/items/RecipeLine.svelte';
	import { formatNumber } from '#lib/format.js';
	import type { ItemPage, SourceKind } from '#lib/site-data.js';
	import { statusSentence } from '#lib/items/describe.js';
	let {
		item,
		idPrefix = ''
	}: {
		item: ItemPage;
		idPrefix?: string;
	} = $props();
	// Details keep mixed projectile hits together beside the game card.
	const details = $derived(detailRows(item.facts, item.projectileGroups));
	const confirmedSources = $derived(item.sources.filter((source) => !source.unconfirmed));
	// Recipes show as recipe lines of their own, smithing first as the source sections order their kinds.
	const recipes = $derived(
		(Object.keys(sourceHeadings) as SourceKind[])
			.filter((kind) => recipeKinds.has(kind))
			.flatMap((kind) => sortSources(confirmedSources.filter((source) => source.kind === kind)))
	);
	const sortedUses = $derived(
		[...item.uses].sort(
			(a, b) => a.item.name.localeCompare(b.item.name, 'en') || a.name.localeCompare(b.name, 'en')
		)
	);
	// The skill level a use needs shows in its own column, so the condition that states it does not repeat.
	const levelOf = (use: ItemPage['uses'][number]) =>
		use.conditions.find((condition) => condition.kind === 'proficiencyLevel')?.value ?? null;
	// A merchant that takes the item as payment sells something for it, which is not a recipe, so payments get their
	// own table with the merchant and the price. Uses run from the lowest level they need, as a crafter unlocks them,
	// and uses without a level follow.
	const uses = $derived(
		sortedUses
			.filter((use) => use.kind !== 'currencyShop')
			.sort((a, b) => (levelOf(a) ?? Infinity) - (levelOf(b) ?? Infinity))
	);
	const payments = $derived(sortedUses.filter((use) => use.kind === 'currencyShop'));
	const otherConditions = (use: (typeof uses)[number]) =>
		use.conditions.filter((condition) => condition.kind !== 'proficiencyLevel');
	// A long list of uses filters by product name, profession, product type and the level a player has.
	const useFilters = $derived(
		uses.length > 10
			? {
					professions: [
						...new Set(uses.map((use) => use.profession?.name ?? useLabel(use.kind)))
					].sort(),
					types: [
						...new Set(uses.map((use) => use.outputType).filter((value) => value != null))
					].sort(),
					leveled: uses.some((use) => levelOf(use) != null)
				}
			: null
	);
	let useName = $state('');
	let useProfession = $state('');
	let useType = $state('');
	let useLevel = $state<number | null>(null);
	const shownUses = $derived.by(() => {
		const needle = useName.trim().toLocaleLowerCase('en');
		return uses.filter(
			(use) =>
				(!needle || use.item.name.toLocaleLowerCase('en').includes(needle)) &&
				(!useProfession || (use.profession?.name ?? useLabel(use.kind)) === useProfession) &&
				(!useType || use.outputType === useType) &&
				(useLevel == null || (levelOf(use) ?? 0) <= useLevel)
		);
	});
	// An item without a confirmed way to get it says so, which every source section would otherwise answer.
	const unsourced = $derived(!confirmedSources.length);
	function useLabel(kind: SourceKind): string {
		return useHeadings[kind] ?? sourceHeadings[kind];
	}
</script>

<div class="detail-layout">
	<div class="answer" data-pagefind-ignore="all">
		<ItemModel name={item.name} renders={item.renders} />
		{#if details.length || item.projectileGroups?.length}<Section
				id={idPrefix + 'details'}
				title="Details"
			>
				<div class="surface detail-surface">
					<ItemDetails
						rows={details}
						projectileGroups={item.projectileGroups}
						charged={item.facts.find((fact) => fact.stat === 'chargedBodyHitDamage')}
						wide
					/>
				</div>
			</Section>{/if}
		<OnHitEffects effects={item.onHitEffects} id={idPrefix + 'on-hit-effects'} />
		<!-- The ways to get an item follow as their own sections, and only an item without one says so here. -->
		{#if unsourced}<Section id={idPrefix + 'how-to-get-it'} title="How to Get It">
				<p class="surface">{statusSentence[item.status]}</p>
			</Section>{/if}
	</div>
	<aside class="item-side" aria-label="Game card">
		<ItemTooltip {item} details={false} />
	</aside>
	<div class="item-relations" data-pagefind-ignore="all">
		{#if recipes.length}<div class="recipes">
				<Section
					id={idPrefix + 'recipes'}
					title={recipes.length === 1 ? 'Recipe' : 'Recipes'}
					count={recipes.length > 1 ? recipes.length : undefined}
				>
					{#each recipes as source, index (source.relationId)}<RecipeLine
							{source}
							id={`${idPrefix}recipe-${index + 1}`}
						/>{/each}
				</Section>
			</div>{/if}
		<SourceSections sources={item.sources} {idPrefix} />
		{#if uses.length}<Section
				id={idPrefix + 'used-for'}
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
						{#if useFilters.professions.length > 1}<label
								><span class="field-label">Profession</span><select bind:value={useProfession}
									><option value="">All</option
									>{#each useFilters.professions as profession (profession)}<option
											value={profession}>{profession}</option
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
									placeholder="Any level"
									bind:value={useLevel}
								/></label
							>{/if}
					</div>{/if}
				{#if !shownUses.length}<p class="surface muted">
						No use matches these filters. Change or clear a filter to see more.
					</p>{:else}<RelationTable
						items={shownUses}
						label="Used for"
						headings={['Makes', 'Profession', 'Needed']}
						numeric={[false, true]}
						>{#snippet row(use, index)}<tr
								id={`${idPrefix}use-${index + 1}`}
								data-relation-id={use.relationId}
								data-relation-direction="outgoing"
								><td
									><EntityLink entity={use.item} icon /><Conditions
										conditions={otherConditions(use)}
									/></td
								><td
									>{#if use.profession}<EntityLink entity={use.profession} />{:else}{useLabel(
											use.kind
										)}{/if}{#if levelOf(use) != null}<span class="muted"
											>{` ${formatNumber(levelOf(use)!)}`}</span
										>{/if}</td
								><td class="numeric">{formatNumber(use.quantity)}</td></tr
							>{/snippet}</RelationTable
					>{/if}
			</Section>{/if}
		{#if payments.length}<Section id={idPrefix + 'buys'} title="Buys" count={payments.length}>
				<RelationTable
					items={payments}
					label="Buys"
					headings={['Item', 'Merchant', 'Price']}
					numeric={[false, true]}
					>{#snippet row(use, index)}<tr
							id={`${idPrefix}buys-${index + 1}`}
							data-relation-id={use.relationId}
							data-relation-direction="outgoing"
							><td
								><EntityLink entity={use.item} icon /><Conditions conditions={use.conditions} /></td
							><td>{use.name}</td><td class="numeric"
								>{pricePhrase(formatNumber(use.quantity), item)}</td
							></tr
						>{/snippet}</RelationTable
				>
			</Section>{/if}
		<UnconfirmedSources sources={item.sources} name={item.name} {idPrefix} />
		<BugsSection notes={item.notes} facts={item.facts} name={item.name} {idPrefix} />
	</div>
</div>
