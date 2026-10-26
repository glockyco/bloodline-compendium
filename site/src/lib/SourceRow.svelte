<script lang="ts">
	import type { Source } from './site-data.js';
	import { formatNumber, formatChance } from './format.js';
	import { formatAmount, oddsLabel, payPrice } from './acquisition.js';
	import EntityLink from './ui/EntityLink.svelte';
	import Conditions from './Conditions.svelte';
	import UnknownValue from './ui/UnknownValue.svelte';
	let {
		source,
		showAmount = false,
		showContext = false,
		showPlaces = true,
		id
	}: {
		source: Source;
		showAmount?: boolean;
		showContext?: boolean;
		showPlaces?: boolean;
		id: string;
	} = $props();
</script>

<tr class="source-row" {id} data-relation-id={source.relationId} data-relation-direction="incoming">
	<td class="source-identity">
		<span class="relation-name"><EntityLink entity={source.entity} /></span>
		<Conditions conditions={source.conditions} />
		{#if showPlaces}<p class="source-places">
				{#if source.places.length}{#each source.places as place, index (index)}{index
							? ', '
							: ''}<EntityLink entity={place} />{/each}{:else}Location unknown{/if}
			</p>{/if}
		{#if source.inputs.length || source.notes.length || source.quotes?.length}
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
								{formatNumber(input.quantity)} × <EntityLink entity={input.item} />
							</li>{/each}
					</ul>
				{/if}
				{#each source.quotes ?? [] as quote, index (index)}<p class="quote">
						{quote.player ? 'You say' : 'They say'}
						"<span data-game>{quote.text}</span>"
					</p>{/each}
				{#if source.notes.length}<ul>
						{#each source.notes as note, index (index)}<li>{note}</li>{/each}
					</ul>{/if}
			</details>
		{/if}
	</td>
	{#if showAmount}<td class="numeric"
			>{source.amount ? formatAmount(source.amount) : 'Unknown'}<small>items</small></td
		>{/if}
	{#if showContext}
		<td class="numeric context-value">
			{#if source.price && payPrice(source.price) === 0}
				Free
			{:else if source.price}
				{formatNumber(payPrice(source.price))}<small
					>{#if source.price.currency}<span
							data-relation-id={source.price.relationId}
							data-relation-direction={source.price.relationId ? 'incoming' : undefined}
							><EntityLink entity={source.price.currency} /></span
						>{:else}gold{/if}</small
				>
			{:else if source.odds?.chance != null}
				{formatChance(source.odds.chance)}<small>{oddsLabel(source.odds, source.kind)}</small>
			{:else if source.odds?.unknown}<UnknownValue
					reason={source.odds.unknown}
					id={id + '-odds'}
				/>{:else}<span class="muted">Unknown</span>{/if}
		</td>
	{/if}
</tr>
