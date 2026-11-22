<script lang="ts">
	import GameCard from '../ui/GameCard.svelte';
	import Tune from './Tune.svelte';
	import { formatNumber, plural } from '../format.js';
	import type { SpellsongPreview } from '../entity/previews.js';
	/** A spellsong's card: its charges and the Lute experience a play gives, its tune and its description. */
	let { song }: { song: Omit<SpellsongPreview, 'kind'> } = $props();
	const lines = $derived([
		{ text: plural(song.charges, 'charge', 'charges'), size: 'small' as const, game: false },
		{
			text: `${formatNumber(song.experience)} Lute experience`,
			size: 'small' as const,
			game: false
		}
	]);
</script>

<div data-ui="tooltip">
	<GameCard
		name={song.name}
		icon={null}
		slot={false}
		description={song.description}
		heading="p"
		{lines}><Tune tune={song.tune} /></GameCard
	>
</div>
