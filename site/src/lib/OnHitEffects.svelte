<script lang="ts">
	import type { ItemPage } from './site-data.js';
	import { formatNumber } from './format.js';
	import RelationTable from './RelationTable.svelte';
	import Section from './ui/Section.svelte';

	let { effects }: { effects?: ItemPage['onHitEffects'] } = $props();

	type Effect = NonNullable<ItemPage['onHitEffects']>[number];

	function chance(effect: Effect): string {
		if (effect.trigger === 'Every shot aimed at an undead enemy') return effect.trigger;
		if (effect.chance === 1) return 'Every hit';
		const percent = effect.chance * 100;
		return Math.abs(percent - Math.round(percent)) < 1e-9
			? `${formatNumber(Math.round(percent))}% per hit`
			: `1 in ${formatNumber(1 / effect.chance)} per hit`;
	}

	function description(effect: Effect): string {
		if (effect.effect === 'Burning Ground') {
			return effect.durationSeconds == null
				? 'Unknown'
				: `Sets the ground under the enemy on fire for ${formatNumber(effect.durationSeconds)} seconds.`;
		}
		if (effect.amount == null) return 'Unknown';
		const value = formatNumber(effect.amount);
		if (effect.effect === 'Restore Mana') return `Restores ${value} mana.`;
		const interval =
			effect.tickIntervalSeconds == null
				? ''
				: effect.tickIntervalSeconds === 1
					? ' per second'
					: ` every ${formatNumber(effect.tickIntervalSeconds)} seconds`;
		const duration =
			effect.durationSeconds == null
				? ' for an unknown duration'
				: ` for ${formatNumber(effect.durationSeconds)} seconds`;
		return `${value} damage${interval}${duration}.`;
	}
</script>

{#if effects?.length}
	<Section id="on-hit-effects" title="On-Hit Effects" count={effects.length}>
		<div class="on-hit-table">
			<RelationTable
				items={effects}
				label="On-Hit Effects"
				headings={['Effect', 'Chance', 'What It Does']}
				numeric={false}
				stackOnMobile={false}
			>
				{#snippet row(effect)}
					<tr>
						<th scope="row">{effect.effect}</th>
						<td>{chance(effect)}</td>
						<td>{description(effect)}{effect.behavior ? ` ${effect.behavior}` : ''}</td>
					</tr>
				{/snippet}
			</RelationTable>
		</div>
	</Section>
{/if}

<style>
	.on-hit-table :global(table) {
		table-layout: fixed;
	}
	.on-hit-table :global(thead th:nth-child(1)) {
		width: 22%;
	}
	.on-hit-table :global(thead th:nth-child(2)) {
		width: 25%;
	}
	.on-hit-table :global(:is(th, td)) {
		padding-inline: var(--inset-cell);
		overflow-wrap: anywhere;
	}
</style>
