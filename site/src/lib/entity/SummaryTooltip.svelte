<script lang="ts">
	import GameCard from '../ui/GameCard.svelte';
	import type { SummaryPreview } from './previews.js';
	/** A resource's, profession's or mechanic's card: the facts its list card shows, in the site's voice. */
	let { preview }: { preview: SummaryPreview } = $props();
	const lines = $derived(
		preview.lines.map((text) => ({ text, size: 'small' as const, game: false }))
	);
</script>

<div data-ui="tooltip">
	<GameCard
		name={preview.name}
		icon={preview.icon}
		slot={preview.icon != null}
		description={null}
		heading="p"
		{lines}
	/>
</div>
