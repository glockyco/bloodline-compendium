<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import Conditions from '#lib/Conditions.svelte';
	import ItemTooltip from '#lib/items/ItemTooltip.svelte';
	import SkillTooltip from '#lib/skills/SkillTooltip.svelte';
	import PassiveTooltip from '#lib/skills/PassiveTooltip.svelte';
	import SpellsongTooltip from '#lib/skills/SpellsongTooltip.svelte';
	import DisciplineTooltip from '#lib/skills/DisciplineTooltip.svelte';
	import type { Fact } from '#lib/site-data.js';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid from '#lib/ui/CardGrid.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import IconTile from '#lib/ui/IconTile.svelte';
	import Section from '#lib/ui/Section.svelte';
	import Tabs from '#lib/ui/Tabs.svelte';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
	const icons = $derived(data.icons);
	// Invented content at the extremes each primitive must survive: one line beside three lines, a missing icon,
	// single digits beside five, long unbroken names and unknown values.
	const cards = $derived([
		{ title: 'Short', count: 1, text: 'One line.' },
		{
			title: 'Three Lines of Description',
			count: 12345,
			text: 'A description long enough to wrap onto three lines in a narrow card, so its rows must still line up with the cards beside it.'
		},
		{
			title: 'A Title Long Enough to Wrap Across Two Lines',
			count: 42,
			text: 'Two lines of title.'
		},
		{
			title: 'Unbroken',
			count: 7,
			text: 'Supercalifragilisticexpialidociousness'
		}
	]);
	const tiles = $derived([
		{ name: 'Short', meta: '1 item', icon: icons[0] ?? null },
		{ name: 'A Name Long Enough to Wrap in a Tile', meta: '12,345 items', icon: icons[1] ?? null },
		{ name: 'No Icon', meta: '42 skills', icon: null },
		{ name: 'Supercalifragilisticexpialidociousness', meta: '7 skills', icon: icons[2] ?? null }
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
				effect: null,
				gameShows: '23456.789'
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

<div class="fixtures">
	<Section id="cards" title="Cards" count={cards.length}
		>{#snippet action()}<a href="#tables">Jump to tables</a>{/snippet}
		<CardGrid rows={3} label="Card fixtures">
			{#each cards as card (card.title)}
				<Card href="#cards">
					<span class="card-head" data-ui="row"><h3>{card.title}</h3></span>
					<span class="card-text" data-ui="row">{card.text}</span>
				</Card>
			{/each}
		</CardGrid>
	</Section>

	<Section id="tiles" title="Tiles" count={tiles.length}>
		<ul class="tile-grid">
			{#each tiles as tile (tile.name)}<li>
					<IconTile href="#tiles" icon={tile.icon} name={tile.name} meta={tile.meta} />
				</li>{/each}
		</ul>
	</Section>

	<Section
		id="small"
		title="A Section Heading Long Enough to Wrap Across Two Lines on a Phone"
		count={123456}
	>
		<div class="row">
			<ButtonLink href="#small">Browse Items</ButtonLink>
			<ButtonLink href="#small" secondary>Browse Skills</ButtonLink>
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
	</Section>

	<Section id="tables" title="Tables">
		<RelationTable items={rows} label="Table fixture" headings={['Merchant', 'Price']}
			>{#snippet row(entry)}<tr
					><td><span class="relation-name">{entry.name}</span></td><td class="numeric"
						>{formatNumber(entry.price)}<small>gold</small></td
					></tr
				>{/snippet}</RelationTable
		>
	</Section>

	<!-- More tabs than a phone's row holds, so the layout check sees the select that replaces them. -->
	<Section id="tabs" title="Tabs">
		<Tabs
			tabs={[
				{ id: 'woodcutting', label: 'Trees and Other Woodcutting Spots', count: 12 },
				{ id: 'mining', label: 'Ore Veins and Other Mining Spots', count: 9 },
				{ id: 'foraging', label: 'Bushes and Other Foraging Spots', count: 31 }
			]}
			label="Tab fixtures"
			initial="woodcutting">{#snippet panel(id)}<p>Panel {id}.</p>{/snippet}</Tabs
		>
	</Section>

	<Section id="tooltip" title="Item Tooltip" class="tooltip-fixture">
		<ItemTooltip item={tooltipItem} heading="p" />
	</Section>

	<!-- The other kinds' cards at their extremes: an unknown stat, many ranks, a long tune and a long name. -->
	<Section
		id="kind-tooltips"
		title="Skill, Passive, Spellsong and Discipline Tooltips"
		class="tooltip-fixture"
	>
		<div class="row">
			<SkillTooltip
				heading="p"
				skill={{
					name: 'A Skill Name Long Enough to Shrink Its Ribbon',
					icon: icons[0] ?? null,
					description: 'Throws something that burns, with a description long enough to wrap.',
					facts: [
						{
							stat: 'cooldown',
							label: 'Cooldown',
							tooltipLabel: 'Cooldown',
							number: 12.5,
							text: null,
							effect: null,
							unit: 'seconds'
						} as Fact,
						{
							stat: 'manaCost',
							label: 'Mana Cost',
							tooltipLabel: 'Mana Cost',
							number: null,
							text: null,
							effect: null,
							unit: null
						} as Fact
					]
				}}
			/>
			<PassiveTooltip
				passive={{
					name: 'Kindle',
					icon: icons[1] ?? null,
					description: '+2% damage per rank.',
					unlocks: 'Pyromancy level 5',
					costs: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
				}}
			/>
			<SpellsongTooltip
				song={{
					name: 'Ballad',
					description: null,
					tune: [1, 2, 3, 4, 1, 2, 3, 4, 1, 2],
					charges: 1,
					experience: 0,
					route: null
				}}
			/>
			<DisciplineTooltip
				discipline={{ name: 'Utility Gadgets', category: 'Tech/Etc.', contents: null }}
			/>
		</div>
	</Section>
</div>

<style>
	.fixtures {
		display: grid;
		gap: var(--gap-page);
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
	.row {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--gap-block);
		margin-bottom: var(--gap-groups);
	}
	.fixtures :global(.tooltip-fixture) {
		max-width: var(--size-item-side);
	}
</style>
