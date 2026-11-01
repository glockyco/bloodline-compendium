<script lang="ts">
	import type { Snippet } from 'svelte';
	import { formatNumber } from '../format.js';
	/**
	 * A section of a page, as in Afallon: a heading with its count and one optional action above one body. The body
	 * spaces its blocks by the block gap, and sections space each other by the section gap of their container.
	 */
	let {
		id,
		title,
		count,
		line,
		action,
		class: className,
		children
	}: {
		id: string;
		title: string;
		count?: number;
		/** One sentence that states what every row shares. */
		line?: string;
		action?: Snippet;
		class?: string;
		children: Snippet;
	} = $props();
	const lastSpace = $derived(title.lastIndexOf(' '));
</script>

<section class={['section', className]} aria-labelledby={id}>
	<header>
		<div class="heading">
			<h2 {id}>
				{#if count === undefined}{title}{:else}{title.slice(0, lastSpace + 1)}<span
						class="heading-end"
						>{title.slice(lastSpace + 1)}&nbsp;<span class="count">{formatNumber(count)}</span
						></span
					>{/if}
			</h2>
			{#if action}<div class="action">{@render action()}</div>{/if}
		</div>
		{#if line}<p class="line">{line}</p>{/if}
	</header>
	<div class="body">{@render children()}</div>
</section>

<style>
	.section {
		display: grid;
		gap: var(--gap-heading);
		min-width: 0;
		margin: 0;
	}
	header {
		display: grid;
		gap: var(--gap-line);
	}
	.heading {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		justify-content: space-between;
		gap: var(--gap-inline);
	}
	h2 {
		margin: 0;
		font-size: var(--type-section);
		line-height: var(--leading-heading);
	}
	.heading-end {
		white-space: nowrap;
	}
	.count {
		color: var(--text-muted);
		font-family: var(--font-body);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
		font-variant-numeric: tabular-nums;
	}
	.action {
		margin-left: auto;
	}
	.line {
		margin: 0;
		color: var(--text-muted);
	}
	.body {
		display: grid;
		gap: var(--gap-block);
		min-width: 0;
	}
</style>
