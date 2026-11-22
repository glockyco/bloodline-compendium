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
		/** A row's slot, as in search results, or a tile's larger one. */
		size?: 'row' | 'tile';
	} = $props();
</script>

<span class="slot {size}" data-ui="slot"
	>{#if icon}<img
			src="/icons/{icon}-160.webp"
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
