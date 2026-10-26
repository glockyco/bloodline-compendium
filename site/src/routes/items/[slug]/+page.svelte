<script lang="ts">
	import { resolve } from '$app/paths';
	import ItemLink from '#lib/ItemLink.svelte';
	import { sourceLabels } from '#lib/labels.js';
	let { data } = $props();
	const item = $derived(data.item);
</script>

<svelte:head
	><title>{item.name} | The Bloodline Compendium</title><meta
		name="description"
		content={item.description ?? 'Facts and known sources for ' + item.name + '.'}
	/></svelte:head
>
<a class="back" href={resolve('items/')}>All items</a>
<article data-pagefind-body>
	<div class="item-heading">
		{#if item.icon}<img
				class="large-icon"
				src={'/icons/' + item.icon + '-160.webp'}
				width="160"
				height="160"
				alt={item.name}
			/>{:else}<div class="large-icon missing-icon">No icon known</div>{/if}
		<div>
			<a class="eyebrow" href={resolve(`items/?category=${item.category.slug}`)}
				>{item.category.name}</a
			>
			<h1 data-pagefind-meta="title">{item.name}</h1>
			<p class="intro">{item.description ?? 'Description unknown.'}</p>
		</div>
	</div>
	<section aria-labelledby="facts">
		<h2 id="facts">Facts</h2>
		<dl class="facts panel">
			{#each item.facts as fact (fact.label)}<div>
					<dt>{fact.label}</dt>
					<dd class:unknown={fact.value === null}>{fact.value ?? 'Unknown'}</dd>
				</div>{/each}
		</dl>
	</section>
	<section aria-labelledby="sources">
		<h2 id="sources">Known sources <span class="count">{item.sources.length}</span></h2>
		{#if !item.sources.length}<aside class="notice">
				<p>No source is known for this item. It may still be obtainable in the game.</p>
				<a href={resolve('coverage/#itemWithoutSource')}>Read about gaps in coverage</a>
			</aside>{:else}<p class="muted">
				Sources describe what the data says, not a guarantee that a place is reachable.
			</p>
			<div class="source-grid">
				{#each item.sources as source, index (index)}<section class="panel source">
						<p class="eyebrow">{sourceLabels[source.kind]}</p>
						<h3>{source.name}</h3>
						<dl class="source-facts">
							<div>
								<dt>Places</dt>
								<dd>
									{source.places.length ? source.places.map((p) => p.name).join(', ') : 'Unknown'}
								</dd>
							</div>
							{#if source.price}<div>
									<dt>Price</dt>
									<dd>
										{source.price.amount}
										{#if source.price.currency}<ItemLink
												item={source.price.currency}
											/>{:else}money{/if}
									</dd>
								</div>{/if}{#if source.amount}<div>
									<dt>Amount</dt>
									<dd>
										{source.amount.min === source.amount.max
											? source.amount.min
											: source.amount.min + ' to ' + source.amount.max}
									</dd>
								</div>{/if}{#if source.weight}<div>
									<dt>Raw weight</dt>
									<dd>{source.weight.value} of {source.weight.total}</dd>
								</div>{/if}{#if source.chance !== null}<div>
									<dt>Chance</dt>
									<dd>
										{(source.chance * 100).toLocaleString('en', { maximumFractionDigits: 4 })}%
									</dd>
								</div>{/if}
						</dl>
						{#if source.inputs.length}<h4>Consumed inputs</h4>
							<ul>
								{#each source.inputs as input, i (i)}<li>
										{input.quantity} × <ItemLink item={input.item} />
									</li>{/each}
							</ul>{/if}{#if source.notes.length}<h4>Notes</h4>
							<ul>
								{#each source.notes as note, i (i)}<li>{note}</li>{/each}
							</ul>{/if}
					</section>{/each}
			</div>{/if}
	</section>
</article>
