<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';
	import StaticMore from './StaticMore.svelte';
	let {
		items,
		label,
		headings,
		numeric = true,
		stackOnMobile = true,
		collapse = true,
		row
	}: {
		items: T[];
		label: string;
		/** Column headings. A heading with a unit names it once for every row, which the rows then leave out. */
		headings: (string | { label: string; unit: string | null })[];
		/** Whether the columns after the first hold numbers, which align right with their heading, one flag per column. */
		numeric?: boolean | boolean[];
		stackOnMobile?: boolean;
		/** Whether a long table shows its first rows behind a button, which a table read as a whole turns off. */
		collapse?: boolean;
		row: Snippet<[T, number]>;
	} = $props();
	const preview = $derived(collapse && items.length > 10 ? 8 : items.length);
	const parts = $derived(
		headings.map((heading) =>
			typeof heading === 'string' ? { label: heading, unit: null } : heading
		)
	);
	// By default, a phone stacks tables with three or more columns, labelling each value after the name. With no
	// heading above them, a value carries its column's unit after it, as the game writes "4 seconds".
	const labels = $derived(
		parts
			.slice(1)
			.flatMap((heading, index) => [
				`--label-${index + 2}: ${JSON.stringify(heading.label)}`,
				`--unit-${index + 2}: ${JSON.stringify(heading.unit ? ' ' + heading.unit : '')}`
			])
			.join('; ')
	);
</script>

{#snippet table(entries: T[], offset: number)}
	<table
		class="relation-table"
		class:stacked={stackOnMobile && headings.length > 2}
		aria-label={label + (offset ? ' continued' : '')}
		style={labels}
	>
		<thead
			><tr
				>{#each parts as heading, index (index)}<th
						scope="col"
						class:numeric={index > 0 && (Array.isArray(numeric) ? numeric[index - 1] : numeric)}
						>{heading.label}{#if heading.unit}<small>{heading.unit}</small>{/if}</th
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
		container-type: inline-size;
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
	.relation-panel :global(td) {
		line-height: var(--leading-data);
	}
	/* Narrow panels, including item previews, stack labelled values without horizontal scrolling. */
	@container (width < 40rem) {
		.relation-panel :global(.stacked) {
			display: block;
		}
		.relation-panel :global(.stacked thead) {
			position: absolute;
			width: var(--size-hairline);
			height: var(--size-hairline);
			overflow: hidden;
			clip-path: inset(50%);
		}
		.relation-panel :global(.stacked tbody) {
			display: block;
		}
		.relation-panel :global(.stacked tr) {
			display: flex;
			flex-wrap: wrap;
			align-items: baseline;
			gap: var(--gap-line) 0;
			padding: var(--inset-cell) 0;
		}
		.relation-panel :global(.stacked tbody tr + tr) {
			border-top: var(--border-width) solid var(--border);
		}
		.relation-panel :global(.stacked tr > :is(th, td)) {
			padding: 0 var(--inset);
			border: 0;
		}
		.relation-panel :global(.stacked tr > :first-child) {
			flex-basis: 100%;
		}
		/* Each value takes its own line, its label on the left and the value with its unit on the right. */
		.relation-panel :global(.stacked td:nth-child(n + 2)) {
			display: flex;
			flex: 1 0 100%;
			align-items: baseline;
			gap: var(--gap-inline);
		}
		.relation-panel :global(.stacked td:nth-child(n + 2)::before) {
			margin-right: auto;
			color: var(--text-muted);
			font-size: var(--type-meta);
		}
		.relation-panel :global(.stacked .numeric small) {
			display: inline;
			margin-top: 0;
		}
		.relation-panel :global(.stacked td:nth-child(2)::before) {
			content: var(--label-2);
		}
		.relation-panel :global(.stacked td:nth-child(3)::before) {
			content: var(--label-3);
		}
		.relation-panel :global(.stacked td:nth-child(4)::before) {
			content: var(--label-4);
		}
		.relation-panel :global(.stacked td:nth-child(5)::before) {
			content: var(--label-5);
		}
		.relation-panel :global(.stacked td:nth-child(2)::after) {
			content: var(--unit-2);
			white-space: pre;
		}
		.relation-panel :global(.stacked td:nth-child(3)::after) {
			content: var(--unit-3);
			white-space: pre;
		}
		.relation-panel :global(.stacked td:nth-child(4)::after) {
			content: var(--unit-4);
			white-space: pre;
		}
		.relation-panel :global(.stacked td:nth-child(5)::after) {
			content: var(--unit-5);
			white-space: pre;
		}
		/* A value the row lacks or the game does not tell has no unit to carry. */
		.relation-panel :global(.stacked td.absent::after) {
			content: none;
		}
	}
	/* A place that several rows share heads them, so their rows do not repeat it. */
	.relation-panel :global(.group-heading th) {
		background: var(--surface-raised);
		color: var(--text-soft);
		font-size: var(--type-0);
		font-weight: var(--weight-strong);
	}
</style>
