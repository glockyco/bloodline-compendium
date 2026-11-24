<script lang="ts">
	import { sourceHeadings } from '#lib/labels.js';
	import type { ItemSummary, ListFacet, SourceKind } from '#lib/site-data.js';
	import { formatNumber } from '#lib/format.js';
	import { neutralSale } from '#lib/rules/price.js';
	import SeoHead from '#lib/SeoHead.svelte';
	import ListName from '#lib/list/ListName.svelte';
	import EntityList from '#lib/list/EntityList.svelte';
	import type { FixedDisplay } from '#lib/list/state.js';
	import { kindPath } from '#lib/entities.js';
	import { itemListModel } from '#lib/items/list.js';
	let { data } = $props();
	const index = $derived(data.index);
	const model = $derived(itemListModel(index));
	const categoryNames = $derived(
		new Map(
			index.categories.map((category) => [category.slug, category.name.toLocaleLowerCase('en')])
		)
	);
	const categoryPlurals = $derived(
		Object.fromEntries(index.categories.map((category) => [category.slug, category.plural]))
	);
	// The type column adds nothing when every shown item's type only repeats its category, as for food.
	function fixed(shown: ItemSummary[]): FixedDisplay<ItemSummary>[] {
		const showType = shown.some(
			(item) => item.type.toLocaleLowerCase('en') !== categoryNames.get(item.category)
		);
		return [
			...(showType
				? [
						{
							id: 'type',
							label: 'Type',
							numeric: false,
							position: 'start' as const,
							text: (item: ItemSummary) => item.type,
							game: true
						}
					]
				: []),
			{
				id: 'value',
				label: 'Sells for',
				numeric: true,
				position: 'end' as const,
				text: (item: ItemSummary) =>
					item.value === null ? 'Unknown' : formatNumber(neutralSale(item.value)) + ' gold'
			}
		];
	}
	function valueLabel(facet: ListFacet, value: string): string {
		return (
			facet.labels[value] ?? (facet.id === 'source' ? sourceHeadings[value as SourceKind] : value)
		);
	}
</script>

<SeoHead
	title="Items | The Bloodline Compendium"
	description="Find The Bloodline items by name, category, type and stats. Compare values and open known shops, recipes, drops and gathering sources."
/>
<h1 class="list-title">Items</h1>
<EntityList
	rows={index.items}
	{model}
	noun={{ one: 'item', many: 'items', column: 'Item' }}
	{categoryPlurals}
	{fixed}
	statusNotes={{ noKnownSource: 'No known source', notInNormalPlay: 'Not in normal play' }}
	reveal={{
		show: (count) => `Show ${count} more without a known way to get them`,
		hide: 'Hide items without a known way to get them'
	}}
	{valueLabel}
	path={(query) => kindPath('item', data.kinds, query)!}
>
	{#snippet name(item: ItemSummary, meta: string | null, note: string | null)}<ListName
			row={item}
			{meta}
			{note}
		/>{/snippet}
</EntityList>
