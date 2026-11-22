<script lang="ts">
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { SvelteSet } from 'svelte/reactivity';
	import SeoHead from '#lib/SeoHead.svelte';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import Section from '#lib/ui/Section.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { formatNumber, plural } from '#lib/format.js';
	let { data } = $props();
	const profession = $derived(data.profession);
	const kind = $derived(data.kinds.find((entry) => entry.kind === profession.kind)!);
	const listPath = $derived(kindPath(profession.kind, data.kinds)!);
	const reference = $derived({
		kind: profession.kind,
		key: profession.key,
		name: profession.name,
		slug: profession.slug,
		icon: null
	});
	const groups = $derived(data.groups);
	const total = $derived(data.total);
	const leveled = $derived(groups.some((group) => group.level != null));
	const title = $derived(profession.farming ? 'Crops' : 'Recipes');
	const levels = $derived(groups.map((group) => group.level).filter((level) => level != null));
	// A phone shows each level as one row until a reader opens it, since forty levels of items would not fit a
	// glance. The static page and wider screens show every level open.
	const collapsed = new SvelteSet<number | null>();
	onMount(() => {
		if (leveled && matchMedia('(width < 40rem)').matches)
			for (const group of groups) collapsed.add(group.level);
	});
	function toggle(level: number | null) {
		if (collapsed.has(level)) collapsed.delete(level);
		else collapsed.add(level);
	}
	const description = $derived(
		`${profession.name} in The Bloodline: ${plural(total, 'item', 'items')} it gives` +
			(leveled ? ', by the level each needs.' : '.')
	);
</script>

<SeoHead
	title={profession.name + ' Profession | The Bloodline Compendium'}
	{description}
	entity={reference}
	{kind}
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Professions', path: listPath },
		{ name: profession.name, path: entityPath(reference, data.kinds)! }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li><a href={listPath}>Professions</a></li>
		<li aria-current="page">{profession.name}</li>
	</ol>
</nav>
<EntityArticle {kind} name={profession.name} icon={groups[0]?.items[0]?.icon ?? null} detail={null}>
	<header class="item-heading icon-heading">
		<IconSlot icon={groups[0]?.items[0]?.icon ?? null} />
		<div>
			<h1>{profession.name}</h1>
			<dl class="facts">
				{#if profession.stations.length}<div>
						<dt>Made at</dt>
						<dd>
							{#each profession.stations as station, index (station.key)}{index
									? ', '
									: ''}<EntityLink entity={station} />{/each}
						</dd>
					</div>{/if}
				{#if levels.length}<div>
						<dt>Levels</dt>
						<dd>{formatNumber(levels[0]!)} to {formatNumber(levels.at(-1)!)}</dd>
					</div>{/if}
			</dl>
		</div>
	</header>
	<div class="item-relations" data-pagefind-ignore="all">
		<Section
			id="recipes"
			{title}
			count={total}
			line={leveled ? 'What each level lets you make.' : undefined}
			>{#snippet action()}<a href={data.all}>All {formatNumber(total)} in the item list</a
				>{/snippet}
			<ol class="level-groups">
				{#each groups as group (group.level ?? 'none')}<li
						class="level-group"
						class:unleveled={!leveled}
					>
						{#if leveled}<div class="level">
								<span class="level-word">Level</span><span class="level-number"
									>{group.level == null ? 'None' : formatNumber(group.level)}</span
								>
								<span class="level-count">{plural(group.count, 'item', 'items')}</span>
								<button
									type="button"
									class="level-toggle"
									aria-expanded={!collapsed.has(group.level)}
									aria-controls={'level-' + (group.level ?? 'none')}
									onclick={() => toggle(group.level)}
									>{collapsed.has(group.level) ? 'Show' : 'Hide'}<span class="sr-only">
										level {group.level ?? 'none'} items</span
									></button
								>
							</div>{/if}
						<ul
							class="chips"
							id={'level-' + (group.level ?? 'none')}
							hidden={collapsed.has(group.level)}
						>
							{#each group.items as item (item.key)}<li>
									<EntityLink entity={{ ...item, kind: 'item' }} icon />
								</li>{/each}
							{#if group.url && group.count > group.items.length}<li>
									<a class="more-chip" href={group.url}
										>{formatNumber(group.count - group.items.length)} more in the item list</a
									>
								</li>{/if}
						</ul>
					</li>{/each}
			</ol>
		</Section>
	</div>
</EntityArticle>

<style>
	h1 {
		margin: 0;
	}
	.facts {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-line) var(--gap-groups);
		margin: var(--gap-line) 0 0;
	}
	.facts div {
		display: flex;
		gap: var(--gap-inline);
	}
	.facts dt {
		color: var(--text-muted);
	}
	.facts dd {
		margin: 0;
	}
	/* One panel, a row per level, the level's badge beside the items it unlocks, as a tier list reads. */
	.level-groups {
		display: grid;
		margin: 0;
		padding: 0;
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-2);
		background: var(--surface-panel);
		list-style: none;
	}
	.level-group {
		display: grid;
		grid-template-columns: var(--size-level-badge) minmax(0, 1fr);
		gap: var(--gap-block);
		padding: var(--inset-cell) var(--inset);
	}
	.level-group + .level-group {
		border-top: var(--border-width) solid var(--border);
	}
	.level {
		display: grid;
		align-content: start;
		justify-items: center;
		gap: var(--gap-line);
		padding: var(--inset-cell) 0;
		text-align: center;
	}
	.level-word {
		color: var(--text-muted);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
	}
	.level-number {
		font-family: var(--font-display);
		font-size: var(--type-section);
		line-height: var(--leading-heading);
	}
	.level-count {
		color: var(--text-muted);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
	}
	/* Only a phone collapses levels, so only a phone shows their toggles. */
	.level-toggle {
		display: none;
	}
	.chips {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(var(--size-chip-name), 1fr));
		gap: var(--gap-line) var(--gap-block);
		align-content: start;
		margin: 0;
		padding: var(--inset-cell) 0;
		list-style: none;
	}
	/* A profession whose recipes need no level shows its items without a badge. */
	.level-group.unleveled {
		grid-template-columns: minmax(0, 1fr);
	}
	.chips[hidden] {
		display: none;
	}
	/* The link to a level's other items sits in the grid as one more entry. */
	.more-chip {
		display: inline-flex;
		align-items: center;
		min-height: var(--size-icon-inline);
		font-weight: var(--weight-strong);
	}
	@media (width < 40rem) {
		/* A phone gives each level one band, as a table's group heading, with its items below when open. */
		.level-group {
			grid-template-columns: minmax(0, 1fr);
			gap: 0;
			padding: 0;
		}
		.level {
			display: flex;
			align-items: center;
			gap: var(--gap-inline);
			padding: var(--inset-cell) var(--inset);
			background: var(--surface-raised);
		}
		.level-number {
			font-size: var(--type-sub);
		}
		.level-count {
			margin-left: auto;
		}
		.chips {
			padding: var(--inset-cell) var(--inset);
		}
		/* The toggle reads as the site's other Show links: accent text without a box, the same width open or shut. */
		.level-toggle {
			display: inline-flex;
			align-items: center;
			justify-content: flex-end;
			min-width: var(--size-toggle);
			min-height: var(--size-control-compact);
			padding: 0;
			border: 0;
			background: none;
			color: var(--accent);
		}
		.level-toggle:hover {
			text-decoration: underline;
		}
		.level-group.unleveled .chips {
			grid-template-columns: repeat(auto-fill, minmax(var(--size-tile), 1fr));
		}
	}
</style>
