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
</script>

<section class={['section', className]} aria-labelledby={id}>
	<header>
		<div class="heading">
			<h2 {id}>
				{title}{#if count !== undefined}<span class="count">{formatNumber(count)}</span>{/if}
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
		gap: var(--gap-block);
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
		display: flex;
		align-items: baseline;
		gap: var(--gap-inline);
		margin: 0;
		font-size: var(--type-2);
		line-height: var(--leading-title);
	}
	.count {
		color: var(--text-muted);
		font-family: var(--font-body);
		font-size: var(--type-0);
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
