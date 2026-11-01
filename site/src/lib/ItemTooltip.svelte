<script lang="ts">
	import type { Fact, ItemPage } from './site-data.js';
	import { cardLines, detailRows } from './game-card.js';
	import ItemDetails from './ItemDetails.svelte';
	import OnHitEffects from './OnHitEffects.svelte';

	interface TooltipItem {
		name: string;
		icon: string | null;
		description: string | null;
		facts: Fact[];
		projectileGroups?: ItemPage['projectileGroups'];
		onHitEffects?: ItemPage['onHitEffects'];
	}
	let {
		item,
		heading = 'h2',
		details = true
	}: {
		item: TooltipItem;
		heading?: 'h2' | 'p';
		/** Whether the facts the game's tooltip does not print follow the card, as in link previews. */
		details?: boolean;
	} = $props();

	const lines = $derived(cardLines(item.facts));
	const rows = $derived(details ? detailRows(item.facts, item.projectileGroups) : []);
	// The game shrinks a long name to keep its ribbon on one line. Here it shrinks to body size at most, and a
	// name still too wide for the card wraps instead of running under the notches.
	const nameFit = $derived(26 / Math.max(item.name.length, 1));
	const nameScale = $derived(Math.max(0.8, Math.min(1, nameFit)));
</script>

<div class="tooltip" data-ui="tooltip">
	<div class="card">
		<svelte:element
			this={heading}
			class="name"
			data-game
			data-pagefind-ignore
			style:font-size={nameScale < 1 ? `${nameScale * 1.25}rem` : undefined}
			>{item.name}</svelte:element
		>
		<div class="body" class:bare={!lines.first && !lines.second}>
			<div class="icon">
				{#if item.icon}<img src="/icons/{item.icon}-160.webp" alt="" width="80" height="80" />{/if}
			</div>
			{#if lines.first || lines.second}<div class="lines">
					{#if lines.first}<p class="first" data-game>{lines.first.text}</p>{/if}
					{#if lines.second}<p class="second" data-game>{lines.second.text}</p>{/if}
				</div>{/if}
		</div>
		{#if item.description}<p class="description" data-game>{item.description}</p>{/if}
	</div>
	{#if rows.length || (details && item.projectileGroups?.length)}<ItemDetails
			{rows}
			projectileGroups={item.projectileGroups}
			chargedBodyHitDamage={item.facts.find((fact) => fact.stat === 'chargedBodyHitDamage')?.number}
		/>{/if}
	{#if details}<OnHitEffects effects={item.onHitEffects} />{/if}
</div>

<style>
	.tooltip {
		display: grid;
		gap: var(--gap-block);
	}
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
	.first {
		font-size: var(--type-2);
	}
	.second {
		font-size: var(--type-1);
	}
	.description {
		margin: 0;
		padding: 0 var(--inset-loose);
		font-size: var(--type-2);
		line-height: var(--leading-snug);
	}
</style>
