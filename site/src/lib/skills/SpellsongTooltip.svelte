<script lang="ts">
	import GameCard from '../ui/GameCard.svelte';
	import Tune from './Tune.svelte';
	import { formatNumber } from '../format.js';
	import type { SpellsongPreview } from '../entity/previews.js';
	/** A spellsong's card: its charges and the Lute experience a play gives, its tune and its description. */
	let { song }: { song: Omit<SpellsongPreview, 'kind'> } = $props();
	const lines = $derived([
		{
			text: `${formatNumber(song.charges)} ${song.charges === 1 ? 'charge' : 'charges'}`,
			size: 'small' as const
		},
		{ text: `${formatNumber(song.experience)} Lute experience`, size: 'small' as const }
	]);
</script>

<div class="song" data-ui="tooltip">
	<GameCard
		name={song.name}
		icon={null}
		slot={false}
		description={song.description}
		heading="p"
		{lines}
	/>
	<Tune tune={song.tune} />
</div>

<style>
	.song {
		display: grid;
		gap: var(--gap-inline);
	}
</style>
