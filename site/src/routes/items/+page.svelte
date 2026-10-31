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
	const selectedCategories = $derived(list.facets.category ?? []);
	// The stat picker offers the stats that items of the selected categories have, or every stat while none is
	// selected.
	const statColumns = $derived(
		index.columns.filter(
			(column) =>
				column.numeric &&
				!list.stats.some((filter) => filter.column === column.id) &&
				(!selectedCategories.length ||
					index.items.some(
						(item) => selectedCategories.includes(item.category) && item.cells[column.id]
					))
		)
	);
	// The type column adds nothing when every shown item's type only repeats its category, as for food.
	const categoryNames = $derived(
		new Map(
			index.categories.map((category) => [category.slug, category.name.toLocaleLowerCase('en')])
		)
	);
	const showType = $derived(
		items.some((item) => item.type.toLocaleLowerCase('en') !== categoryNames.get(item.category))
	);
	const sortColumns = $derived([
		{ id: 'name', label: 'Item', numeric: false },
		...(showType ? [{ id: 'type', label: 'Type', numeric: false }] : []),
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
		// A cell's text is what it shows when it has one, such as a speed word that sorts by its number. A cell
		// without either is a fact the game does not tell, and a missing cell is a stat the item lacks.
		if (!cell) return '';
		if (cell.text != null) return cell.text;
		return cell.number != null ? formatNumber(cell.number) : 'Unknown';
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
<h1 class="list-title">Items</h1>
<div class="layout">
	<aside class="sidebar" aria-label="Item filters">
		<a class="skip-results" href="#results">Skip to results</a>
		{@render filters('side')}
	</aside>
	<dialog bind:this={dialog} class="sheet" aria-labelledby="filter-heading">
		<div class="sheet-head">
			<h2 id="filter-heading">Filters</h2>
			<button type="button" class="close" onclick={() => dialog.close()} aria-label="Close filters"
				>×</button
			>
		</div>
		<div class="sheet-body">{@render filters('sheet')}</div>
		<div class="sheet-foot">
			<span>{formatNumber(items.length)} {items.length === 1 ? 'item' : 'items'}</span>
			<button class="show" type="button" onclick={() => dialog.close()}
				>Show {formatNumber(items.length)} {items.length === 1 ? 'item' : 'items'}</button
			>
		</div>
	</dialog>
	<div class="results" id="results" tabindex="-1">
		<div class="search-row">
			<input
				type="search"
				aria-label="Search by name"
				value={list.name}
				oninput={(event) => apply({ ...list, name: event.currentTarget.value })}
				placeholder="Filter items by name"
			/>
			<button class="filters-button" type="button" onclick={() => dialog.showModal()}
				>Filters{activeCount ? ` (${activeCount})` : ''}</button
			>
		</div>
		<div class="bar">
			<p role="status" aria-live="polite">
				<strong>{formatNumber(items.length)}</strong> of {formatNumber(index.items.length)} items
			</p>
			{#if activeCount}<ul class="chips" aria-label="Active filters">
					{#if list.name}<li>
							<button type="button" class="chip" onclick={() => apply({ ...list, name: '' })}
								>Name: {list.name} <span aria-hidden="true">×</span><span class="sr-only"
									>Remove name filter</span
								></button
							>
						</li>{/if}
					{#each index.facets as facet (facet.id)}{#each list.facets[facet.id] ?? [] as value (value)}<li
							>
								<button type="button" class="chip" onclick={() => toggle(facet.id, value, false)}
									>{valueLabel(facet, value)} <span aria-hidden="true">×</span><span class="sr-only"
										>Remove {facet.label} filter</span
									></button
								>
							</li>{/each}{/each}
					{#each list.stats as filter, position (filter.column)}<li>
							<button
								type="button"
								class="chip"
								onclick={() =>
									apply({ ...list, stats: list.stats.filter((_, other) => other !== position) })}
								>{index.columns.find((column) => column.id === filter.column)?.label}{filter.min
									? ` from ${filter.min}`
									: ''}{filter.max ? ` to ${filter.max}` : ''}
								<span aria-hidden="true">×</span><span class="sr-only">Remove stat filter</span
								></button
							>
						</li>{/each}
					<li>
						<button
							type="button"
							class="clear"
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
					</li>
				</ul>{/if}
			<!-- One button that changes its text keeps keyboard focus when it is used. -->
			{#if hiddenCount || list.reveal}<button
					type="button"
					class="reveal"
					onclick={() => apply({ ...list, reveal: !list.reveal })}
					>{list.reveal
						? 'Hide items without a known way to get them'
						: `Show ${formatNumber(hiddenCount)} more without a known way to get them`}</button
				>{/if}
		</div>
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
		<div class="list">
			<div class="table-scroll">
				<table>
					<thead
						><tr
							>{#each sortColumns as column (column.id)}<th
									scope="col"
									class:numeric={column.numeric}
									aria-sort={ariaSort(column.id)}
									><button type="button" class="sort" onclick={() => sort(column.id)}
										>{column.label}<span
											class="sort-mark"
											class:idle={list.sort.column !== column.id}
											aria-hidden="true"
											>{list.sort.column === column.id && list.sort.direction === 'desc'
												? '▼'
												: '▲'}</span
										></button
									></th
								>{/each}</tr
						></thead
					>
					<tbody
						>{#each rows as item (item.slug)}<tr>
								<td class="name"
									><EntityLink class="item-name" entity={item}
										>{#if item.icon}<img
												src={'/icons/' + item.icon + '-48.webp'}
												alt=""
												width="32"
												height="32"
												loading="lazy"
											/>{/if}<span data-game
											>{item.name}{#if item.qualifier}<span class="qualifier"
													>{` (${item.qualifier})`}</span
												>{/if}</span
										></EntityLink
									>{#if statusNote[item.status]}<small class="status-note"
											>{statusNote[item.status]}</small
										>{/if}</td
								>
								{#if showType}<td class="type" data-label="Type"
										><span data-game>{item.type}</span></td
									>{/if}
								{#each columns as column (column.id)}<td
										class:numeric={column.numeric}
										class:short={cellText(item, column.id).length <= 16}
										data-label={cellText(item, column.id) ? column.label : undefined}
										>{cellText(item, column.id)}</td
									>{/each}
								<td class="numeric value" data-label="Value"
									>{item.value === null ? 'Unknown' : formatNumber(item.value) + ' gold'}</td
								>
							</tr>{/each}</tbody
					>
				</table>
			</div>
			{#if !items.length}<p class="empty">
					No items match these filters. Change a filter or clear all to browse again.
				</p>{/if}
		</div>
		<noscript
			><p>
				Name filtering, filters and sorting need JavaScript. Follow an item link to read its
				complete page.
			</p></noscript
		>
	</div>
</div>

{#snippet facet(group: (typeof groups)[number])}
	{@const selected = list.facets[group.facet.id] ?? []}
	{@const needle = (narrowing[group.facet.id] ?? '').toLocaleLowerCase('en')}
	<!-- Places are many, so their group starts closed unless it holds a choice. -->
	<details class="group" open={group.facet.id !== 'place' || selected.length > 0}>
		<summary
			><span class="title">{group.facet.label}</span>{#if selected.length}<span class="picked"
					>{selected.length}</span
				>{/if}<span class="chevron" aria-hidden="true"></span></summary
		>
		{#if group.options.length > 10}<input
				class="narrow"
				type="search"
				aria-label={`Find ${group.facet.label.toLowerCase()} values`}
				placeholder={`Find ${group.facet.label.toLowerCase()}`}
				value={narrowing[group.facet.id] ?? ''}
				oninput={(event) =>
					(narrowing = { ...narrowing, [group.facet.id]: event.currentTarget.value })}
			/>{/if}
		<ul class="options" class:long={group.options.length > 10}>
			{#each group.options.filter((option) => option.label
					.toLocaleLowerCase('en')
					.includes(needle)) as option (option.value)}<li>
					<label
						><input
							type="checkbox"
							checked={selected.includes(option.value)}
							onchange={(event) =>
								toggle(group.facet.id, option.value, event.currentTarget.checked)}
						/><span class="option-name">{option.label}</span><span class="count"
							>{formatNumber(option.count)}</span
						></label
					>
				</li>{/each}
		</ul>
	</details>
{/snippet}

{#snippet filters(instance: string)}
	<h2 class="sr-only">Filters</h2>
	<!-- What an item is comes first, then its stats, then how and where to get it. -->
	{#each groups.filter((group) => group.facet.id === 'category' || group.facet.categories.length) as group (group.facet.id)}{@render facet(
			group
		)}{/each}
	{#if statColumns.length || list.stats.length}
		<details class="group" open={list.stats.length > 0}>
			<summary
				><span class="title">Stats</span>{#if list.stats.length}<span class="picked"
						>{list.stats.length}</span
					>{/if}<span class="chevron" aria-hidden="true"></span></summary
			>
			{#each list.stats as filter, position (filter.column)}
				{@const label = index.columns.find((column) => column.id === filter.column)?.label}
				<div class="stat">
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
			{#if statColumns.length}<select
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
					><option value="">Add a stat</option>{#each statColumns as column (column.id)}<option
							value={column.id}
							>{column.label}{column.categories.length === 1 && !selectedCategories.length
								? ` (${index.categories.find((category) => category.slug === column.categories[0])?.plural ?? column.categories[0]})`
								: ''}</option
						>{/each}</select
				>{/if}
		</details>
	{/if}
	{#each groups.filter((group) => group.facet.id !== 'category' && !group.facet.categories.length) as group (group.facet.id)}{@render facet(
			group
		)}{/each}
{/snippet}

<style>
	/* The list follows Afallon's: filters beside the results, a name search above them, the count, chips and the
	   reveal on one line, and a compact striped table in one surface. */
	.list-title {
		margin-bottom: var(--gap-groups);
	}
	.layout {
		display: grid;
		grid-template-columns: var(--size-filters) minmax(0, 1fr);
		gap: var(--gap-section);
		align-items: start;
	}
	.sidebar {
		display: grid;
	}
	.skip-results {
		justify-self: start;
	}
	.skip-results:not(:focus) {
		clip-path: inset(50%);
		position: absolute;
	}
	.results {
		display: grid;
		gap: var(--gap-block);
		min-width: 0;
	}
	/* The name field and the filter button share one height, and the button never wraps its label. */
	.search-row {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
	}
	.bar {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--gap-inline) var(--gap-block);
	}
	.bar p {
		color: var(--text-soft);
	}
	.chips {
		display: contents;
		list-style: none;
	}
	.chip,
	.clear,
	.reveal {
		min-height: var(--size-chip);
		padding: var(--inset-line) var(--inset-cell);
		font-size: var(--type-0);
	}
	.chip {
		border-color: var(--accent);
		border-radius: var(--radius-1);
		background: transparent;
	}
	.chip span {
		color: var(--text-muted);
	}
	.clear,
	.reveal {
		border: 0;
		background: none;
		text-decoration: underline;
		text-underline-offset: var(--underline-offset);
	}
	.clear {
		color: var(--accent);
	}
	/* Hidden entries matter to few readers, so the reveal is a quiet link at the end of the bar. */
	.reveal {
		margin-left: auto;
		color: var(--text-muted);
		text-decoration-style: dotted;
	}
	/* One surface whose cells carry the only inset, so text lines up with the surfaces on item pages. As a grid
	   item it would grow to its content, so it keeps a zero minimum width and its table scrolls inside it. */
	.list {
		min-width: 0;
		overflow: hidden;
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
	}
	.table-scroll {
		overflow-x: auto;
	}
	table {
		width: 100%;
		border-collapse: collapse;
	}
	th,
	td {
		padding: var(--inset-cell) var(--inset);
		line-height: var(--leading-data);
		text-align: left;
		vertical-align: middle;
		border: 0;
	}
	thead th {
		position: sticky;
		top: 0;
		z-index: var(--layer-table-heading);
		background: var(--surface-panel);
		border-bottom: var(--border-width) solid var(--border);
		color: var(--text-muted);
		font-size: var(--type-0);
		font-weight: var(--weight-strong);
		white-space: nowrap;
	}
	tbody td {
		border-top: var(--border-width) solid var(--border);
	}
	tbody tr:nth-child(even) {
		background: var(--surface-stripe);
	}
	tbody tr:hover {
		background: var(--surface-hover);
	}
	.numeric {
		text-align: right;
		font-variant-numeric: tabular-nums;
		white-space: nowrap;
	}
	/* A name keeps room for a few words before other columns take the width. */
	td.name {
		min-width: var(--size-name-column);
	}
	/* Types and short words such as Above Average stay on one line, so names and long lists wrap instead. */
	@media (width >= 40rem) {
		td:is(.type, .short) {
			white-space: nowrap;
		}
	}
	.sort {
		display: inline-flex;
		align-items: center;
		gap: var(--gap-line);
		min-height: auto;
		padding: 0;
		border: 0;
		background: none;
		color: inherit;
		font-weight: inherit;
	}
	/* A numeric heading keeps its arrow on the left, so the label lines up with the numbers below it. */
	th.numeric .sort {
		flex-direction: row-reverse;
	}
	.sort:hover {
		color: var(--accent);
	}
	.sort-mark {
		color: var(--accent);
	}
	/* Only the active column shows its arrow, and an idle arrow appears on hover and focus. */
	.sort-mark.idle {
		opacity: 0;
	}
	.sort:hover .sort-mark.idle,
	.sort:focus-visible .sort-mark.idle {
		opacity: 0.8;
	}
	/* The link covers its icon and name only, so pointing at empty cell space neither follows nor previews it. */
	:global(.item-name) {
		display: inline-flex;
		align-items: center;
		gap: var(--gap-inline);
		vertical-align: middle;
		text-decoration: none;
		overflow-wrap: break-word;
	}
	:global(.item-name:hover) {
		text-decoration: underline;
	}
	:global(.item-name img) {
		flex: none;
		width: var(--size-icon-row);
		height: var(--size-icon-row);
		object-fit: contain;
	}
	.qualifier {
		color: var(--text-muted);
	}
	.status-note {
		display: block;
		padding-left: var(--inset-note);
		color: var(--text-muted);
		font-size: var(--type-0);
	}
	.empty {
		padding: var(--inset-loose) var(--inset);
		text-align: center;
		color: var(--text-muted);
	}
	.phone-sort,
	.filters-button {
		display: none;
	}

	/* Filters are disclosure groups, as in Afallon's filter panel: a title, a count of choices and a chevron. */
	.group {
		padding: var(--inset) 0;
		border-top: var(--border-width) solid var(--border);
	}
	.group:first-of-type {
		border-top: 0;
		padding-top: 0;
	}
	summary {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
		min-height: var(--size-chip);
		font-size: var(--type-0);
		font-weight: var(--weight-strong);
		list-style: none;
	}
	summary::-webkit-details-marker {
		display: none;
	}
	.chevron {
		width: var(--size-chevron);
		height: var(--size-chevron);
		margin-left: auto;
		border-right: var(--marker-width) solid var(--text-muted);
		border-bottom: var(--marker-width) solid var(--text-muted);
		transform: rotate(45deg) scale(0.6);
	}
	details[open] > summary .chevron {
		transform: rotate(225deg) scale(0.6);
	}
	.picked {
		padding: 0 var(--inset-cell);
		border-radius: var(--radius-round);
		background: var(--accent);
		color: var(--surface);
		font-size: var(--type-0);
	}
	.options {
		display: grid;
		margin: var(--gap-line) 0 0;
		padding: 0;
		list-style: none;
	}
	/* A long group scrolls inside a bounded height, so it does not push the later groups off the screen. */
	.options.long {
		max-height: var(--size-facet-values);
		overflow-y: auto;
	}
	.options label {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr) auto;
		align-items: center;
		gap: var(--gap-inline);
		min-height: var(--size-option);
		color: var(--text-soft);
	}
	.option-name {
		overflow-wrap: break-word;
	}
	.count {
		color: var(--text-muted);
		font-size: var(--type-0);
		font-variant-numeric: tabular-nums;
		white-space: nowrap;
	}
	.narrow,
	.group select,
	.bounds input {
		width: 100%;
		min-height: var(--size-control-compact);
		margin-top: var(--gap-line);
		padding: var(--inset-line) var(--inset-cell);
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-1);
		background: var(--surface);
	}
	.stat {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: center;
		gap: 0 var(--gap-inline);
		margin-top: var(--gap-block);
	}
	.stat .bounds {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
		grid-column: 1 / -1;
		gap: var(--gap-inline);
	}
	.remove {
		min-height: var(--size-chip);
		padding: 0 var(--inset-cell);
	}
	.match {
		display: grid;
		gap: var(--gap-line);
		margin-top: var(--gap-block);
	}

	/* Phones open the filters as a sheet whose footer stays in view. */
	.sheet {
		width: 100%;
		max-width: 100%;
		height: 100%;
		max-height: 100%;
		margin: 0;
		padding: 0;
		border: 0;
		background: var(--surface-panel);
		color: var(--text);
	}
	.sheet[open] {
		display: flex;
		flex-direction: column;
	}
	.sheet::backdrop {
		background: var(--surface-backdrop);
	}
	.sheet-head,
	.sheet-foot {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--gap-block);
		padding: var(--inset);
	}
	.sheet-head {
		border-bottom: var(--border-width) solid var(--border);
	}
	.sheet-head h2 {
		font-size: var(--type-sub);
	}
	.close {
		width: var(--size-control);
		min-height: var(--size-control);
		padding: 0;
	}
	.sheet-head,
	.sheet-foot,
	.sheet-body {
		padding-inline: var(--gutter);
	}
	.sheet-body {
		flex: 1 1 auto;
		overflow-y: auto;
		padding-block: var(--inset);
	}
	/* The sheet is for touch, so its rows, summaries and fields are full touch targets. */
	.sheet :is(summary, .options label) {
		min-height: var(--size-control);
	}
	.sheet :is(.narrow, .group select, .bounds input) {
		min-height: var(--size-control);
	}
	.sheet-foot {
		border-top: var(--border-width) solid var(--border);
	}
	.show {
		background: var(--accent);
		color: var(--surface);
		font-weight: var(--weight-strong);
	}

	@media (width < 64rem) {
		.layout {
			grid-template-columns: minmax(0, 1fr);
		}
		.sidebar {
			display: none;
		}
		.filters-button {
			display: inline-flex;
			flex: none;
			align-items: center;
			white-space: nowrap;
		}
	}
	@media (width >= 64rem) {
		.sheet {
			display: none;
		}
	}
	/* The name stays in view while a wide table scrolls sideways. A striped row's translucent tint lies over the
	   panel colour, so the sticky cell hides what scrolls beneath it. */
	@media (width >= 40rem) {
		thead th:first-child,
		tbody td.name {
			position: sticky;
			left: 0;
		}
		tbody td.name {
			z-index: var(--layer-table-heading);
			background: var(--surface-panel);
		}
		tbody tr:nth-child(even) td.name {
			background: linear-gradient(var(--surface-stripe), var(--surface-stripe)) var(--surface-panel);
		}
		tbody tr:hover td.name {
			background: var(--surface-hover);
		}
	}
	/* A phone reads each item as a compact row of the same surface: its name on the first line, then its type, its
	   labelled stats and its value on the next. */
	@media (width < 40rem) {
		.phone-sort {
			display: flex;
			align-items: center;
			gap: var(--gap-inline);
		}
		thead {
			display: none;
		}
		table,
		tbody {
			display: block;
		}
		tbody tr {
			display: flex;
			flex-wrap: wrap;
			align-items: baseline;
			gap: var(--gap-line) var(--gap-inline);
			padding: var(--inset-cell) var(--inset);
		}
		tbody tr + tr {
			border-top: var(--border-width) solid var(--border);
		}
		tbody td {
			padding: 0;
			border: 0;
		}
		tbody td.name {
			flex-basis: 100%;
		}
		tbody td.type {
			color: var(--text-muted);
		}
		tbody td.value {
			margin-left: auto;
		}
		.reveal {
			margin-left: 0;
			padding-inline: 0;
			text-align: left;
		}
		td[data-label]:not(.type, .value)::before {
			content: attr(data-label) ' ';
			color: var(--text-muted);
			font-size: var(--type-meta);
		}
	}
</style>
