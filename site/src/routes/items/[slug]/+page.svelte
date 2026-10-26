<script lang="ts">
	import { resolve } from '$app/paths';
	import SourceRow from '#lib/SourceRow.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import ItemLink from '#lib/ItemLink.svelte';
	import ItemTooltip from '#lib/ItemTooltip.svelte';
	import { sourceHeadings, titleCase } from '#lib/labels.js';
	import { sortSources, sourceContext, useHeadings } from '#lib/acquisition.js';
	import { formatNumber } from '#lib/format.js';
	import { factValue } from '#lib/stats.js';
	import type { SourceKind } from '#lib/site-data.js';
	import SeoHead from '#lib/SeoHead.svelte';
	let { data } = $props();
	const item = $derived(data.item);
	const displayName = $derived(item.name + (item.qualifier ? ` (${item.qualifier})` : ''));
	const sourceGroups = $derived(
		(Object.keys(sourceHeadings) as SourceKind[])
			.map((kind) => {
				const sources = sortSources(item.sources.filter((source) => source.kind === kind));
				const showAmount = sources.some(
					(source) => source.amount && (source.amount.min !== 1 || source.amount.max !== 1)
				);
				const showContext = sources.some(
					(source) =>
						source.price ||
						source.weight ||
						source.picks ||
						(source.chance !== null && source.chanceBasis)
				);
				const contextHeading = sources.some((source) => source.price)
					? 'Price'
					: sources.some((source) => source.chance !== null && source.chanceBasis)
						? 'Chance'
						: sources.some((source) => source.weight)
							? 'Relative Chance'
							: 'Chance';
				return {
					kind,
					sources,
					showAmount,
					showContext,
					headings: [
						'Source',
						...(showAmount ? ['Quantity'] : []),
						...(showContext ? [contextHeading] : [])
					]
				};
			})
			.filter((group) => group.sources.length)
	);
	const uses = $derived(
		[...item.uses].sort(
			(a, b) => a.item.name.localeCompare(b.item.name, 'en') || a.name.localeCompare(b.name, 'en')
		)
	);
	const description = $derived(
		[
			`${displayName}: ${titleCase(item.type)} in The Bloodline.`,
			...item.facts
				.filter((fact) => factValue(fact) !== null)
				.slice(0, 2)
				.map((fact) => `${fact.label}: ${factValue(fact)}.`),
			item.sources.length
				? `${sourceHeadings[item.sources[0].kind]}: ${item.sources[0].name}${sourceContext(item.sources[0]) ? ` (${sourceContext(item.sources[0])})` : ''}.`
				: 'How to obtain it is unknown.'
		].join(' ')
	);
</script>

<SeoHead
	title={displayName + ' | The Bloodline Compendium'}
	{description}
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Items', path: '/items/' },
		{ name: displayName, path: `/items/${item.slug}/` }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li><a href={resolve('items/')}>Items</a></li>
		<li aria-current="page">{displayName}</li>
	</ol>
</nav>
<article data-pagefind-body>
	<div class="item-heading">
		{#if item.icon}<img
				class="title-icon"
				src={'/icons/' + item.icon + '-160.webp'}
				width="80"
				height="80"
				alt=""
			/>{/if}
		<div>
			<h1 data-pagefind-meta="title">{displayName}</h1>
			<p class="identity">
				<a href={resolve(`items/?category=${encodeURIComponent(item.category.slug)}`)}
					>{titleCase(item.category.name)}</a
				>
				· {titleCase(item.type)}
			</p>
		</div>
	</div>
	<div class="detail-layout">
		<section class="answer panel" aria-labelledby="how-to-get-it" data-pagefind-ignore="all">
			<h2 id="how-to-get-it">How to get it</h2>
			{#if !sourceGroups.length}
				<p>No known source. This item may still be obtainable in the game.</p>
				<a href={resolve('coverage/#itemWithoutSource')}>Read about gaps in coverage</a>
			{:else}
				<ul class="acquisition-routes">
					{#each sourceGroups as group (group.kind)}
						<li>
							<a href={'#source-' + group.kind}
								><span>{sourceHeadings[group.kind]}</span><strong
									>{group.sources
										.slice(0, 2)
										.map((source) => source.name)
										.join(', ')}{group.sources.length > 2
										? ` and ${group.sources.length - 2} more`
										: ''}</strong
								>{#if sourceContext(group.sources[0])}<small
										>{sourceContext(group.sources[0])}</small
									>{/if}</a
							>
						</li>
					{/each}
				</ul>
			{/if}
		</section>
		<aside class="item-side" aria-label="Item details">
			<ItemTooltip item={{ ...item, name: displayName }} />
			{#if !item.icon}<p class="muted">No icon is known for this item.</p>{/if}
		</aside>
		<div class="item-relations" data-pagefind-ignore="all">
			{#if sourceGroups.length}<p class="muted">
					Known sources do not guarantee that a location is reachable. <a
						href={resolve('coverage/')}>Coverage</a
					>
				</p>{/if}
			<div class="source-groups">
				{#each sourceGroups as group (group.kind)}
					<section class="source-group" aria-labelledby={'source-' + group.kind}>
						<h2 id={'source-' + group.kind}>
							{sourceHeadings[group.kind]} <span class="count">{group.sources.length}</span>
						</h2>
						<RelationTable
							items={group.sources}
							label={sourceHeadings[group.kind]}
							headings={group.headings}
							>{#snippet row(source, index)}<SourceRow
									{source}
									showAmount={group.showAmount}
									showContext={group.showContext}
									id={`source-${group.kind}-${index + 1}`}
								/>{/snippet}</RelationTable
						>
					</section>
				{/each}
			</div>
			{#if uses.length}<section aria-labelledby="used-for">
					<h2 id="used-for">Used for <span class="count">{uses.length}</span></h2>
					<RelationTable items={uses} label="Used for" headings={['Item', 'Required Quantity']}
						>{#snippet row(use, index)}<tr id={`use-${index + 1}`}
								><td
									><ItemLink item={use.item} /><small
										>{useHeadings[use.kind] ?? sourceHeadings[use.kind]} · {use.name}</small
									></td
								><td class="numeric">{formatNumber(use.quantity)}<small>items</small></td></tr
							>{/snippet}</RelationTable
					>
				</section>{/if}
		</div>
	</div>
</article>
