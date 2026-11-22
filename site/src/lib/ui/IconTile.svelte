<script lang="ts">
	import IconSlot from './IconSlot.svelte';
	/**
	 * A compact link to a list or page: an icon in its slot, the name and one muted line such as a count, as the
	 * sibling compendiums' category tiles read.
	 */
	let {
		href,
		icon,
		name,
		meta,
		game = false
	}: {
		href: string;
		icon: string | null;
		name: string;
		meta?: string;
		/** Whether the name is the game's own, such as a discipline's, which reads in the game's typeface. */
		game?: boolean;
	} = $props();
</script>

<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- callers pass hrefs they resolved. -->
<a class="tile" {href}>
	<IconSlot {icon} />
	<span class="text">
		<span class="name" data-game={game || undefined}>{name}</span>
		{#if meta}<span class="meta">{meta}</span>{/if}
	</span>
</a>

<style>
	/* Tiles of one row share its height, as their names and lines wrap differently. */
	.tile {
		display: grid;
		height: 100%;
		grid-template-columns: auto minmax(0, 1fr);
		align-items: start;
		gap: var(--gap-block);
		padding: var(--inset-cell);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-2);
		background: var(--surface-panel);
		color: var(--text);
		text-decoration: none;
		transition:
			border-color var(--duration-hover),
			background-color var(--duration-hover);
	}
	.tile:hover {
		border-color: var(--border-control);
		background: var(--surface-raised);
		color: var(--text);
	}
	.text {
		display: grid;
		min-width: 0;
	}
	.name {
		font-weight: var(--weight-strong);
		overflow-wrap: anywhere;
		text-wrap: balance;
	}
	.meta {
		color: var(--text-muted);
		text-wrap: pretty;
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
	}
</style>
