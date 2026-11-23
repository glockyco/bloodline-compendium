<script lang="ts">
	import { onMount } from 'svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid from '#lib/ui/CardGrid.svelte';
	import { page } from '$app/state';
	import { browseKinds } from '#lib/entities.js';
	import { searchPages, type SearchResult } from '#lib/search.js';
	/**
	 * The page for an address with nothing at it. The address usually names what the reader wanted, such as
	 * /items/knights-broadsword/, so the page searches for its words and offers the best matches, preferring the kind
	 * the address names, as cards like the list pages, and then every list to browse.
	 */
	const browse = $derived(browseKinds(page.data.kinds));
	let wanted = $state('');
	let matches: SearchResult[] = $state([]);
	onMount(async () => {
		const parts = location.pathname.split('/').filter(Boolean);
		const last = decodeURIComponent(parts.at(-1) ?? '');
		wanted = last
			.replace(/\.html?$/, '')
			.replace(/[-_+]+/g, ' ')
			.trim();
		if (!wanted) return;
		const kind = parts.length > 1 ? `/${parts[0]}/` : null;
		const found = (await searchPages(wanted, 12)) ?? [];
		// A result of the kind the address names comes first, as /items/ asked for an item.
		matches = found
			.map((result, index) => ({ result, index }))
			.sort(
				(a, b) =>
					Number(kind !== null && b.result.url.startsWith(kind)) -
						Number(kind !== null && a.result.url.startsWith(kind)) || a.index - b.index
			)
			.slice(0, 6)
			.map(({ result }) => result);
	});
</script>

<SeoHead
	title="Page not found | The Bloodline Compendium"
	description="This page does not exist."
	noindex
/>
<div class="sections missing">
	<header class="page-heading">
		<h1>Page Not Found</h1>
		<p class="intro">
			Nothing is at this address. The page may have moved, or the address may have a typo.
		</p>
	</header>
	{#if matches.length}<section class="matches" aria-labelledby="matches">
			<h2 id="matches">Possible Matches</h2>
			<CardGrid rows={2} columns={3} label="Possible matches">
				{#each matches as match (match.url)}<Card href={match.url}
						><span class="card-head"><IconSlot icon={match.icon} />{match.title}</span><span
							class="match-line"
							>{match.detail ? `${match.kind} · ${match.detail}` : match.kind}</span
						></Card
					>{/each}
			</CardGrid>
		</section>{/if}
	<section class="matches" aria-labelledby="browse">
		<h2 id="browse">Browse</h2>
		<CardGrid rows={1} columns={3} label="Browse">
			{#each browse as entry (entry.path)}<Card href={entry.path}
					><span class="card-head">{entry.label}</span></Card
				>{/each}
		</CardGrid>
	</section>
</div>

<style>
	.missing {
		max-width: var(--size-measure);
	}
	.missing > :global(*) {
		min-width: 0;
	}
	.matches {
		display: grid;
		grid-template-columns: minmax(0, 1fr);
		gap: var(--gap-heading);
	}
	.matches h2 {
		margin: 0;
		font-size: var(--type-section);
		line-height: var(--leading-heading);
	}
	.match-line {
		color: var(--text-muted);
		font-size: var(--type-meta);
	}
	.card-head {
		display: flex;
		align-items: center;
		gap: var(--gap-block);
		font-size: var(--type-sub);
	}
</style>
