<script lang="ts" generics="T">
	import type { Snippet } from 'svelte';
	import MoreToggle from './MoreToggle.svelte';
	import { isHydrated } from './hydration.js';
	let {
		items,
		label,
		headings,
		numeric = true,
		stackOnMobile = true,
		stackPairs = false,
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
		/** Whether a two-column table stacks on a phone too, when its first column holds long text, such as a description. */
		stackPairs?: boolean;
		/** Whether a long table shows its first rows behind a button, which a table read as a whole turns off. */
		collapse?: boolean;
		row: Snippet<[T, number]>;
	} = $props();
	const preview = $derived(collapse && items.length > 10 ? 8 : items.length);
	// Rows past the preview stay in the same table, so their columns line up with the rows above.
	// The static page carries every entry, so a reader without JavaScript can open them.
	let shown = $state(!isHydrated());
	const parts = $derived(
		headings.map((heading) =>
			typeof heading === 'string' ? { label: heading, unit: null } : heading
		)
	);
	// By default, a phone stacks tables with three or more columns, labelling each value after the name. With no
	// heading above them, a value carries its column's unit after it, as the game writes "4 seconds". A column
	// without a unit defines no unit, so its cells have no empty unit that would take a gap beside the value.
	const labels = $derived(
		parts
			.slice(1)
			.flatMap((heading, index) => [
				`--label-${index + 2}: ${JSON.stringify(heading.label)}`,
				...(heading.unit
					? [
							`--unit-${index + 2}: ${JSON.stringify(' ' + heading.unit)}`,
							// A cell holding exactly 1, which marks itself "one", reads the unit in the singular.
							`--unit-one-${index + 2}: ${JSON.stringify(' ' + heading.unit.replace(/s$/, ''))}`
						]
					: [])
			])
			.join('; ')
	);
</script>

<div class="relation-panel">
	<table
		class="relation-table"
		class:stacked={stackOnMobile && (headings.length > 2 || (stackPairs && headings.length === 2))}
		aria-label={label}
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
			>{#each items.slice(0, preview) as item, index (index)}{@render row(
					item,
					index
				)}{/each}</tbody
		>
		{#if items.length > preview && shown}<tbody class="more"
				>{#each items.slice(preview) as item, index (index)}{@render row(
						item,
						index + preview
					)}{/each}</tbody
			>{/if}
	</table>
	{#if items.length > preview}<MoreToggle
			more={`Show ${items.length - preview} more`}
			less="Show fewer"
			{label}
			onshow={(value) => (shown = value)}
		/>{/if}
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
	/* The panel clips what leaves it, so a hint in the last rows opens upward. */
	.relation-panel :global(tbody tr:nth-last-child(-n + 2) .hint) {
		inset-block-start: auto;
		inset-block-end: var(--position-below);
		margin-block: 0 var(--gap-line);
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
	/* The rows past the preview continue the rows above, divided as they are. */
	.relation-panel :global(tbody.more tr:first-child :is(th, td)) {
		border-top: var(--border-width) solid var(--border);
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
	/* A name with its icon opening a cell is a row of its own, so no text line adds space under it, and the lines
	   that follow start under the name's text, where they read as part of it. */
	.relation-panel :global(td > .with-icon:first-child) {
		display: flex;
		width: fit-content;
	}
	.relation-panel :global(td > .with-icon:first-child ~ :is(small, .relation-conditions)) {
		padding-left: var(--inset-icon-text);
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
		/* A long heading such as "Chance per Item Gathered" wraps between its words, so it leaves the row's name its
		   width. */
		.relation-panel :global(thead .numeric) {
			white-space: normal;
		}
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
		.relation-panel :global(.stacked tbody tr + tr),
		.relation-panel :global(.stacked tbody.more tr:first-child) {
			border-top: var(--border-width) solid var(--border);
		}
		.relation-panel :global(.stacked tr > :is(th, td)) {
			padding: 0 var(--inset);
			border: 0;
		}
		.relation-panel :global(.stacked tr > :first-child) {
			flex-basis: 100%;
		}
		/* A group heading is one band from edge to edge: the row carries no padding and its cell keeps a cell's own. */
		.relation-panel :global(.stacked tr.group-heading) {
			padding: 0;
		}
		.relation-panel :global(.stacked tr.group-heading > th) {
			padding: var(--inset-cell) var(--inset);
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
		/* A unit follows its value after one space, not the gap between the label and the value. */
		.relation-panel :global(.stacked td:nth-child(n + 2)::after) {
			margin-left: calc(-1 * var(--gap-inline));
		}
		.relation-panel :global(.stacked td.one:nth-child(2)::after) {
			content: var(--unit-one-2);
		}
		.relation-panel :global(.stacked td.one:nth-child(3)::after) {
			content: var(--unit-one-3);
		}
		.relation-panel :global(.stacked td.one:nth-child(4)::after) {
			content: var(--unit-one-4);
		}
		.relation-panel :global(.stacked td.one:nth-child(5)::after) {
			content: var(--unit-one-5);
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
