<script lang="ts">
	import type { ItemPage } from './site-data.js';
	import { formatChance, formatNumber } from './format.js';
	import RelationTable from './RelationTable.svelte';
	import Section from './ui/Section.svelte';

	let { effects }: { effects?: ItemPage['onHitEffects'] } = $props();

	function amount(effect: NonNullable<ItemPage['onHitEffects']>[number]): string {
		if (effect.amount == null) return 'Unknown';
		const value = formatNumber(effect.amount);
		if (effect.effect === 'Restore Mana') return `${value} Mana Instantly`;
		return effect.tickIntervalSeconds == null
			? `${value} Damage`
			: `${value} Damage Every ${formatNumber(effect.tickIntervalSeconds)} s`;
	}
</script>

{#if effects?.length}
	<Section id="on-hit-effects" title="On-Hit Effects" count={effects.length}>
		<RelationTable
			items={effects}
			label="On-Hit Effects"
			headings={['Effect', 'When', 'Chance', 'Amount', 'Duration']}
			numeric={[false, true, false, false]}
		>
			{#snippet row(effect)}
				<tr>
					<th scope="row">{effect.effect}</th>
					<td>{effect.trigger}</td>
					<td class="numeric">{formatChance(effect.chance)}</td>
					<td>{amount(effect)}</td>
					<td>
						{#if effect.effect === 'Restore Mana'}Instant
						{:else if effect.durationSeconds != null}{formatNumber(effect.durationSeconds)} s
						{:else}Unknown{/if}
						{#if effect.behavior}<small>{effect.behavior}</small>{/if}
					</td>
				</tr>
			{/snippet}
		</RelationTable>
	</Section>
{/if}
