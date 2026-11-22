<script lang="ts">
	import type { Snippet } from 'svelte';
	/**
	 * A card in the game's tooltip frame: the name on a ribbon, the icon beside its stat lines, and the description.
	 * Item and skill tooltips fill it with the lines their in-game tooltips print.
	 */
	let {
		name,
		icon,
		lines,
		description,
		heading = 'h2',
		slot = true,
		children
	}: {
		name: string;
		icon: string | null;
		/**
		 * The stat lines in the game's order, each at the size the game prints it. A line the site writes, such as
		 * when a passive unlocks, sets game to false, so it reads in the site's typeface and voice.
		 */
		lines: { text: string; size: 'large' | 'small'; game?: boolean }[];
		description: string | null;
		heading?: 'h2' | 'p';
		/** Whether the card keeps an icon slot, empty when the icon is unknown, as items do. */
		slot?: boolean;
		/** What the card shows between its lines and its description, such as a spellsong's tune. */
		children?: Snippet;
	} = $props();
	// The game shrinks a long name to keep its ribbon on one line. Here it shrinks to body size at most, and a
	// name still too wide for the card wraps instead of running under the notches.
	const nameFit = $derived(26 / Math.max(name.length, 1));
	const nameScale = $derived(Math.max(0.8, Math.min(1, nameFit)));
</script>

<div class="card">
	<svelte:element
		this={heading}
		class="name"
		data-game
		data-pagefind-ignore
		style:font-size={nameScale < 1 ? `${nameScale * 1.25}rem` : undefined}>{name}</svelte:element
	>
	{#if slot || lines.length}<div class="body" class:bare={!lines.length} class:text={!slot}>
			{#if slot}<div class="icon">
					{#if icon}<img src="/icons/{icon}-160.webp" alt="" width="80" height="80" />{/if}
				</div>{/if}
			{#if lines.length}<div class="lines">
					{#each lines as line, index (index)}<p
							class={line.size}
							data-game={line.game === false ? undefined : true}
						>
							{line.text}
						</p>{/each}
				</div>{/if}
		</div>{/if}
	{#if children}<div class="extra">{@render children()}</div>{/if}
	{#if description}<p class="description" data-game>{description}</p>{/if}
</div>

<style>
	/* The game's frame: a thin brown border around a dark, warm gradient. */
	.card {
		display: grid;
		gap: var(--gap-block);
		padding-bottom: var(--inset);
		border: var(--marker-width) solid var(--tooltip-frame);
		border-radius: var(--radius-1);
		background: radial-gradient(var(--tooltip-surface), var(--tooltip-surface-edge));
		box-shadow: var(--shadow-raised);
		color: var(--tooltip-text);
		font-family: var(--font-game);
		overflow: hidden;
	}
	/* The cream ribbon across the top, with dark notches at both ends, holds the name clear of the notches. */
	.name {
		margin: 0;
		padding: var(--inset-line) var(--inset-ribbon);
		background: var(--tooltip-ribbon);
		color: var(--tooltip-ink);
		font-family: var(--font-game);
		font-size: var(--type-2);
		font-weight: var(--weight-regular);
		line-height: var(--leading-snug);
		text-align: center;
		text-wrap: balance;
		clip-path: polygon(
			0 0,
			100% 0,
			calc(100% - var(--size-ribbon-notch)) 50%,
			100% 100%,
			0 100%,
			var(--size-ribbon-notch) 50%
		);
	}
	.body {
		display: grid;
		grid-template-columns: var(--size-icon-title) minmax(0, 1fr);
		align-items: center;
		gap: var(--gap-block);
		padding: 0 var(--inset-loose);
	}
	.body.bare {
		grid-template-columns: var(--size-icon-title);
		justify-content: center;
	}
	.body.text {
		grid-template-columns: minmax(0, 1fr);
	}
	.icon {
		display: grid;
		place-items: center;
		aspect-ratio: 1;
	}
	.icon img {
		width: 100%;
		height: 100%;
		object-fit: contain;
	}
	.lines p {
		margin: 0;
		line-height: var(--leading-snug);
	}
	.large {
		font-size: var(--type-2);
	}
	.small {
		font-size: var(--type-1);
	}
	/* The game sets the description at its stat line's size, so it wraps where the game wraps it. */
	.description {
		margin: 0;
		padding: 0 var(--inset-loose);
		font-size: var(--type-1);
		line-height: var(--leading-snug);
		text-wrap: pretty;
	}
	.extra {
		padding: 0 var(--inset-loose);
	}
</style>
