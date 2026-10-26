<script lang="ts">
	import type { Snippet } from 'svelte';
	let {
		rows,
		label,
		children
	}: {
		/** How many rows every card shares with its neighbours, so each row lines up across a line of cards. */
		rows: number;
		label?: string;
		children: Snippet;
	} = $props();
</script>

<ul class="card-grid" style:--card-rows={rows} aria-label={label}>{@render children()}</ul>

<style>
	.card-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(min(100%, var(--size-card)), 1fr));
		gap: 0 var(--space-4);
		margin: 0;
		padding: 0;
		list-style: none;
	}
</style>
