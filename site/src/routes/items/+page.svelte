<script lang="ts">
	import { resolve } from '$app/paths';
	import { browser } from '$app/env';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { SvelteURL } from 'svelte/reactivity';
	import { filterItems, matchesItem, readFilters } from '#lib/items.js';
	import { sourceNames, titleCase } from '#lib/labels.js';
	import type { SourceKind } from '#lib/site-data.js';
	import { formatNumber } from '#lib/format.js';
	import SeoHead from '#lib/SeoHead.svelte';
	let { data } = $props();
	let dialog: HTMLDialogElement;
	let categorySearch = $state('');
	let typeSearch = $state('');
	let sourceSearch = $state('');
	const currentUrl = $derived(page.shallow?.url ?? page.url);
	const filters = $derived(readFilters(browser ? currentUrl.searchParams : undefined));
	const items = $derived(filterItems(data.index.items, filters));
	const unknownCount = $derived(data.index.items.filter((item) => !item.sourceKinds.length).length);
	const activeCount = $derived(
		filters.categories.length +
			filters.types.length +
			filters.sources.length +
			Number(Boolean(filters.name)) +
			Number(filters.showUnknown)
	);
	const categories = $derived(
		data.index.categories.map((category) => ({
			value: category.slug,
			label: category.plural,
			count: data.index.items.filter(
				(item) =>
					item.category === category.slug && matchesItem(item, { ...filters, categories: [] })
			).length
		}))
	);
	const types = $derived(
		[...new Set(data.index.items.map((item) => item.type))]
			.sort((a, b) => a.localeCompare(b, 'en'))
			.map((type) => ({
				value: type,
				label: titleCase(type),
				count: data.index.items.filter(
					(item) => item.type === type && matchesItem(item, { ...filters, types: [] })
				).length
			}))
	);
	const sources = $derived(
		(Object.keys(sourceNames) as SourceKind[])
			.map((kind) => ({
				value: kind,
				label: sourceNames[kind],
				count: data.index.items.filter(
					(item) =>
						item.sourceKinds.includes(kind) && matchesItem(item, { ...filters, sources: [] })
				).length
			}))
			.filter((entry) => entry.count > 0 || filters.sources.includes(entry.value))
			.sort((a, b) => a.label.localeCompare(b.label, 'en'))
	);
	function update(key: string, values: string[]) {
		const url = new SvelteURL(currentUrl.href);
		url.searchParams.delete(key);
		for (const value of values) if (value) url.searchParams.append(key, value);
		goto(resolve(`items/${url.search}${url.hash}`), { shallow: true });
	}
	function toggle(key: 'category' | 'type' | 'source', value: string, checked: boolean) {
		const values = currentUrl.searchParams.getAll(key).filter((entry) => entry !== value);
		update(key, checked ? [...values, value] : values);
	}
	function sort(column: string) {
		const next =
			filters.sort === `${column}-asc`
				? `${column}-desc`
				: filters.sort === `${column}-desc`
					? `${column}-asc`
					: `${column}-${column === 'value' ? 'desc' : 'asc'}`;
		update('sort', next === 'name-asc' ? [] : [next]);
	}
	function sortDirection(column: string): 'ascending' | 'descending' | 'none' {
		return filters.sort.startsWith(column + '-')
			? filters.sort.endsWith('asc')
				? 'ascending'
				: 'descending'
			: 'none';
	}
</script>

<SeoHead
	title="Items | The Bloodline Compendium"
	description="Find The Bloodline items by name, category and type. Compare values and open known shops, recipes, drops and gathering sources."
/>
<h1>Items</h1>
<p class="intro">Find equipment, ingredients and curios, then see how to get them.</p>
<div class="list-controls">
	<label
		>Search by name<input
			type="search"
			value={filters.name}
			oninput={(event) => update('name', [event.currentTarget.value])}
			placeholder="Item name"
		/></label
	>
	<button class="mobile-filters" type="button" onclick={() => dialog.showModal()}
		>Filters{activeCount ? ` (${activeCount})` : ''}</button
	>
