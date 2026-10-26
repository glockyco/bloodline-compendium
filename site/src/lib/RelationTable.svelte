<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';
	import StaticMore from './StaticMore.svelte';
	let {
		items,
		label,
		headings,
		row
	}: {
		items: T[];
		label: string;
		headings: string[];
		row: Snippet<[T, number]>;
	} = $props();
	const preview = $derived(items.length > 10 ? 8 : items.length);
</script>

{#snippet table(entries: T[], offset: number)}
	<table class="relation-table" aria-label={label + (offset ? ' continued' : '')}>
		<thead
			><tr
				>{#each headings as heading, index (index)}<th scope="col" class:numeric={index > 0}
						>{heading}</th
					>{/each}</tr
			></thead
		>
		<tbody
			>{#each entries as item, index (index)}{@render row(item, index + offset)}{/each}</tbody
		>
	</table>
{/snippet}
<div class="relation-panel">
	{@render table(items.slice(0, preview), 0)}
	{#if items.length > preview}
		<StaticMore more={`Show ${items.length - preview} more`} less="Show fewer" {label}
			>{@render table(items.slice(preview), preview)}</StaticMore
		>
	{/if}
</div>
