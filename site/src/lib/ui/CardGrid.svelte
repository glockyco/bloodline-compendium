<script lang="ts" module>
	/**
	 * How many cards a full line holds for a set of a given size, at most the given number: the width whose last line
	 * is fullest, so no line ends with a lone card, as four mechanics read as two lines of two and seven containers as
	 * lines of four and three. A set longer than two full lines keeps at least one fewer than the most, so a long list
	 * stays dense instead of narrowing to two cards a line.
	 */
	export function balancedColumns(count: number, most = 3): number {
		const least = count > most * 2 ? Math.max(2, most - 1) : 2;
		let best = Math.min(most, Math.max(count, 1));
		let bestFill = 0;
		for (let columns = Math.min(most, count); columns >= least; columns--) {
			const fill = count % columns === 0 ? 1 : (count % columns) / columns;
			if (fill > bestFill) [best, bestFill] = [columns, fill];
		}
		return best;
	}
</script>

<script lang="ts">
	import type { Snippet } from 'svelte';
	let {
		rows,
		columns,
		small = false,
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
		/** Whether cards may narrow to an icon tile, two to a phone screen, for a name, a short line and a few icons. */
		small?: boolean;
		label?: string;
		children: Snippet;
	} = $props();
</script>

<ul
	class="card-grid"
	class:fixed={columns !== undefined}
	style:--card-rows={rows}
	style:--card-columns={columns}
	style:--card-narrowest={small ? 'var(--size-tile)' : 'var(--size-card-narrow)'}
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
					min(100%, var(--card-narrowest)),
					calc((100% - (var(--card-columns) - 1) * var(--gap-block)) / var(--card-columns))
				),
				1fr
			)
		);
	}
</style>
