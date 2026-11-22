<script lang="ts">
	/** A card's icon slot, which fills its grid cell, or stays empty when the icon is unknown. */
	let {
		icon
	}: {
		/** The icon's hash, or null to keep the slot empty. */
		icon: string | null;
	} = $props();
</script>

<span class="slot" data-ui="slot"
	>{#if icon}<img
			src="/icons/{icon}-160.webp"
			alt=""
			width="80"
			height="80"
			loading="lazy"
		/>{/if}</span
>

<style>
	.slot {
		display: grid;
		place-items: center;
		aspect-ratio: 1;
		min-width: 0;
		overflow: hidden;
	}
	img {
		width: 88%;
		height: 88%;
		object-fit: contain;
	}
</style>
