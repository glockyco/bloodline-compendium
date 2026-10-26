<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import Conditions from '#lib/Conditions.svelte';
	import ItemTooltip from '#lib/ItemTooltip.svelte';
	import type { Fact } from '#lib/site-data.js';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid from '#lib/ui/CardGrid.svelte';
	import Count from '#lib/ui/Count.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import IssueNote from '#lib/ui/IssueNote.svelte';
	import SectionHeading from '#lib/ui/SectionHeading.svelte';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
	const icons = $derived(data.icons);
	// Invented content at the extremes each primitive must survive: one line beside three lines, empty
	// slots, single digits beside five, long unbroken names and unknown values.
	const cards = $derived([
		{ title: 'Short', count: 1, text: 'One line.', icons: icons },
		{
			title: 'Three Lines of Description',
			count: 12345,
			text: 'A description long enough to wrap onto three lines in a narrow card, so its icon row must still line up with the cards beside it.',
			icons: icons.slice(0, 2)
		},
		{
			title: 'A Title Long Enough to Wrap Across Two Lines',
			count: 42,
			text: 'Two empty slots.',
			icons: []
		},
		{
			title: 'Unbroken',
			count: 7,
			text: 'Supercalifragilisticexpialidociousness',
			icons: icons.slice(0, 1)
		}
	]);
	const rows = [
		{ name: 'A merchant with an extraordinarily long name that must wrap', price: 1234567 },
		{ name: 'Short', price: 1 }
	];
	const tooltipItem = $derived({
		name: 'An Item Name Long Enough to Wrap Inside the Ribbon',
		icon: icons[0] ?? null,
		description:
			'A description of several sentences. It wraps over more than one line, as many game descriptions do.',
		facts: [
			{
				stat: 'damage',
				label: 'Damage',
				tooltipLabel: 'Damage',
				unit: null,
				number: 12345.678,
				text: null,
				effect: null
			},
			{
				stat: 'speed',
				label: 'Speed',
				tooltipLabel: 'Speed',
				unit: null,
				number: null,
				text: null,
				effect: null
			},
			{ stat: 'value', label: 'Value', unit: 'gold', number: 1000000, text: null, effect: null },
			{
				stat: 'effect',
				label: 'Effect 1',
				number: null,
				text: null,
				effect: { name: 'Restore Health', power: 345, seconds: 60 }
			},
			{ stat: 'damageType', label: 'Damage type', number: null, text: 'Pierce', effect: null }
		] satisfies Fact[]
	});
</script>

<SeoHead
	title="Presentation Fixtures | The Bloodline Compendium"
	description="Every shared presentation primitive with extreme invented content, for layout checks."
	noindex
/>
<h1>Presentation Fixtures</h1>
<p class="intro">Invented content at the extremes each shared primitive must survive.</p>

<section aria-labelledby="cards">
	<SectionHeading
		id="cards"
		title="Cards"
		count={cards.length}
		link={{ href: '#tables', label: 'Jump to tables' }}
	/>
	<CardGrid rows={3} label="Card fixtures">
		{#each cards as card (card.title)}
			<Card href="#cards">
				<span class="card-head" data-ui="row"
					><h3>{card.title}</h3>
					<Count value={card.count} /></span
				>
				<span class="card-text" data-ui="row">{card.text}</span>
				<span class="slots" data-ui="slots">
					{#each [0, 1, 2, 3, 4] as slot (slot)}<IconSlot icon={card.icons[slot] ?? null} />{/each}
				</span>
			</Card>
		{/each}
	</CardGrid>
</section>

<section aria-labelledby="small">
	<SectionHeading
		id="small"
		title="A Section Heading Long Enough to Wrap Across Two Lines on a Phone"
	/>
	<div class="row">
		<IconSlot icon={icons[0] ?? null} size="stack" />
		<IconSlot icon={null} size="stack" />
		<Count value={1} />
		<Count value={123456} />
		<ButtonLink href="#small">Browse All Items</ButtonLink>
	</div>
	<p>
		Linked: <EntityLink entity={data.item} />. Without a page: <EntityLink
			entity={{
				kind: 'merchant',
				key: '/Invented/Merchant',
				name: 'Unpublished Merchant',
				slug: null,
				icon: null
			}}
		/>.
	</p>
	<div id="condition-fixture">
		<Conditions
			conditions={[
				{
					kind: 'gameState',
					entity: data.item,
					value: 13,
					label: `Requires ${data.item.name} at level 13.`
				}
			]}
		/>
	</div>
	<IssueNote title="An issue title that runs long enough to wrap">
		The game shows one value in its tooltip and applies another when the item is equipped. This body
		runs over several lines.
	</IssueNote>
</section>

<section aria-labelledby="tables">
	<SectionHeading id="tables" title="Tables" />
	<RelationTable items={rows} label="Table fixture" headings={['Merchant', 'Price']}
		>{#snippet row(entry)}<tr
				><td><span class="relation-name">{entry.name}</span></td><td class="numeric"
					>{formatNumber(entry.price)}<small>gold</small></td
				></tr
			>{/snippet}</RelationTable
	>
</section>

<section aria-labelledby="tooltip" class="tooltip-fixture">
	<SectionHeading id="tooltip" title="Item Tooltip" />
	<ItemTooltip item={tooltipItem} heading="p" />
</section>

<style>
	section {
		margin-top: var(--gap-page);
	}
	.card-head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: var(--gap-block);
	}
	h3 {
		margin: 0;
		font-size: var(--type-3);
		line-height: var(--leading-title);
		overflow-wrap: anywhere;
	}
	.card-text {
		color: var(--text-muted);
		line-height: var(--leading-snug);
		overflow-wrap: anywhere;
	}
	.slots {
		display: grid;
		grid-template-columns: repeat(5, minmax(0, 1fr));
		gap: var(--gap-inline);
		align-self: end;
	}
	.row {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--gap-block);
		margin-bottom: var(--gap-groups);
	}
	.tooltip-fixture {
		max-width: var(--size-item-side);
	}
</style>
