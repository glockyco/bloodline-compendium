<script lang="ts">
	import type { Snippet } from 'svelte';
	let {
		rows,
		columns,
		label,
		children
	}: {
		/** How many rows every card shares with its neighbours, so each row lines up across a line of cards. */
		rows: number;
		/**
		 * How many cards a full-width line holds, such as 4 for the four skill window categories. Narrow screens still
		 * wrap to fewer, never below the narrow card width. Without it, cards keep the standard width.
		 */
		columns?: number;
		label?: string;
		children: Snippet;
	} = $props();
</script>

<ul
	class="card-grid"
	class:fixed={columns !== undefined}
	style:--card-rows={rows}
	style:--card-columns={columns}
	aria-label={label}
>
	{@render children()}
</ul>

<style>
	.card-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(min(100%, var(--size-card)), 1fr));
		gap: 0 var(--gap-block);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.card-grid.fixed {
		grid-template-columns: repeat(
			auto-fill,
			minmax(
				max(
					min(100%, var(--size-card-narrow)),
					calc((100% - (var(--card-columns) - 1) * var(--gap-block)) / var(--card-columns))
				),
				1fr
			)
		);
	}
</style>
