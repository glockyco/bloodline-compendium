<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';
	import MoreToggle from './MoreToggle.svelte';
	import { isHydrated } from './hydration.js';
	let {
		items,
		initial,
		noun,
		className,
		label,
		row
	}: {
		items: T[];
		initial: number;
		noun: string;
		className: string;
		/** The list's name for screen readers, which the button names after its count. */
		label: string;
		row: Snippet<[T, number]>;
	} = $props();
	const preview = $derived(items.length > 10 ? initial : items.length);
	// Entries past the preview stay in the same list, so a grid of them has no gap where the preview ends.
	// The static page carries every entry, so a reader without JavaScript can open them.
	let shown = $state(!isHydrated());
</script>

<ul class={className}>
	{#each items.slice(0, preview) as item, index (index)}<li>{@render row(item, index)}</li>{/each}
	{#if items.length > preview && shown}{#each items.slice(preview) as item, index (index)}<li
				class="more"
			>
				{@render row(item, index + preview)}
			</li>{/each}{/if}
</ul>
{#if items.length > preview}<MoreToggle
		more={`Show ${items.length - preview} more ${noun}`}
		less={`Hide additional ${noun}`}
		{label}
		onshow={(value) => (shown = value)}
	/>{/if}

<style>
	ul {
		margin: 0;
	}
</style>
