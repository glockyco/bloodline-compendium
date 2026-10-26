<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';
	import StaticMore from './StaticMore.svelte';
	let {
		items,
		initial,
		noun,
		className,
		row
	}: { items: T[]; initial: number; noun: string; className: string; row: Snippet<[T, number]> } =
		$props();
	const preview = $derived(items.length > 10 ? initial : items.length);
</script>

<ul class={className}>
	{#each items.slice(0, preview) as item, index (index)}<li>{@render row(item, index)}</li>{/each}
</ul>
{#if items.length > preview}
	<StaticMore
		more={`Show ${items.length - preview} more ${noun}`}
		less={`Hide additional ${noun}`}
		label={noun}
	>
		<ul class={className}>
			{#each items.slice(preview) as item, index (index)}<li>
					{@render row(item, index + preview)}
				</li>{/each}
		</ul>
	</StaticMore>
{/if}