</div>
<div class="browse-layout">
	<aside class="desktop-filters" aria-label="Item filters">{@render facets()}</aside>
	<dialog bind:this={dialog} class="filter-sheet" aria-labelledby="filter-heading">
		<div class="sheet-heading">
			<h2 id="filter-heading">Filters</h2>
			<button type="button" onclick={() => dialog.close()} aria-label="Close filters">Close</button>
		</div>
		{@render facets()}
		<button class="button result-button" type="button" onclick={() => dialog.close()}
			>Show {items.length} {items.length === 1 ? 'item' : 'items'}</button
		>
	</dialog>
	<div class="list-results">
		<p role="status" aria-live="polite">
			{formatNumber(items.length)} of {formatNumber(data.index.items.length)} items
		</p>
		{#if unknownCount}<label class="check-label availability"
				><input
					type="checkbox"
					checked={filters.showUnknown}
					onchange={(event) => update('unknown', event.currentTarget.checked ? ['1'] : [])}
				/>Show {formatNumber(unknownCount)} items without a known source</label
			>
			<p class="filter-hint">
				No known source does not mean unobtainable. <a href={resolve('coverage/#itemWithoutSource')}
					>Coverage</a
				>
			</p>{/if}
		{#if activeCount}<div class="filter-chips" aria-label="Active filters">
				{#if filters.name}<button type="button" onclick={() => update('name', [])}
						>Name: {filters.name} <span aria-hidden="true">×</span><span class="sr-only"
							>Remove name filter</span
						></button
					>{/if}
				{#each filters.categories as value (value)}<button
						type="button"
						onclick={() => toggle('category', value, false)}
						>{categories.find((category) => category.value === value)?.label ?? titleCase(value)}
						<span aria-hidden="true">×</span><span class="sr-only">Remove category filter</span
						></button
					>{/each}
				{#each filters.sources as value (value)}<button
						type="button"
						onclick={() => toggle('source', value, false)}
						>{sourceNames[value as SourceKind] ?? titleCase(value)}
						<span aria-hidden="true">×</span><span class="sr-only">Remove source filter</span
						></button
					>{/each}
				{#each filters.types as value (value)}<button
						type="button"
						onclick={() => toggle('type', value, false)}
						>{titleCase(value)} <span aria-hidden="true">×</span><span class="sr-only"
							>Remove type filter</span
						></button
					>{/each}
				<button
					type="button"
					onclick={() => {
						const url = new SvelteURL(currentUrl.href);
						url.search = '';
						goto(resolve(`items/${url.search}${url.hash}`), { shallow: true });
					}}>Clear all</button
				>
			</div>{/if}
		<table class="item-table">
			<thead
				><tr
					>{#each ['name', 'type', 'value'] as column (column)}<th
							scope="col"
							class:numeric={column === 'value'}
							aria-sort={sortDirection(column)}
							><button type="button" onclick={() => sort(column)}
								>{column === 'name'
									? 'Item'
									: titleCase(column)}{#if filters.sort.startsWith(column + '-')}<span
										class="sort-indicator"
										class:descending={filters.sort.endsWith('-desc')}
										aria-hidden="true"
									></span>{/if}</button
							></th
						>{/each}</tr
				></thead
			>
			<tbody
				>{#each items as item (item.slug)}<tr>
						<td
							><a class="item-name" href={resolve(`items/${item.slug}/`)}
								>{#if item.icon}<img
										src={'/icons/' + item.icon + '-48.webp'}
										alt=""
										width="40"
										height="40"
										loading="lazy"
									/>{/if}<span
									>{item.name}{#if item.qualifier}<small>{item.qualifier}</small>{/if}</span
								></a
							></td
						>
						<td class="item-type">{titleCase(item.type)}</td><td class="numeric"
							>{item.value === null ? 'Unknown' : formatNumber(item.value) + ' gold'}</td
						>
					</tr>{/each}</tbody
			>
		</table>
		{#if !items.length}<p class="empty-state">
				No items match these filters. Change a filter or clear all to browse again.
			</p>{/if}
		<noscript
			><p>
				Name filtering, facets and sorting need JavaScript. Follow an item link to read its complete
				page.
			</p></noscript
		>
	</div>
</div>

{#snippet facets()}
	<h2 class="desktop-filter-title">Filters</h2>
	{#each [{ key: 'category' as const, title: 'Category', entries: categories, selected: filters.categories, query: categorySearch }, { key: 'source' as const, title: 'How to Get', entries: sources, selected: filters.sources, query: sourceSearch }, { key: 'type' as const, title: 'Type', entries: types, selected: filters.types, query: typeSearch }] as facet (facet.key)}
		<details class="facet" open>
			<summary>{facet.title}{facet.selected.length ? ` (${facet.selected.length})` : ''}</summary>
			{#if facet.entries.length > 10}<label
					>Find {facet.title.toLowerCase()}<input
						type="search"
						value={facet.query}
						oninput={(event) => {
							if (facet.key === 'category') categorySearch = event.currentTarget.value;
							else if (facet.key === 'source') sourceSearch = event.currentTarget.value;
							else typeSearch = event.currentTarget.value;
						}}
					/></label
				>{/if}
			<div class="facet-values">
				{#each facet.entries.filter((entry) => entry.label
						.toLowerCase()
						.includes(facet.query.toLowerCase())) as entry (entry.value)}<label class="check-label"
						><input
							type="checkbox"
							checked={facet.selected.includes(entry.value)}
							onchange={(event) => toggle(facet.key, entry.value, event.currentTarget.checked)}
						/><span>{entry.label}</span><span class="facet-count">{formatNumber(entry.count)}</span
						></label
					>{/each}
			</div>
		</details>
	{/each}
{/snippet}
