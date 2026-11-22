<script lang="ts">
	import type { Fact, ItemPage } from '../site-data.js';
	import { cardLines, detailRows } from './game-card.js';
	import ItemDetails from './ItemDetails.svelte';
	import OnHitEffects from './OnHitEffects.svelte';
	import GameCard from '../ui/GameCard.svelte';

	interface TooltipItem {
		name: string;
		icon: string | null;
		description: string | null;
		facts: Fact[];
		projectileGroups?: ItemPage['projectileGroups'];
		onHitEffects?: ItemPage['onHitEffects'];
	}
	let {
		item,
		heading = 'h2',
		details = true
	}: {
		item: TooltipItem;
		heading?: 'h2' | 'p';
		/** Whether the facts the game's tooltip does not print follow the card, as in link previews. */
		details?: boolean;
	} = $props();

	const lines = $derived(cardLines(item.facts));
	const rows = $derived(details ? detailRows(item.facts, item.projectileGroups) : []);
</script>

<div class="tooltip" data-ui="tooltip">
	<GameCard
		name={item.name}
		icon={item.icon}
		description={item.description}
		{heading}
		lines={[
			...(lines.first ? [{ text: lines.first.text, size: 'large' as const }] : []),
			...(lines.second ? [{ text: lines.second.text, size: 'small' as const }] : [])
		]}
	/>
	{#if rows.length || (details && item.projectileGroups?.length)}<ItemDetails
			{rows}
			projectileGroups={item.projectileGroups}
			chargedBodyHitDamage={item.facts.find((fact) => fact.stat === 'chargedBodyHitDamage')?.number}
		/>{/if}
	{#if details}<OnHitEffects effects={item.onHitEffects} />{/if}
</div>

<style>
	.tooltip {
		display: grid;
		gap: var(--gap-block);
	}
</style>
