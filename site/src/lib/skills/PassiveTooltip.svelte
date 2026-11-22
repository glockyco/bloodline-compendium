<script lang="ts">
	import GameCard from '../ui/GameCard.svelte';
	import { formatNumber, plural } from '../format.js';
	import type { PassivePreview } from '../entity/previews.js';
	/** A passive's card: its level and the points each rank costs, then its description. */
	let { passive }: { passive: Omit<PassivePreview, 'kind'> } = $props();
	const lines = $derived([
		{ text: `Unlocks at ${passive.unlocks}`, size: 'small' as const, game: false },
		{
			text:
				passive.costs.length === 1
					? plural(passive.costs[0], 'point', 'points')
					: `${plural(passive.costs.length, 'rank', 'ranks')}: ${passive.costs.map(formatNumber).join(', ')} points`,
			size: 'small' as const,
			game: false
		}
	]);
</script>

<div data-ui="tooltip">
	<GameCard
		name={passive.name}
		icon={passive.icon}
		description={passive.description}
		heading="p"
		{lines}
	/>
</div>
