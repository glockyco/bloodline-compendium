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
		/** A small row slot, a list row's readable one, or a tile's larger one. */
		size?: 'row' | 'list' | 'tile';
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
