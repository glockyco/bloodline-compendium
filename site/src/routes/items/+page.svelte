<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import {
		facetGroups,
		hiddenAvailability,
		matches,
		naturalDirection,
		readState,
		shownColumns,
		sortItems,
		writeState,
		type ListState
	} from '#lib/list.js';
	import { sourceNames } from '#lib/labels.js';
	import type { ItemSummary, ListFacet, SourceKind } from '#lib/site-data.js';
	import { formatNumber } from '#lib/format.js';
	import SeoHead from '#lib/SeoHead.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import { kindPath } from '#lib/entities.js';
	let { data } = $props();
	const index = $derived(data.index);
	let dialog: HTMLDialogElement;
	let narrowing = $state<Record<string, string>>({});
	const currentUrl = $derived(page.shallow?.url ?? page.url);
	// The prerendered list has no address query, so hydration starts from the defaults and the address applies after
	// mount. Hydrating straight into a filtered list keeps the prerendered rows' image sources, which Svelte does not
	// repair, so rows would show other items' icons.
	let mounted = $state(false);
	onMount(() => {
		mounted = true;
	});
	const list = $derived(
		readState(mounted ? currentUrl.searchParams : new URLSearchParams(), index)
	);
	const items = $derived(
		sortItems(
			index.items.filter((item) => matches(item, list, index)),
			list.sort
		)
	);
	// Items a reader would see by showing those without a known way to get them.
	const hiddenCount = $derived(
		list.reveal || list.name || list.facets.availability?.length
			? 0
			: index.items.filter(
					(item) =>
						hiddenAvailability.includes(item.status) &&
						matches(item, { ...list, reveal: true }, index)
				).length
	);
	const groups = $derived(facetGroups(index.items, items, list, index, valueLabel));
	const columns = $derived(shownColumns(items, list, index));
	const numericColumns = $derived(index.columns.filter((column) => column.numeric));
	const sortColumns = $derived([
		{ id: 'name', label: 'Item', numeric: false },
		{ id: 'type', label: 'Type', numeric: false },
		...columns,
		{ id: 'value', label: 'Value', numeric: true }
	]);
	const activeCount = $derived(
		Object.values(list.facets).reduce((sum, values) => sum + values.length, 0) +
			list.stats.length +
			Number(Boolean(list.name))
	);
	// The first rows render at once and the rest follow in small batches, so a long list never blocks a click.
	let built = $state(60);
	$effect(() => {
		const total = items.length;
		let count = 60;
		built = count;
		let timer: ReturnType<typeof setTimeout> | undefined;
		const grow = () => {
			count = Math.min(total, count + 200);
			built = count;
			if (count < total) timer = setTimeout(grow, 16);
		};
		if (total > count) timer = setTimeout(grow, 16);
		return () => clearTimeout(timer);
	});
	const rows = $derived(items.slice(0, built));

	function valueLabel(facet: ListFacet, value: string): string {
		return (
			facet.labels[value] ?? (facet.id === 'source' ? sourceNames[value as SourceKind] : value)
		);
	}
	function apply(next: ListState) {
		const search = writeState(next, index).toString();
		goto(kindPath('item', data.kinds, (search ? '?' + search : '') + currentUrl.hash)!, {
			shallow: true
		});
	}
	function toggle(facet: string, value: string, checked: boolean) {
		const values = (list.facets[facet] ?? []).filter((entry) => entry !== value);
		apply({
			...list,
			facets: { ...list.facets, [facet]: checked ? [...values, value] : values }
		});
	}
	function sort(column: string) {
		const direction =
			list.sort.column === column
				? list.sort.direction === 'asc'
					? 'desc'
					: 'asc'
				: naturalDirection(column, index);
		apply({ ...list, sort: { column, direction } });
	}
	function ariaSort(column: string): 'ascending' | 'descending' | 'none' {
		return list.sort.column === column
			? list.sort.direction === 'asc'
				? 'ascending'
				: 'descending'
			: 'none';
	}
	function cellText(item: ItemSummary, column: string): string {
		const cell = item.cells[column];
		if (!cell) return '';
		return cell.number != null ? formatNumber(cell.number) : (cell.text ?? '');
	}
	const statusNote: Record<string, string> = {
		noKnownSource: 'No known way to get it',
		notInNormalPlay: 'Not in normal play'
	};
</script>

<SeoHead
	title="Items | The Bloodline Compendium"
	description="Find The Bloodline items by name, category, type and stats. Compare values and open known shops, recipes, drops and gathering sources."
