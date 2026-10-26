<script lang="ts">
	import type { Source } from './site-data.js';
	import { formatNumber, formatChance } from './format.js';
	import { chanceLabel, formatAmount } from './acquisition.js';
	import EntityLink from './ui/EntityLink.svelte';
	let {
		source,
		showAmount = false,
		showContext = false,
		id
	}: { source: Source; showAmount?: boolean; showContext?: boolean; id: string } = $props();
</script>

<tr class="source-row" {id}>
	<td class="source-identity">
		<span class="relation-name" data-game>{source.name}</span>
		<p class="source-places">
			{#if source.places.length}<span data-game
					>{source.places.map((place) => place.name).join(', ')}</span
				>{:else}Location unknown{/if}
		</p>
		{#if source.weight && !(source.chance !== null && source.chanceBasis)}<p class="source-places">
				Compared with other possible items. The selection rule is unknown, so this is not a drop
				chance.
			</p>{/if}
		{#if source.inputs.length || source.notes.length || (source.chance !== null && !source.chanceBasis)}
			<details class="row-details">
				<summary
					>Details <span class="sr-only">for <span data-game>{source.name}</span></span></summary
				>
				{#if source.inputs.length}
					<p class="detail-label">Required items</p>
					<ul>
						{#each source.inputs as input, index (index)}<li>
								{formatNumber(input.quantity)} × <EntityLink entity={input.item} />
							</li>{/each}
					</ul>
				{/if}
				{#if source.chance !== null && !source.chanceBasis}<p>
						Chance: {formatChance(source.chance)}. What this chance applies to is unknown.
					</p>{/if}
				<!-- Notes quote dialogue lines from the game. The entity graph replaces them with typed conditions. -->
				{#if source.notes.length}<ul data-game>
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
			{#if source.price}
				{formatNumber(source.price.amount)}<small
					>{#if source.price.currency}<EntityLink
							entity={source.price.currency}
						/>{:else}gold{/if}</small
				>
			{:else if source.chance !== null && source.chanceBasis}
				{formatChance(source.chance)}<small>{chanceLabel(source)}</small>
			{:else if source.weight}
				{formatNumber(source.weight.value)} of {formatNumber(source.weight.total)}<small
					>Relative chance</small
				>
			{:else}<span class="muted">Unknown</span>{/if}
			{#if source.picks}<small
					>{formatAmount(source.picks)}
					{source.picks.min === 1 && source.picks.max === 1 ? 'pick' : 'picks'} per {source.kind ===
					'containerLoot'
						? 'container'
						: 'roll'}</small
				>{/if}
		</td>
	{/if}
</tr>
