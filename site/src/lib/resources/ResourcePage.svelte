<script lang="ts">
	import { untrack } from 'svelte';
	import { alignSide } from '#lib/ui/align-side.js';
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import Chance from '#lib/ui/Chance.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import { formatChance, formatNumber } from '#lib/format.js';
	import type { EntityKindDefinition, ResourcePage } from '#lib/site-data.js';
	/**
	 * A gathering resource's page: what it needs and gives per harvest, the items one spot gives with their value, and
	 * the areas it stands in with how many a visit finds. Each item row is the relation the item's own page shows.
	 */
	let { resource, kinds }: { resource: ResourcePage; kinds: EntityKindDefinition[] } = $props();
	const reference = $derived({
		kind: resource.kind,
		key: resource.key,
		name: resource.name,
		slug: resource.slug,
		icon: resource.icon ?? null
	});
	const harvest = $derived(resource.harvest ?? null);
	const loot = $derived(resource.loot);
	const places = $derived(resource.places);
	// What one spot is called, for "per deposit" and "From One Tree".
	const spot = $derived(
		(
			{ Woodcutting: 'tree', Mining: 'deposit', Foraging: 'plant', Farming: 'crop' } as Record<
				string,
				string
			>
		)[resource.category] ?? 'spot'
	);
	const title = (text: string) => text.charAt(0).toUpperCase() + text.slice(1);
	// A spot gives several items only for trees and deposits, so only then does a column count them.
	const perSpot = $derived(harvest && harvest.itemsPerSpot > 1 ? harvest.itemsPerSpot : null);
	const hasValue = $derived(loot.some((entry) => entry.value != null));
	const hasMost = $derived(places.some((place) => place.most > 0));
	const lootHeadings = $derived([
		'Item',
		'Chance',
		...(perSpot ? [`From One ${title(spot)}`] : []),
		...(hasValue ? ['Value'] : [])
	]);
	const placeHeadings = $derived(['Area', 'Average per Visit', ...(hasMost ? ['Most'] : [])]);

	// Your level, for the bonus chance. It starts at the level the resource needs.
	let level = $state(untrack(() => resource.harvest?.bonusFrom ?? 1));
	const bonusAt = (at: number) =>
		harvest && harvest.bonus.length
			? harvest.bonus[Math.min(Math.max(at - harvest.bonusFrom, 0), harvest.bonus.length - 1)]
			: null;
	const certainAt = $derived(
		harvest && harvest.bonus.length ? harvest.bonusFrom + harvest.bonus.length - 1 : null
	);
	const afterUse = $derived(
		!harvest
			? null
			: harvest.afterUse === 'stays'
				? `A deposit you mine stays empty until you leave the area and come back.`
				: harvest.minigame
					? `A tree you cut down is gone until you leave the area and come back.`
					: `A plant you harvest is gone until you leave the area and come back.`
	);
	const description = $derived(
		`Where to find ${resource.name} in The Bloodline, what it gives and what each item is worth` +
			(harvest ? `, with the ${harvest.profession} level and experience it needs and gives.` : '.')
	);
	const kindTitle = $derived(
		(
			{
				Woodcutting: 'Tree',
				Mining: 'Deposit',
				Foraging: 'Foraging Spot',
				Farming: 'Crop',
				Gathering: 'Gathering Spot'
			} as Record<string, string>
		)[resource.category] ?? 'Resource'
	);
</script>

