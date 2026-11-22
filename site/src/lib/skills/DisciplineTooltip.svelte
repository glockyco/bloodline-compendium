<script lang="ts">
	import GameCard from '../ui/GameCard.svelte';
	import type { DisciplinePreview } from '../entity/previews.js';
	/** A discipline's card: its category and what it teaches, as its list shows them. */
	let { discipline }: { discipline: Omit<DisciplinePreview, 'kind'> } = $props();
	const lines = $derived([
		{ text: discipline.category, size: 'small' as const, game: false },
		...(discipline.contents
			? [{ text: discipline.contents, size: 'small' as const, game: false }]
			: [])
	]);
</script>

<div data-ui="tooltip">
	<GameCard
		name={discipline.name}
		icon={null}
		slot={false}
		description={null}
		heading="p"
		{lines}
	/>
</div>
