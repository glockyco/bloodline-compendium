<script lang="ts">
	import type { Source } from './site-data.js';
	import { formatNumber, formatChance } from './format.js';
	import ItemLink from './ItemLink.svelte';
	let { source }: { source: Source } = $props();
</script>

<div class="source-row">
	<div class="source-identity">
		<h4>{source.name}</h4>
		<p class="source-places">
			<span>Places:</span>
			{source.places.length ? source.places.map((place) => place.name).join(', ') : 'Unknown'}
		</p>
	</div>
	<div class="source-details">
		<dl class="source-facts">
			{#if source.price}<div>
					<dt>Price</dt>
					<dd>
						{formatNumber(source.price.amount)}
						{#if source.price.currency}<ItemLink item={source.price.currency} />{:else}gold{/if}
					</dd>
				</div>{/if}
			{#if source.amount}<div>
					<dt>Amount</dt>
					<dd>
						{source.amount.min === source.amount.max
							? formatNumber(source.amount.min)
							: formatNumber(source.amount.min) + ' to ' + formatNumber(source.amount.max)}
					</dd>
				</div>{/if}
			{#if source.weight}<div>
					<dt>Raw weight</dt>
					<dd>{formatNumber(source.weight.value)} of {formatNumber(source.weight.total)}</dd>
				</div>{/if}
			{#if source.chance !== null}<div>
					<dt>Chance</dt>
					<dd>{formatChance(source.chance)}</dd>
				</div>{/if}
		</dl>
		{#if source.inputs.length}<div class="source-inputs">
				<span class="detail-label">Consumed inputs:</span>
				<ul>
					{#each source.inputs as input, index (index)}<li>
							{formatNumber(input.quantity)} × <ItemLink item={input.item} />
						</li>{/each}
				</ul>
			</div>{/if}
		{#if source.notes.length}<div class="source-notes">
				<span class="detail-label">Notes:</span>
				<ul>
					{#each source.notes as note, index (index)}<li>{note}</li>{/each}
				</ul>
			</div>{/if}
	</div>
</div>
