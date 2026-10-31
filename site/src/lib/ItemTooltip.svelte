<script lang="ts">
	import { resolve } from '$app/paths';
	import type { Fact } from './site-data.js';
	import { titleCase } from './labels.js';
	import { factValue } from './stats.js';

	interface TooltipItem {
		name: string;
		icon: string | null;
		description: string | null;
		facts: Fact[];
	}
	let {
		item,
		heading = 'h2'
	}: {
		item: TooltipItem;
		heading?: 'h2' | 'p';
	} = $props();

	const find = (stat: string) => item.facts.find((fact) => fact.stat === stat);
	// The game's tooltip has two stat lines beside the icon: a weapon's speed word and its damage, or an armor
	// piece's class and its armor. They read as the game prints them, with the value the game applies.
	const lines = $derived.by(() => {
		const speed = find('speed');
		const damage = find('damage');
		const type = find('armorType');
		const armor = find('armorRating');
		if (speed || damage)
			return {
				first: speed ? { fact: speed, text: speed.text ?? 'Unknown' } : null,
				second: damage ? { fact: damage, text: `Damage: ${factValue(damage) ?? 'Unknown'}` } : null
			};
		if (type || armor)
			return {
				first:
					type && type.text !== 'Unarmored' ? { fact: type, text: `Type: ${type.text}` } : null,
				second: armor ? { fact: armor, text: `ARMOR: ${factValue(armor) ?? 'Unknown'}` } : null
			};
		return { first: null, second: null };
	});
	const native = $derived(new Set([lines.first?.fact, lines.second?.fact]));
	// Facts the game's tooltip does not print follow the game card, in the site's own type.
	const extra = $derived(item.facts.filter((fact) => !native.has(fact)));
	const misstated = $derived(item.facts.filter((fact) => fact.gameShows));
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
	{#if misstated.length}<p class="game-shows">
			{#each misstated as fact (fact.stat)}<span
					>{#if fact.gameShowsIssue}<a href={resolve(`known-issues/#${fact.gameShowsIssue}`)}
							>The game's tooltip shows {fact.tooltipLabel ?? fact.label} {fact.gameShows}</a
						>{:else}The game's tooltip shows {fact.tooltipLabel ?? fact.label}
						{fact.gameShows}{/if}</span
				>{/each}
		</p>{/if}
	{#if extra.length}<dl class="extra">
			{#each extra as fact (fact.stat + fact.label)}<div class:prose={fact.number == null}>
					<dt>{titleCase(fact.label)}</dt>
					<dd>{factValue(fact) ?? 'Unknown'}</dd>
				</div>{/each}
		</dl>{/if}
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
	.game-shows {
		display: grid;
		gap: var(--gap-line);
		margin: 0;
		color: var(--text-soft);
	}
	.extra {
		display: grid;
		gap: var(--gap-line);
		margin: 0;
	}
	/* A label and its value share a row, and a long value wraps in its own track on the right instead of under
	   the label. */
	.extra div {
		display: grid;
		grid-template-columns: max-content minmax(0, 1fr);
		align-items: baseline;
		gap: var(--gap-block);
	}
	.extra dt {
		color: var(--text-soft);
	}
	.extra dd {
		margin: 0;
		text-align: right;
		font-variant-numeric: tabular-nums;
	}
	/* A worded value, such as an effect, reads left to right from its own edge instead of hugging the right. */
	.extra .prose dd {
		text-align: left;
	}
</style>
