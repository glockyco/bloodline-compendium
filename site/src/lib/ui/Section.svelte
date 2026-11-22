<script lang="ts">
	import type { Snippet } from 'svelte';
	import Count from './Count.svelte';
	import LinkedText from './LinkedText.svelte';
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
		/** One sentence that states what every row shares. It may link concepts as text written for the site does. */
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
						class="heading-end">{title.slice(lastSpace + 1)}&nbsp;<Count value={count} /></span
					>{/if}
			</h2>
			{#if action}<div class="action">{@render action()}</div>{/if}
		</div>
		{#if line}<p class="line"><LinkedText text={line} /></p>{/if}
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
	.action {
		margin-left: auto;
	}
	/* An intro wraps so its last line never holds a word or two alone. */
	.line {
		margin: 0;
		color: var(--text-muted);
		text-wrap: pretty;
	}
	.body {
		display: grid;
		gap: var(--gap-block);
		min-width: 0;
	}
</style>
