<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';
	let {
		items,
		initial,
		noun,
		className,
		row
	}: { items: T[]; initial: number; noun: string; className: string; row: Snippet<[T, number]> } =
		$props();
</script>

<ul class={className}>
	{#each items.slice(0, initial) as item, index (index)}<li>{@render row(item, index)}</li>{/each}
</ul>
{#if items.length > initial}
	<details class="more-entries">
		<summary
			><span class="show-more">Show {items.length - initial} more {noun}</span><span
				class="show-less">Hide additional {noun}</span
			></summary
		>
		<ul class={className}>
			{#each items.slice(initial) as item, index (index)}<li>
					{@render row(item, index + initial)}
				</li>{/each}
		</ul>
	</details>
{/if}

<style>
	.show-less {
		display: none;
	}
	details[open] > summary .show-more {
		display: none;
	}
	details[open] > summary .show-less {
		display: inline;
	}
</style>
