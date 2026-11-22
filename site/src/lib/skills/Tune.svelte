<script lang="ts">
	/**
	 * A spellsong's tune as the game draws it: one arrow per note, 1 up, 2 down, 3 right and 4 left, as the game's
	 * note sheet builds them (tools/runtime/checks/spellsong-arrows.lua).
	 */
	let { tune }: { tune: number[] } = $props();
	const names: Record<number, string> = { 1: 'up', 2: 'down', 3: 'right', 4: 'left' };
	const rotation: Record<number, number> = { 1: 0, 2: 180, 3: 90, 4: 270 };
</script>

<span class="tune" role="img" aria-label={'Notes: ' + tune.map((note) => names[note]).join(', ')}>
	{#each tune as note, index (index)}<svg viewBox="0 0 16 16" aria-hidden="true"
			><path d="M8 2 14 10H10V14H6V10H2Z" transform={`rotate(${rotation[note]} 8 8)`} /></svg
		>{/each}
</span>

<style>
	.tune {
		display: inline-flex;
		flex-wrap: wrap;
		gap: var(--gap-hair);
	}
	svg {
		width: var(--size-note);
		height: var(--size-note);
		fill: var(--accent);
	}
</style>
