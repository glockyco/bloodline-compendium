<script lang="ts">
	import { resolve } from '$app/paths';
	import ExpandableList from '#lib/ExpandableList.svelte';
	import SourceRow from '#lib/SourceRow.svelte';
	import { sourceHeadings } from '#lib/labels.js';
	import type { SourceKind } from '#lib/site-data.js';
	import SeoHead from '#lib/SeoHead.svelte';
	let { data } = $props();
	const item = $derived(data.item);
	const description = $derived(
		item.description?.trim() ||
			[
				item.name + ' (' + item.category.name + ').',
				...item.facts
					.filter((fact) => fact.value !== null)
					.map((fact) => fact.label + ': ' + fact.value + '.')
			].join(' ')
	);
	const sourceGroups = $derived(
		(Object.keys(sourceHeadings) as SourceKind[])
			.map((kind) => ({ kind, sources: item.sources.filter((source) => source.kind === kind) }))
			.filter((group) => group.sources.length)
	);
</script>

<SeoHead title={item.name + ' | The Bloodline Compendium'} {description} />
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
	<!-- Search finds items by name and description, so facts and sources stay out of the index. -->
	<section aria-labelledby="facts" data-pagefind-ignore="all">
		<h2 id="facts">Facts</h2>
		<dl class="facts panel">
			{#each item.facts as fact (fact.label)}<div>
					<dt>{fact.label}</dt>
					<dd class:unknown={fact.value === null}>{fact.value ?? 'Unknown'}</dd>
				</div>{/each}
		</dl>
	</section>
	<section aria-labelledby="sources" data-pagefind-ignore="all">
		<h2 id="sources">Known sources <span class="count">{item.sources.length}</span></h2>
		{#if !item.sources.length}<aside class="notice">
				<p>No source is known for this item. It may still be obtainable in the game.</p>
				<a href={resolve('coverage/#itemWithoutSource')}>Read about gaps in coverage</a>
			</aside>{:else}<p class="muted">
				Sources describe what the data says, not a guarantee that a place is reachable.
			</p>
			<div class="source-groups">
				{#each sourceGroups as group (group.kind)}
					<section class="source-group" aria-labelledby={'source-' + group.kind}>
						<h3 id={'source-' + group.kind}>
							{sourceHeadings[group.kind]} <span class="count">{group.sources.length}</span>
						</h3>
						<ExpandableList
							items={group.sources}
							initial={3}
							noun="sources"
							className="source-list"
						>
							{#snippet row(source)}<SourceRow {source} />{/snippet}
						</ExpandableList>
					</section>
				{/each}
			</div>{/if}
	</section>
</article>
