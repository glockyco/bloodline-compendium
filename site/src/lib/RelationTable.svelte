<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';
	import StaticMore from './StaticMore.svelte';
	let {
		items,
		label,
		headings,
		numeric = true,
		row
	}: {
		items: T[];
		label: string;
		headings: string[];
		/** Whether the columns after the first hold numbers, which align right with their heading, one flag per column. */
		numeric?: boolean | boolean[];
		row: Snippet<[T, number]>;
	} = $props();
	const preview = $derived(items.length > 10 ? 8 : items.length);
</script>

{#snippet table(entries: T[], offset: number)}
	<table class="relation-table" aria-label={label + (offset ? ' continued' : '')}>
		<thead
			><tr
				>{#each headings as heading, index (index)}<th
						scope="col"
						class:numeric={index > 0 && (Array.isArray(numeric) ? numeric[index - 1] : numeric)}
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

<style>
	/* After Afallon's relation table: one bordered surface, compact cells, muted small headings and secondary lines,
	   and numbers right-aligned under their heading. Columns size to their content. */
	.relation-panel {
		min-width: 0;
		overflow-x: auto;
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
	}
	.relation-panel :global(table) {
		width: 100%;
		border-collapse: collapse;
	}
	.relation-panel :global(:is(th, td)) {
		padding: var(--inset-cell) var(--inset);
		text-align: left;
		vertical-align: top;
		border: 0;
	}
	.relation-panel :global(thead th) {
		border-bottom: var(--border-width) solid var(--border);
		background: none;
		color: var(--text-muted);
		font-size: var(--type-0);
		font-weight: var(--weight-strong);
	}
	.relation-panel :global(tbody :is(th, td)) {
		border-top: var(--border-width) solid var(--border);
		overflow-wrap: break-word;
	}
	.relation-panel :global(tbody tr:first-child :is(th, td)) {
		border-top: 0;
	}
	.relation-panel :global(tbody tr:hover) {
		background: var(--surface-hover);
	}
	/* Numbers stay on one line, and their column grows to fit them. */
	.relation-panel :global(.numeric) {
		text-align: right;
		font-variant-numeric: tabular-nums;
		white-space: nowrap;
	}
	.relation-panel :global(.numeric small) {
		white-space: normal;
	}
	.relation-panel :global(td small) {
		display: block;
		margin-top: var(--gap-line);
		color: var(--text-muted);
		font-size: var(--type-0);
	}
	/* A place that several rows share heads them, so their rows do not repeat it. */
	.relation-panel :global(.place-heading th) {
		background: var(--surface-raised);
		color: var(--text-soft);
		font-size: var(--type-0);
		font-weight: var(--weight-strong);
	}
</style>
