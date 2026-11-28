<script lang="ts">
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import Section from '#lib/ui/Section.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid, { balancedColumns } from '#lib/ui/CardGrid.svelte';
	import ItemStrip from '#lib/entity/ItemStrip.svelte';
	import ItemFinder from './ItemFinder.svelte';
	import { placeRoles, placeSections } from '#lib/labels.js';
	import { formatDays, plural } from '#lib/format.js';
	import type {
		EntityKindDefinition,
		EntityReference,
		PlaceItem,
		PlacePage,
		SourceKind
	} from '#lib/site-data.js';
	/**
	 * A place's page: who and what is there, each as one card that says what it does and shows the items it gives,
	 * in sections by kind of thing, as a visitor meets them, then the events that can happen there. A person who
	 * sells and also gives items in dialogue is one card with both. Finding an item narrows every card to the things
	 * that give it.
	 */
	let {
		place,
		kinds,
		categories
	}: {
		place: PlacePage;
		kinds: EntityKindDefinition[];
		categories: { slug: string; plural: string }[];
	} = $props();
	const reference = $derived({
		kind: place.kind,
		key: place.key,
		name: place.name,
		slug: place.slug,
		icon: null
	});
	interface Thing {
		entity: EntityReference;
		roles: { kind: SourceKind; items: PlaceItem[] }[];
		items: PlaceItem[];
		relationIds: string[];
	}
	// One thing per entity across the ways it gives items, with each way's count and every item once. A merchant and
	// a person of one key, such as a vendor who also gives items in dialogue, are one card.
	const things = $derived(
		[
			...Map.groupBy(
				place.groups.flatMap((group) => group.things.map((entry) => ({ kind: group.kind, entry }))),
				({ entry }) => entry.entity.key ?? entry.entity.name
			).values()
		].map((ways): Thing => ({
			entity: ways[0].entry.entity,
			roles: ways.map(({ kind, entry }) => ({ kind, items: entry.items })),
			items: [
				...Map.groupBy(
					ways.flatMap(({ entry }) => entry.items),
					(item) => item.item.key ?? item.item.name
				).values()
			].map((same) => same[0]),
			relationIds: ways.flatMap(({ entry }) => entry.relationIds)
		}))
	);
	let selected = $state<string | null>(null);
	let query = $state('');
	const matches = (entry: PlaceItem) =>
		(selected === null || entry.category.slug === selected) &&
		entry.item.name.toLocaleLowerCase('en').includes(query.trim().toLocaleLowerCase('en'));
	const finding = $derived(selected !== null || query.trim() !== '');
	const sections = $derived(
		placeSections
			.map((section) => {
				const all = things
					.filter((thing) => section.kinds.includes(thing.entity.kind))
					.toSorted((a, b) => a.entity.name.localeCompare(b.entity.name, 'en'));
				return {
					...section,
					all,
					shown: all
						.map((thing) => ({
							...thing,
							items: thing.items.filter(matches),
							// A card's counts follow the finder, so a filtered card says what it gives of that kind.
							roles: thing.roles
								.map((role) => ({ ...role, items: role.items.filter(matches) }))
								.filter((role) => role.items.length)
						}))
						.filter((thing) => thing.items.length)
				};
			})
			.filter((section) => section.all.length)
	);
	const items = $derived([
		...new Map(
			things.flatMap((thing) => thing.items.map((entry) => [entry.item.key, entry] as const))
		).values()
	]);
	const itemKinds = $derived(
		[...Map.groupBy(items, (entry) => entry.category.slug)]
			.map(([slug, entries]) => ({
				slug,
				label:
					categories.find((category) => category.slug === slug)?.plural ?? entries[0].category.name,
				count: entries.length
			}))
			.toSorted((a, b) => b.count - a.count || a.label.localeCompare(b.label, 'en'))
	);
	// The overview tiles: one per section, and the skills to learn, each with an icon of what it holds.
	const tiles = $derived([
		...sections.map((section) => ({
			id: section.id,
			text: plural(section.all.length, section.noun[0], section.noun[1]),
			icon: section.all.find((thing) => thing.items[0]?.item.icon)?.items[0].item.icon ?? null
		})),
		...(place.lessons.length
			? [
					{
						id: 'lessons',
						text: plural(place.lessons.length, 'skill to learn', 'skills to learn'),
						icon: place.lessons.find((lesson) => lesson.entity.icon)?.entity.icon ?? null
					}
				]
			: [])
	]);
	// Cards keep the width of four a line, so a short section's cards match the sections around it, and a longer
	// one wraps where its last line is fullest.
	const columns = (count: number) => (count <= 4 ? 4 : balancedColumns(count, 4));
	const description = $derived(
		`${place.name} in The Bloodline: who and what is there, and the items each sells, drops or gives.`
	);