{#snippet facts()}
	<dl class="facts">
		<div>
			<dt>Needs</dt>
			<dd>{harvest!.profession} level {formatNumber(harvest!.level)}</dd>
		</div>
		<div>
			<dt>Yield</dt>
			<dd>
				{formatNumber(harvest!.itemsPerSpot)}
				{harvest!.itemsPerSpot === 1 ? 'item' : 'items'} per {spot}
			</dd>
		</div>
		<div>
			<dt>Experience</dt>
			<dd>{formatNumber(harvest!.experience)} per {harvest!.minigame ? 'hit' : 'harvest'}</dd>
		</div>
	</dl>
{/snippet}

{#snippet bonus()}
	{#if harvest && harvest.bonus.length}<div class="bonus">
			<label
				>Your {harvest.profession} Level
				<input type="number" min={harvest.bonusFrom} bind:value={level} /></label
			>
			<p class="chance">
				Bonus chance per hit <strong>{formatChance(bonusAt(level) ?? 0)}</strong>
			</p>
			<p class="muted">
				A swing that hits the green area can also give bonus items. The chance grows with your level
				and is certain from level {formatNumber(certainAt!)}. A swing that misses still gives its
				item, but no experience and no bonus.
			</p>
		</div>{/if}
{/snippet}

{#snippet gives()}
	<Section
		id="gives"
		title="Gives"
		count={loot.length}
		line={loot.length === 1
			? `Each ${harvest?.minigame ? 'swing' : 'harvest'} gives this item.`
			: `Each ${harvest?.minigame ? 'swing' : 'harvest'} gives one of these items, by these chances.`}
	>
		<RelationTable
			items={loot}
			label="Items {resource.name} gives"
			headings={lootHeadings}
			collapse={false}
			>{#snippet row(entry, index)}<tr
					data-relation-id={entry.relationIds[0]}
					data-relation-direction="outgoing"
					><td
						><EntityLink
							entity={entry.item}
							icon
						/>{#each entry.relationIds.slice(1) as id (id)}<span
								data-relation-id={id}
								data-relation-direction="outgoing"
							></span>{/each}</td
					><td class="numeric" class:absent={entry.chance == null}
						>{#if entry.chance == null}Unknown{:else}<Chance
								min={entry.chance}
								id={`gives-${index + 1}-chance`}
							/>{/if}</td
					>{#if perSpot}<td class="numeric"
							>{entry.chance == null
								? 'Unknown'
								: formatNumber(Math.round(perSpot * entry.chance * 10) / 10)}</td
						>{/if}{#if hasValue}<td class="numeric" class:absent={entry.value == null}
							>{entry.value == null ? 'Unknown' : `${formatNumber(entry.value)} gold`}</td
						>{/if}</tr
				>{/snippet}</RelationTable
		>
	</Section>
{/snippet}

{#snippet where()}
	{#if places.length}<Section
			id="where"
			title="Where"
			count={places.length}
			line="Not every {spot} is there each time you arrive. The game decides which ones appear whenever you enter an area, so the average is what you can expect, and Most is every one that can appear.{afterUse
				? ' ' + afterUse
				: ''}"
		>
			<RelationTable
				items={places}
				label="Areas with {resource.name}"
				headings={placeHeadings}
				stackOnMobile={false}
				>{#snippet row(place)}<tr
						><td><EntityLink entity={place.place} /></td><td class="numeric"
							>{place.more && place.expected === 0
								? 'In dungeon runs'
								: formatNumber(Math.max(1, Math.round(place.expected)))}</td
						>{#if hasMost}<td class="numeric" class:absent={place.most === 0}
								>{place.most === 0 ? 'Unknown' : formatNumber(place.most)}</td
							>{/if}</tr
					>{/snippet}</RelationTable
			>
		</Section>{/if}
{/snippet}

<EntityPage
	entity={reference}
	{kinds}
	title={`${resource.name}, ${kindTitle}`}
	{description}
	icon={resource.icon ?? null}
	detail={resource.category}
>
	<header class="item-heading">
		<div>
			<h1>{resource.name}</h1>
			{#if !harvest && resource.category === 'Farming'}<p class="line">
					A crop you grow on a farming plot.
				</p>{/if}
		</div>
	</header>
	{#if harvest}<div class="detail-layout" use:alignSide>
			<div class="answer" data-main data-pagefind-ignore="all">{@render gives()}</div>
			<aside class="item-side panel harvest" data-side aria-labelledby="harvest">
				<h2 id="harvest">Harvest</h2>
				{@render facts()}
				{@render bonus()}
			</aside>
			<div class="item-relations" data-main data-pagefind-ignore="all">{@render where()}</div>
		</div>{:else}<div class="item-relations" data-pagefind-ignore="all">
			{@render gives()}{@render where()}
		</div>{/if}
</EntityPage>

<style>
	h1 {
		margin: 0;
	}
	.line {
		margin: var(--gap-line) 0 0;
		color: var(--text-muted);
	}
	/* The Harvest panel lists each fact as a label and its value on one line. */
	.harvest {
		padding: var(--inset);
	}
	.harvest h2 {
		margin: 0;
		font-size: var(--type-sub);
		line-height: var(--leading-heading);
	}
	.facts {
		display: grid;
		gap: var(--gap-line);
		margin: 0;
	}
	.facts div {
		display: flex;
		justify-content: space-between;
		gap: var(--gap-inline);
	}
	.facts dt {
		color: var(--text-muted);
	}
	.facts dd {
		margin: 0;
		text-align: end;
	}
	.bonus {
		display: grid;
		gap: var(--gap-block);
		padding-top: var(--gap-block);
		border-top: var(--border-width) solid var(--border);
	}
	.bonus p {
		margin: 0;
	}
	.bonus label {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--gap-inline);
	}
	.bonus input {
		width: var(--size-filter-control);
	}
	.chance {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: var(--gap-inline);
	}
	.chance strong {
		font-size: var(--type-sub);
	}
	.muted {
		color: var(--text-muted);
	}
</style>
