<script lang="ts">
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import Chance from '#lib/ui/Chance.svelte';
	import Hint from '#lib/ui/Hint.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import Conditions from '#lib/Conditions.svelte';
	import { formatAmount } from '#lib/items/acquisition.js';
	import { eventLabels } from '#lib/labels.js';
	import type { CreaturePage, EntityKindDefinition } from '#lib/site-data.js';
	/**
	 * A creature's page: what it drops, each row the relation its item's Dropped by row shows, and the areas and
	 * events that bring it.
	 */
	let { creature, kinds }: { creature: CreaturePage; kinds: EntityKindDefinition[] } = $props();
	const reference = $derived({
		kind: creature.kind,
		key: creature.key,
		name: creature.name,
		slug: creature.slug,
		icon: null
	});
	const drops = $derived(creature.drops);
	// Classes of one name can live in different areas, so rows name their areas when they differ.
	const varied = $derived(
		drops.some(
			(drop) =>
				JSON.stringify([drop.places, drop.events]) !==
				JSON.stringify([drops[0].places, drops[0].events])
		)
	);
	const showAmount = $derived(
		drops.some((drop) => drop.amount && (drop.amount.min !== 1 || drop.amount.max !== 1))
	);
	const headings = $derived(['Item', 'Chance per Kill', ...(showAmount ? ['Quantity'] : [])]);
	const eventLines = $derived(
		(Object.keys(eventLabels) as (keyof typeof eventLabels)[])
			.map((kind) => ({ kind, events: creature.events.filter((event) => event.kind === kind) }))
			.filter((line) => line.events.length)
	);
	const description = $derived(
		`What ${creature.name} drops in The Bloodline, with the chance of each item per kill, and where to find it.`
	);
</script>

<EntityPage
	entity={reference}
	{kinds}
	title={`${creature.name}, Creature`}
	{description}
	icon={null}
	detail="Creature"
>
	<header class="item-heading">
		<div>
			<h1 data-game>{creature.name}</h1>
			{#if creature.unconfirmed}<p class="line">
					No creature of this kind is placed anywhere in the world, so what it drops is not
					confirmed.
				</p>{/if}
		</div>
	</header>
	<div class="item-relations" data-pagefind-ignore="all">
		{#if drops.length}<Section id="drops" title="Drops" count={drops.length}>
				<RelationTable items={drops} label="Items {creature.name} drops" {headings} collapse={false}
					>{#snippet row(drop, index)}<tr
							data-relation-id={drop.relationIds[0]}
							data-relation-direction="outgoing"
							><td
								><EntityLink
									entity={drop.item}
									icon
								/>{#each drop.relationIds.slice(1) as id (id)}<span
										data-relation-id={id}
										data-relation-direction="outgoing"
									></span>{/each}<Conditions conditions={drop.conditions} />{#if varied}<p
										class="source-places"
									>
										{#if drop.places.length}{#each drop.places as place, at (place.key ?? place.name)}{at
													? ', '
													: ''}<EntityLink
													entity={place}
												/>{/each}{:else if !drop.events.length}Location unknown{/if}{#each drop.events as event, at (event.name)}{at ||
											drop.places.length
												? ', '
												: ''}<EntityLink entity={event.page}>{event.name}</EntityLink>{/each}
									</p>{/if}{#each drop.notes as note (note)}<p class="source-places">
										{note}
									</p>{/each}</td
							><td class="numeric"
								>{#if drop.odds?.chance != null}<Chance
										min={drop.odds.chance}
										max={drop.odds.highestChance ?? drop.odds.chance}
										id={`drop-${index + 1}-chance`}
									/>{:else if drop.odds?.unknown}<Hint
										text="Unknown"
										hint={drop.odds.unknown}
										id={`drop-${index + 1}-chance`}
										soft
									/>{:else}<span class="muted">Unknown</span>{/if}</td
							>{#if showAmount}<td class="numeric"
									>{drop.amount ? formatAmount(drop.amount) : 'Unknown'}</td
								>{/if}</tr
						>{/snippet}</RelationTable
				>
			</Section>{/if}
		<Section id="where" title="Where to Find It" count={creature.places.length || undefined}>
			<div class="surface">
				{#if creature.places.length}<p>
						{#each creature.places as place, index (place.key ?? place.name)}{index
								? ', '
								: ''}<EntityLink entity={place} />{/each}
					</p>{:else}<p class="muted">Location unknown</p>{/if}
				{#each eventLines as line (line.kind)}<p>
						{eventLabels[line.kind][line.events.length === 1 ? 0 : 1] +
							' '}{#each line.events as event, index (event.name)}{index ? ', ' : ''}<EntityLink
								entity={event.page}>{event.name}</EntityLink
							>{/each}
					</p>{/each}
			</div>
		</Section>
	</div>
</EntityPage>
