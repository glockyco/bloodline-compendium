<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid, { balancedColumns } from '#lib/ui/CardGrid.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import { entityPath } from '#lib/entities.js';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
	const professions = $derived(data.index.professions);
	const levels = (min: number | null | undefined, max: number | null | undefined) =>
		min == null || max == null
			? 'None'
			: min === max
				? formatNumber(min)
				: `${formatNumber(min)} to ${formatNumber(max)}`;
</script>

<SeoHead
	title="Professions | The Bloodline Compendium"
	description="Weaponsmithing, Cooking and every other profession in The Bloodline, with each recipe's level, ingredients and experience."
/>
<header class="page-heading list-title">
	<h1>Professions</h1>
	<p class="intro">
		Professions level up as you craft, cook and farm. Each one lists its recipes by level, with
		ingredients and experience.
	</p>
</header>
<CardGrid rows={3} columns={balancedColumns(professions.length)} label="Professions">
	{#each professions as profession (profession.key)}<Card
			href={entityPath(
				{
					kind: 'profession',
					key: profession.key,
					name: profession.name,
					slug: profession.slug,
					icon: null
				},
				data.kinds
			)!}
		>
			<h2 class="card-head"><IconSlot icon={profession.icon ?? null} />{profession.name}</h2>
			<dl class="card-facts">
				<div>
					<dt>Made at</dt>
					<dd class:absent={!profession.stations.length}>
						{profession.stations.map((station) => station.name).join(', ') || 'None'}
					</dd>
				</div>
				<div>
					<dt>Levels</dt>
					<dd class:absent={profession.minLevel == null}>
						{levels(profession.minLevel, profession.maxLevel)}
					</dd>
				</div>
				<div>
					<dt>Items</dt>
					<dd>{formatNumber(profession.count)}</dd>
				</div>
			</dl>
			<ul class="examples" aria-label="Examples">
				{#each profession.examples as example (example.key)}<li title={example.name}>
						<IconSlot icon={example.icon ?? null} size="row" /><span class="sr-only"
							>{example.name}</span
						>
					</li>{/each}
			</ul>
		</Card>{/each}
</CardGrid>

<style>
	.card-head {
		display: flex;
		align-items: center;
		gap: var(--gap-block);
		margin: 0;
		font-size: var(--type-sub);
		line-height: var(--leading-heading);
	}
	/* Each fact is a label above its value, the place taking the widest column, as its names run longest. */
	.card-facts {
		display: grid;
		grid-template-columns: minmax(0, 2fr) minmax(0, 1fr) minmax(0, 1fr);
		gap: var(--gap-block);
		margin: 0;
	}
	.card-facts div {
		display: grid;
		align-content: start;
		gap: var(--gap-line);
	}
	.card-facts dt {
		color: var(--text-muted);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
	}
	.card-facts dd {
		margin: 0;
	}
	.examples {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline);
		margin: 0;
		padding: 0;
		list-style: none;
	}
</style>
