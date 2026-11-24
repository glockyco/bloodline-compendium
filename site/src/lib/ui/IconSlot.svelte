<script lang="ts">
	/**
	 * An icon in a slot like the game's inventory slots, as tiles and search results show one. It keeps its size when
	 * the icon is unknown, so rows line up.
	 */
	let {
		icon,
		size = 'tile'
	}: {
		/** The icon's hash, or null to keep the slot empty. */
		icon: string | null;
		/** A small row slot, a list row's readable one, one as tall as a name and the line under it, or a tile's. */
		size?: 'row' | 'list' | 'pair' | 'tile';
	} = $props();
</script>

<span class="slot {size}" data-ui="slot"
	>{#if icon}<img
			src="/icons/{icon}-{size === 'list' ? 48 : 160}.webp"
			alt=""
			width="48"
			height="48"
			loading="lazy"
		/>{/if}</span
>

<style>
	.slot {
		display: grid;
		flex: none;
		place-items: center;
		border: var(--marker-width) solid var(--slot-edge);
		border-radius: var(--radius-1);
		background: var(--surface-slot);
	}
	.row {
		width: var(--size-icon-row);
		height: var(--size-icon-row);
	}
	/* A list holds hundreds of rows, so its slots load the small icon. */
	.list {
		width: var(--size-icon-list);
		height: var(--size-icon-list);
	}
	.pair {
		width: var(--size-icon-pair);
		height: var(--size-icon-pair);
	}
	.tile {
		width: var(--size-icon-tile);
		height: var(--size-icon-tile);
	}
	img {
		width: 88%;
		height: 88%;
		object-fit: contain;
	}
</style>
