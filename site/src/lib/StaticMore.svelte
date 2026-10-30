<script lang="ts">
	import { onMount, tick, type Snippet } from 'svelte';
	import { isHydrated } from './hydration.js';
	/**
	 * Entries past a preview. The static page keeps them in a native disclosure, so they stay readable without
	 * JavaScript and reachable for search. Once the page runs, a button replaces it and the entries render only
	 * when a reader asks for them, so a page with hundreds of rows stays light.
	 */
	let {
		more,
		less,
		label,
		children
	}: { more: string; less: string; label: string; children: Snippet } = $props();
	let enhanced = $state(isHydrated());
	let expanded = $state(false);
	let revealed = $state<HTMLElement>();
	onMount(() => {
		enhanced = true;
	});
	// Revealed entries render before the button, so focus moves to the first of them, where reading continues.
	async function toggle() {
		expanded = !expanded;
		if (!expanded) return;
		await tick();
		const target =
			revealed?.querySelector<HTMLElement>('a[href], button, input, select, [tabindex]') ??
			revealed;
		target?.focus();
	}
</script>

{#if enhanced}
	{#if expanded}<div class="revealed" bind:this={revealed} tabindex="-1">
			{@render children()}
		</div>{/if}
	<button type="button" class="more-toggle" aria-expanded={expanded} onclick={toggle}
		>{expanded ? less : more}<span class="sr-only"> in {label}</span></button
	>
{:else}
	<details class="more-entries">
		<summary
			><span class="show-more">{more}</span><span class="show-less">{less}</span><span
				class="sr-only"
			>
				in {label}</span
			></summary
		>
		{@render children()}
	</details>
{/if}

<style>
	.revealed:focus {
		outline: none;
	}
	.more-toggle {
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
