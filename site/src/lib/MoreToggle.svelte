<script lang="ts">
	import { onMount, tick } from 'svelte';
	import { isHydrated } from './hydration.js';
	/**
	 * The control under a list or table that shows its entries past a preview. The entries stay in the same container,
	 * marked "more", so they line up with the preview's. Rendered right after that container, it is a native
	 * disclosure on the static page, which shows them without JavaScript, and a button once the page runs, after which
	 * the container renders them only when a reader asks.
	 */
	let {
		more,
		less,
		label,
		onshow
	}: {
		more: string;
		less: string;
		label: string;
		/** Tells the container whether to render its extra entries: always on the static page, then when expanded. */
		onshow: (shown: boolean) => void;
	} = $props();
	let enhanced = $state(isHydrated());
	let expanded = $state(false);
	let toggle = $state<HTMLElement>();
	$effect.pre(() => {
		onshow(!enhanced || expanded);
	});
	onMount(() => {
		enhanced = true;
	});
	// Revealed entries render before the button, so focus moves to the first of them, where reading continues. A
	// value with a hint is passed over, because focus would open its hint over the rows.
	async function flip() {
		expanded = !expanded;
		if (!expanded) return;
		await tick();
		toggle?.previousElementSibling
			?.querySelector<HTMLElement>(
				'.more a[href], .more button, .more input, .more select, .more [tabindex]:not(.hinted)'
			)
			?.focus();
	}
</script>

{#if enhanced}<button
		bind:this={toggle}
		type="button"
		class="more-toggle"
		aria-expanded={expanded}
		onclick={flip}>{expanded ? less : more}<span class="sr-only"> in {label}</span></button
	>{:else}<details class="more-entries">
		<summary
			><span class="show-more">{more}</span><span class="show-less">{less}</span><span
				class="sr-only"
			>
				in {label}</span
			></summary
		>
	</details>{/if}

<style>
	.more-toggle {
		justify-self: start;
		min-height: var(--size-control);
		padding: var(--inset);
		border: 0;
		background: none;
		color: var(--accent);
	}
	.more-toggle:hover {
		text-decoration: underline;
	}
</style>