/>
<h1>Items</h1>
<p class="intro">Find equipment, ingredients and curios, then see how to get them.</p>
<div class="list-controls">
	<label
		>Search by name<input
			type="search"
			value={list.name}
			oninput={(event) => apply({ ...list, name: event.currentTarget.value })}
			placeholder="Item name"
		/></label
	>
	<button class="mobile-filters" type="button" onclick={() => dialog.showModal()}
		>Filters{activeCount ? ` (${activeCount})` : ''}</button
	>
</div>
<div class="browse-layout">
	<aside class="desktop-filters" aria-label="Item filters">{@render filters('side')}</aside>
	<dialog bind:this={dialog} class="filter-sheet" aria-labelledby="filter-heading">
		<div class="sheet-heading">
			<h2 id="filter-heading">Filters</h2>
			<button type="button" onclick={() => dialog.close()} aria-label="Close filters">Close</button>
		</div>
		{@render filters('sheet')}
		<button class="button result-button" type="button" onclick={() => dialog.close()}
			>Show {formatNumber(items.length)} {items.length === 1 ? 'item' : 'items'}</button
		>
	</dialog>
	<div class="list-results">
		<p role="status" aria-live="polite">
			{formatNumber(items.length)} of {formatNumber(index.items.length)} items
		</p>
		{#if hiddenCount}<p class="availability">
				<button type="button" class="link-button" onclick={() => apply({ ...list, reveal: true })}
					>Show {formatNumber(hiddenCount)} more without a known way to get them</button
				>
			</p>{:else if list.reveal}<p class="availability">
				<button type="button" class="link-button" onclick={() => apply({ ...list, reveal: false })}
					>Hide items without a known way to get them</button
				>
			</p>{/if}
		{#if activeCount}<div class="filter-chips" aria-label="Active filters">
				{#if list.name}<button type="button" onclick={() => apply({ ...list, name: '' })}
						>Name: {list.name} <span aria-hidden="true">×</span><span class="sr-only"
							>Remove name filter</span
						></button
					>{/if}
				{#each index.facets as facet (facet.id)}{#each list.facets[facet.id] ?? [] as value (value)}<button
							type="button"
							onclick={() => toggle(facet.id, value, false)}
							>{valueLabel(facet, value)} <span aria-hidden="true">×</span><span class="sr-only"
								>Remove {facet.label} filter</span
							></button
						>{/each}{/each}
				{#each list.stats as filter, position (filter.column)}<button
						type="button"
						onclick={() =>
							apply({ ...list, stats: list.stats.filter((_, other) => other !== position) })}
						>{index.columns.find((column) => column.id === filter.column)?.label}{filter.min
							? ` from ${filter.min}`
							: ''}{filter.max ? ` to ${filter.max}` : ''}
						<span aria-hidden="true">×</span><span class="sr-only">Remove stat filter</span></button
					>{/each}
				<button
					type="button"
					onclick={() =>
						apply({
							name: '',
							facets: {},
							stats: [],
							match: 'all',
							reveal: false,
							sort: list.sort
						})}>Clear all</button
				>
			</div>{/if}
		<label class="phone-sort"
			>Sort by <select
				value={`${list.sort.column}:${list.sort.direction}`}
				onchange={(event) => {
					const [column, direction] = event.currentTarget.value.split(':');
					apply({ ...list, sort: { column, direction: direction === 'desc' ? 'desc' : 'asc' } });
				}}
				>{#each sortColumns as column (column.id)}{#each column.numeric ? ['desc', 'asc'] : ['asc', 'desc'] as direction (direction)}<option
							value={`${column.id}:${direction}`}
							>{column.label}, {column.numeric
								? direction === 'desc'
									? 'highest first'
									: 'lowest first'
								: direction === 'asc'
									? 'A to Z'
									: 'Z to A'}</option
						>{/each}{/each}</select
			></label
		>
		<div class="table-scroll">
			<table class="item-table">
				<thead
					><tr
						>{#each sortColumns as column (column.id)}<th
								scope="col"
								class:numeric={column.numeric}
								aria-sort={ariaSort(column.id)}
								><button type="button" onclick={() => sort(column.id)}
									>{column.label}{#if list.sort.column === column.id}<span
											class="sort-indicator"
											class:descending={list.sort.direction === 'desc'}
											aria-hidden="true"
										></span>{/if}</button
								></th
							>{/each}</tr
					></thead
				>
				<tbody
					>{#each rows as item (item.slug)}<tr>
							<td
								><EntityLink class="item-name" entity={item}
									>{#if item.icon}<img
											src={'/icons/' + item.icon + '-48.webp'}
											alt=""
											width="40"
											height="40"
											loading="lazy"
										/>{/if}<span
										><span data-game>{item.name}</span>{#if item.qualifier}<small
												>{item.qualifier}</small
											>{/if}</span
									></EntityLink
								>{#if statusNote[item.status]}<small class="status-note"
										>{statusNote[item.status]}</small
									>{/if}</td
							>
							<td class="item-type" data-label="Type"><span data-game>{item.type}</span></td>
							{#each columns as column (column.id)}<td
									class:numeric={column.numeric}
									data-label={cellText(item, column.id) ? column.label : undefined}
									>{cellText(item, column.id)}</td
								>{/each}
							<td class="numeric" data-label="Value"
								>{item.value === null ? 'Unknown' : formatNumber(item.value) + ' gold'}</td
							>
						</tr>{/each}</tbody
				>
			</table>
		</div>
		{#if !items.length}<p class="empty-state">
				No items match these filters. Change a filter or clear all to browse again.
			</p>{/if}
		<noscript
			><p>
				Name filtering, filters and sorting need JavaScript. Follow an item link to read its
				complete page.
			</p></noscript
		>
	</div>
</div>

{#snippet filters(instance: string)}
	<h2 class="desktop-filter-title">Filters</h2>
	{#each groups as group (group.facet.id)}
		{@const selected = list.facets[group.facet.id] ?? []}
		{@const needle = (narrowing[group.facet.id] ?? '').toLocaleLowerCase('en')}
		<details class="facet" open>
			<summary
				>{group.facet.label}{#if selected.length}<span class="picked">{selected.length}</span
					>{/if}</summary
			>
			{#if group.options.length > 10}<label
					>Find {group.facet.label.toLowerCase()}<input
						type="search"
						value={narrowing[group.facet.id] ?? ''}
						oninput={(event) =>
							(narrowing = { ...narrowing, [group.facet.id]: event.currentTarget.value })}
					/></label
				>{/if}
			<div class="facet-values" class:long={group.options.length > 10}>
				{#each group.options.filter((option) => option.label
						.toLocaleLowerCase('en')
						.includes(needle)) as option (option.value)}<label class="check-label"
						><input
							type="checkbox"
							checked={selected.includes(option.value)}
							onchange={(event) =>
								toggle(group.facet.id, option.value, event.currentTarget.checked)}
						/><span>{option.label}</span><span class="facet-count"
							>{formatNumber(option.count)}</span
						></label
					>{/each}
			</div>
		</details>
	{/each}
	{#if numericColumns.length}
		<details class="facet" open={list.stats.length > 0}>
			<summary
				>Stats{#if list.stats.length}<span class="picked">{list.stats.length}</span>{/if}</summary
			>
			{#each list.stats as filter, position (filter.column)}
				{@const label = index.columns.find((column) => column.id === filter.column)?.label}
				<div class="stat-filter">
					<span>{label}</span>
					<button
						type="button"
						class="remove"
						aria-label={`Remove the ${label} filter`}
						onclick={() =>
							apply({ ...list, stats: list.stats.filter((_, other) => other !== position) })}
						>×</button
					>
					<div class="bounds">
						<input
							type="number"
							inputmode="decimal"
							placeholder="From"
							aria-label={`Lowest ${label}`}
							value={filter.min}
							onchange={(event) =>
								apply({
									...list,
									stats: list.stats.map((entry, other) =>
										other === position ? { ...entry, min: event.currentTarget.value } : entry
									)
								})}
						/>
						<input
							type="number"
							inputmode="decimal"
							placeholder="To"
							aria-label={`Highest ${label}`}
							value={filter.max}
							onchange={(event) =>
								apply({
									...list,
									stats: list.stats.map((entry, other) =>
										other === position ? { ...entry, max: event.currentTarget.value } : entry
									)
								})}
						/>
					</div>
				</div>
			{/each}
			{#if list.stats.length > 1}<label class="match"
					>Show items with <select
						id={`${instance}-match`}
						value={list.match}
						onchange={(event) =>
							apply({ ...list, match: event.currentTarget.value === 'any' ? 'any' : 'all' })}
						><option value="all">every stat</option><option value="any">any stat</option></select
					></label
				>{/if}
			{#if numericColumns.some((column) => !list.stats.some((filter) => filter.column === column.id))}<select
					id={`${instance}-add-stat`}
					aria-label="Add a stat filter"
					value=""
					onchange={(event) => {
						const column = event.currentTarget.value;
						event.currentTarget.value = '';
						if (column)
							apply({
								...list,
								stats: [...list.stats, { column, min: '', max: '' }],
								sort: { column, direction: 'desc' }
							});
					}}
					><option value="">Add a stat</option
					>{#each numericColumns.filter((column) => !list.stats.some((filter) => filter.column === column.id)) as column (column.id)}<option
							value={column.id}
							>{column.label}{column.categories.length === 1
								? ` (${index.categories.find((category) => category.slug === column.categories[0])?.plural ?? column.categories[0]})`
								: ''}</option
						>{/each}</select
				>{/if}
		</details>
	{/if}
{/snippet}