</script>

<EntityPage
	entity={reference}
	{kinds}
	title={`${place.name}, Place`}
	{description}
	icon={null}
	detail="Place"
>
	<header class="item-heading">
		<div>
			<h1 data-game>{place.name}</h1>
			{#if !tiles.length}<p class="line">Nothing here gives items.</p>{/if}
		</div>
	</header>
	<div class="place" data-pagefind-ignore="all">
		{#if tiles.length}<CardGrid
				rows={1}
				columns={balancedColumns(tiles.length, 4)}
				small
				label="What is in {place.name}"
			>
				{#each tiles as tile (tile.id)}<Card href="#{tile.id}"
						><span class="tile"
							><IconSlot icon={tile.icon} size="pair" /><span>{tile.text}</span></span
						></Card
					>{/each}
			</CardGrid>{/if}
		{#if items.length > 1}<ItemFinder kinds={itemKinds} bind:query bind:selected />{/if}
		{#each sections as section (section.id)}{#if section.shown.length}<Section
					id={section.id}
					title={section.title}
					count={section.shown.length}
				>
					{#if section.id === 'ground'}<CardGrid
							rows={1}
							columns={balancedColumns(section.shown.length, 4)}
							small
							label="Items on the ground in {place.name}"
						>
							{#each section.shown
								.flatMap((thing) => thing.items.map((entry) => ({ entry, ids: thing.relationIds })))
								.toSorted( (a, b) => a.entry.item.name.localeCompare(b.entry.item.name, 'en') ) as ground (ground.entry.item.key)}<Card
									><span class="ground"
										>{#each ground.ids as id (id)}<span
												data-relation-id={id}
												data-relation-direction="outgoing"
											></span>{/each}<EntityLink entity={ground.entry.item} icon /></span
									></Card
								>{/each}
						</CardGrid>{:else}<CardGrid
							rows={3}
							columns={columns(section.shown.length)}
							small
							label={section.title}
						>
							{#each section.shown as thing (thing.entity.key ?? thing.entity.name)}<Card>
									<p class="name"><EntityLink entity={thing.entity} /></p>
									<p class="roles">
										{thing.roles
											.map((role) => placeRoles[role.kind]?.(role.items.length))
											.filter(Boolean)
											.join(' · ')}
									</p>
									<ItemStrip
										items={thing.items.map((entry) => entry.item)}
										label="Items from {thing.entity.name}"
									/>
								</Card>{/each}
						</CardGrid>{/if}
				</Section>{/if}{/each}
		{#if finding && !sections.some((section) => section.shown.length)}<p class="surface muted">
				Nothing here gives such an item. Pick another kind or clear the name.
			</p>{/if}
		{#if place.lessons.length && !finding}<Section
				id="lessons"
				title="Learned Here"
				count={place.lessons.length}
			>
				<CardGrid
					rows={2}
					columns={columns(place.lessons.length)}
					small
					label="Skills and spellsongs learned in {place.name}"
				>
					{#each place.lessons as lesson (lesson.relationId)}<Card>
							<p
								class="name"
								data-relation-id={lesson.relationId}
								data-relation-direction="outgoing"
							>
								<EntityLink entity={lesson.entity} icon />
							</p>
							<p class="roles">
								{lesson.kind === 'noteSheet' ? 'From a note sheet' : 'From a skill book'}
							</p>
						</Card>{/each}
				</CardGrid>
			</Section>{/if}
		{#if place.events.length && !finding}<Section
				id="events"
				title="Events"
				count={place.events.length}
			>
				<CardGrid rows={3} columns={columns(place.events.length)} label="Events in {place.name}">
					{#each place.events as happening (happening.event.name)}<Card>
							<p class="name">
								<EntityLink entity={happening.event.page}>{happening.event.name}</EntityLink>
							</p>
							<p class="roles">{formatDays(happening.chance)} here</p>
							<p class="roles" data-game>{happening.description}</p>
						</Card>{/each}
				</CardGrid>
			</Section>{/if}
	</div>
</EntityPage>

<style>
	.place {
		display: grid;
		gap: var(--gap-section);
	}
	.tile {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
		font-weight: var(--weight-strong);
	}
	.name {
		margin: 0;
		font-weight: var(--weight-strong);
	}
	.roles {
		margin: 0;
		color: var(--text-muted);
		font-size: var(--type-meta);
	}
	.ground {
		display: block;
	}
</style>
