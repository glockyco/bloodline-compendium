<script lang="ts">
	import type { RelationCondition, Source } from '../site-data.js';
	import { formatNumber } from '../format.js';
	import { alwaysThere, formatAmount, oddsLabel, payPrice, placedKinds } from './acquisition.js';
	import { eventLabels, placeUnknownReasons } from '../labels.js';
	import EntityLink from '../ui/EntityLink.svelte';
	import Conditions from '../Conditions.svelte';
	import Chance from '../ui/Chance.svelte';
	import Hint from '../ui/Hint.svelte';
	let {
		source,
		showAmount = false,
		showSpawn = false,
		showContext = false,
		showPlaces = true,
		placeFirst = false,
		showUnit = true,
		levelCondition,
		alsoRelationIds = [],
		id
	}: {
		source: Source;
		showAmount?: boolean;
		showSpawn?: boolean;
		showContext?: boolean;
		showPlaces?: boolean;
		/** Whether the row is its place, as on Picked Up, instead of the thing that gives the item. */
		placeFirst?: boolean;
		/** Whether the chance names its event, which the heading names instead when every row shares it. */
		showUnit?: boolean;
		/**
		 * The kind of condition a level column shows, such as a skill tree's discipline level. The row then shows its
		 * value in that column, and its conditions leave it out.
		 */
		levelCondition?: RelationCondition['kind'];
		/** Further relations this row stands for, when rows that read the same are shown as one. */
		alsoRelationIds?: string[];
		id: string;
	} = $props();
	const level = $derived(
		levelCondition
			? source.conditions.find((condition) => condition.kind === levelCondition)
			: undefined
	);
	const conditions = $derived(
		levelCondition
			? source.conditions.filter((condition) => condition.kind !== levelCondition)
			: source.conditions
	);
	// A gathering row whose resource has a page says how many areas have it, and the page lists them with the notes.
	const summarized = $derived(source.kind === 'gathering' && source.entity?.slug != null);
	// The camp maps of one terrain share their terrain's name, so a list names each place once.
	const places = $derived(
		source.places.filter(
			(place, index) => source.places.findIndex((other) => other.name === place.name) === index
		)
	);
	// A way an event brings is not without a place, and each kind of event has its own line.
	const events = $derived(source.events ?? []);
	const eventLines = $derived(
		(Object.keys(eventLabels) as (keyof typeof eventLabels)[])
			.map((kind) => ({ kind, events: events.filter((event) => event.kind === kind) }))
			.filter((line) => line.events.length)
	);
</script>

<tr
	class="source-row"
	class:with-spawn={showSpawn}
	{id}
	data-relation-id={source.relationId}
	data-relation-direction="incoming"
>
	<td class="source-identity">
		{#snippet placeList()}{#if places.length}{#each places as place, index (index)}{index
						? ', '
						: ''}<EntityLink entity={place} />{/each}{:else if source.settlement}Your settlement{:else}Location
				unknown{#if placeUnknownReasons[source.kind]}{`. ${placeUnknownReasons[source.kind]}`}{/if}{/if}{/snippet}
		{#each alsoRelationIds as also (also)}<span
				data-relation-id={also}
				data-relation-direction="incoming"
			></span>{/each}
		<span class="relation-name"
			>{#if placeFirst && (places.length || source.settlement || !events.length)}{@render placeList()}{:else}<EntityLink
					entity={source.entity}
				/>{/if}</span
		>
		<Conditions {conditions} />
		<!-- Random loot and crops stand nowhere in particular, so only ways that stand somewhere miss a place. -->
		{#if summarized}{#if places.length}<p class="source-places">
					{places.length === 1 ? 'In 1 area' : `In ${formatNumber(places.length)} areas`}
				</p>{/if}{:else if showPlaces && !placeFirst && (places.length || source.settlement || (placedKinds.has(source.kind) && !events.length))}<p
				class="source-places"
			>
				{@render placeList()}
			</p>{/if}
		{#each eventLines as line (line.kind)}<p class="source-places">
				{eventLabels[line.kind][
					line.events.length === 1 ? 0 : 1
				]}{#each line.events as event, index (event.name)}{index ? ', ' : ' '}<EntityLink
						entity={event.page}>{event.name}</EntityLink
					>{/each}
			</p>{/each}
		{#if source.inputs.length || (!summarized && source.notes.length) || source.quotes?.length}
			<details class="row-details">
				<summary
					>Details <span class="sr-only">for <span data-game>{source.name}</span></span></summary
				>
				{#if source.inputs.length}
					<p class="detail-label">Required items</p>
					<ul>
						{#each source.inputs as input, index (index)}<li
								data-relation-id={input.relationId}
								data-relation-direction={input.relationId ? 'incoming' : undefined}
							>
								{formatNumber(input.quantity)} × <EntityLink entity={input.item} icon />
							</li>{/each}
					</ul>
				{/if}
				{#each source.quotes ?? [] as quote, index (index)}<p class="quote">
						{quote.player ? 'You say' : 'They say'}
						"<span data-game>{quote.text}</span>"
					</p>{/each}
				{#if !summarized && source.notes.length}<ul>
						{#each source.notes as note, index (index)}<li>{note}</li>{/each}
					</ul>{/if}
			</details>
		{/if}
	</td>
	{#if levelCondition}<td class="numeric"
			>{#if level?.value == null}<span class="muted">Unknown</span
				>{:else if level.entity && level.entity.key !== source.entity?.key}<EntityLink
					entity={level.entity}
				/>{` level ${formatNumber(level.value)}`}{:else}{formatNumber(level.value)}{/if}</td
		>{/if}
	{#if showAmount}<td class="numeric"
			>{source.amount ? formatAmount(source.amount) : 'Unknown'}<small
				>{source.amount?.max === 1 ? 'item' : 'items'}</small
			></td
		>{/if}
	{#if showSpawn}
		<td class="numeric spawn-value">
			{#if !source.spawn || (source.spawn.min === 1 && source.spawn.max === 1)}Always{:else}<Chance
					min={source.spawn.min}
					max={source.spawn.max}
					id={id + '-spawn'}
				/>{/if}
		</td>
	{/if}
	{#if showContext}
		<td class="numeric context-value">
			{#if source.price && payPrice(source.price) === 0}
				Free
			{:else if source.price && !source.price.currency}
				{`${formatNumber(payPrice(source.price))} gold`}
			{:else if source.price}
				<!-- The amount and its currency read as one phrase, wrapping only inside the currency's name. -->
				<span class="currency-price"
					>{formatNumber(payPrice(source.price))} ×
					<span
						data-relation-id={source.price.relationId}
						data-relation-direction={source.price.relationId ? 'incoming' : undefined}
						><EntityLink entity={source.price.currency!} icon /></span
					></span
				>
			{:else if source.odds?.chance != null}
				{#if alwaysThere(source.odds, source.kind)}Always{:else}<Chance
						min={source.odds.chance}
						max={source.odds.highestChance ?? source.odds.chance}
						id={id + '-odds'}
					/>{#if showUnit}<small>{oddsLabel(source.odds, source.kind)}</small>{/if}{/if}
			{:else if source.odds?.unknown}<Hint
					text={source.kind === 'randomLoot' ? 'Varies' : 'Unknown'}
					hint={source.odds.unknown}
					id={id + '-odds'}
					soft
				/>{:else}<span class="muted">Unknown</span>{/if}
		</td>
	{/if}
</tr>

<style>
	.currency-price {
		white-space: normal;
	}
</style>
