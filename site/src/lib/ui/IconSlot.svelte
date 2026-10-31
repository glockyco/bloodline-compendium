<script lang="ts">
	let {
		icon,
		size = 'card'
	}: {
		/** The icon's hash, or null to keep the slot empty. */
		icon: string | null;
		/** Card slots fill their grid cell, stack slots have the fixed small size. */
		size?: 'card' | 'stack';
	} = $props();
	const file = $derived(size === 'card' ? 160 : 48);
</script>

<span class="slot {size}" data-ui="slot"
	>{#if icon}<img
			src="/icons/{icon}-{file}.webp"
			alt=""
			width={size === 'card' ? 80 : 40}
			height={size === 'card' ? 80 : 40}
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
	.stack {
		width: var(--size-icon-stack);
	}
	img {
		width: 88%;
		height: 88%;
		object-fit: contain;
	}
</style>
