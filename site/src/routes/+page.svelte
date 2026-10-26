<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import Search from '#lib/Search.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	import { formatNumber } from '#lib/format.js';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid from '#lib/ui/CardGrid.svelte';
	import Count from '#lib/ui/Count.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import SectionHeading from '#lib/ui/SectionHeading.svelte';
	import { kindPath } from '#lib/entities.js';
	let { data } = $props();
	const itemsPath = $derived(kindPath('item', data.kinds)!);
	const unknown = $derived(data.count - data.known);
</script>

<SeoHead
	title="The Bloodline Compendium | Item Wiki and Reference"
	description="Every item in The Bloodline with its sources, recipes and uses, read from the game's own files. An unofficial reference."
	website
/>

<section class="hero" aria-labelledby="title">
	<div class="art">
		<img
			src="/art/hero-1920.webp"
			srcset="/art/hero-1280.webp 1280w, /art/hero-1920.webp 1920w, /art/hero-2560.webp 2560w, /art/hero-3840.webp 3840w"
			sizes="100vw"
			width="3840"
			height="1240"
			alt=""
			fetchpriority="high"
		/>
	</div>
	<div class="hero-body">
		<h1 id="title">The Bloodline Compendium</h1>
		<p class="lede">
			All {formatNumber(data.count)} items in The Bloodline, with their sources, recipes and uses.
		</p>
		<div class="hero-search">
			<Search variant="hero" placeholder="Search items, for example {data.example}" />
		</div>
		<div class="actions">
			<ButtonLink href={itemsPath}>Browse All Items</ButtonLink>
			<a href="https://store.steampowered.com/app/1159290/The_Bloodline/" rel="external"
				>The Bloodline on Steam</a
			>
		</div>
		<ReleaseInfo release={data.release} />
	</div>
</section>

<div class="content">
	<section aria-labelledby="items-heading">
		<SectionHeading
			id="items-heading"
			title="Items"
			link={{ href: itemsPath, label: `All ${formatNumber(data.count)} items` }}
		/>
		<CardGrid rows={3}>
			{#each data.groups as group (group.slug)}
				<Card href={kindPath('item', data.kinds, `?category=${group.slug}`)!}>
					<span class="group-head"
						><h3>{group.name}</h3>
						<Count value={group.count} /></span
					>
					<span class="group-text">{group.description}</span>
					<!-- Five slots in every card, empty where a category has fewer icons, so rows keep one rhythm. -->
					<span class="slots" data-ui="slots">
						{#each [0, 1, 2, 3, 4] as slot (slot)}<IconSlot
								icon={group.icons[slot] ?? null}
							/>{/each}
					</span>
				</Card>
			{/each}
		</CardGrid>
		{#if data.minor.length}<p class="minor">
				<span>Also</span>
				{#each data.minor as category (category.slug)}<a
						href={kindPath('item', data.kinds, `?category=${category.slug}`)!}
						>{category.plural} <Count value={category.count} /></a
					>{/each}
			</p>{/if}
	</section>

	<section aria-labelledby="ways-heading">
		<SectionHeading id="ways-heading" title="How to Get Items" />
		<ul class="ways">
			{#each data.ways as way (way.key)}
				<li>
					<a
						class="way"
						href={kindPath(
							'item',
							data.kinds,
							`?${way.kinds.map((kind) => `source=${kind}`).join('&')}`
						)!}
					>
						<span class="stack">
							{#each way.icons as icon (icon)}<IconSlot {icon} size="stack" />{/each}
						</span>
						<span class="way-text"><b>{way.label}</b><span>{way.text}</span></span>
						<span class="way-count">{formatNumber(way.count)}<small>items</small></span>
					</a>
				</li>
			{/each}
		</ul>
	</section>

	<section class="edition" aria-labelledby="edition-heading">
		<div>
			<h2 id="edition-heading">{data.release.title}</h2>
			<p>
				This edition describes the game as patched on {new Intl.DateTimeFormat('en', {
					dateStyle: 'long',
					timeZone: 'UTC'
				}).format(new Date(data.release.patchDate))}. Items, sources and facts are read from the
				game's files.
			</p>
			<p class="links">
				<a href={data.release.patchNotesUrl} rel="external">Patch Notes</a>
				<a href={resolve('about/')}>How the Compendium Is Made</a>
			</p>
		</div>
		<div>
			<p>
				<strong>{formatNumber(data.known)}</strong> of {formatNumber(data.count)} items have a known source.
			</p>
			<div
				class="meter"
				role="img"
				aria-label="{formatNumber(data.known)} of {formatNumber(
					data.count
				)} items have a known source"
			>
				<span style:width="{(data.known / Math.max(data.count, 1)) * 100}%"></span>
			</div>
			{#if unknown}<p>
					<a href={resolve('coverage/#itemWithoutSource')}
						>{formatNumber(unknown)} items without a known source</a
					>
				</p>{/if}
		</div>
	</section>
</div>

<style>
	.hero {
		position: relative;
		margin: 0;
	}
	.art {
		position: relative;
		aspect-ratio: 3.1 / 1;
		min-height: var(--size-hero-min);
		max-height: var(--size-hero-max);
		overflow: hidden;
		background: var(--surface-raised);
	}
	.art img {
		display: block;
		width: 100%;
		height: 100%;
		object-fit: cover;
		object-position: 50% 30%;
	}
	.art::after {
		content: '';
		position: absolute;
		inset: auto 0 0;
		height: 40%;
		background: linear-gradient(var(--surface-fade), var(--surface) 96%);
	}
	.hero-body {
		position: relative;
		max-width: var(--size-page);
		margin: calc(-1 * var(--space-8)) auto 0;
		padding: 0 var(--space-gutter);
	}
	h1 {
		margin: 0;
		font-size: var(--type-6);
		line-height: var(--leading-display);
		text-wrap: balance;
		text-shadow: var(--shadow-text);
	}
	.lede {
		max-width: var(--size-lede);
		margin: var(--space-4) 0 var(--space-6);
		font-size: var(--type-2);
		line-height: var(--leading-snug);
		color: var(--text-soft);
	}
	.hero-search {
		max-width: var(--size-hero-search);
	}
	.actions {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-4) var(--space-5);
		margin: var(--space-5) 0;
	}

	.content {
		max-width: var(--size-page);
		margin: 0 auto;
		padding: 0 var(--space-gutter);
	}
	.content > section {
		margin: var(--space-section) 0 0;
	}
	ul {
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.group-head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: var(--space-4);
	}
	h3 {
		margin: 0;
		font-size: var(--type-3);
		line-height: var(--leading-title);
	}
	.group-text {
		color: var(--text-muted);
		line-height: var(--leading-snug);
	}
	.slots {
		display: grid;
		grid-template-columns: repeat(5, minmax(0, 1fr));
		gap: var(--space-2);
		align-self: end;
	}
	.minor {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2) var(--space-5);
		margin: var(--space-2) 0 0;
		color: var(--text-muted);
	}

	.ways {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(min(100%, var(--size-way)), 1fr));
		column-gap: var(--space-7);
	}
	.way {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr) auto;
		align-items: center;
		gap: var(--space-4);
		padding: var(--space-3) 0;
		border-bottom: var(--border-width) solid var(--border);
		color: var(--text);
		text-decoration: none;
	}
	.way:hover b {
		text-decoration: underline;
	}
	.stack {
		display: grid;
		grid-template-columns: repeat(3, var(--size-icon-stack));
		gap: var(--space-1);
	}
	.way-text {
		display: grid;
	}
	.way-text b {
		font-weight: var(--weight-strong);
	}
	.way-text span {
		color: var(--text-muted);
	}
	.way-count {
		font-family: var(--font-heading);
		font-size: var(--type-3);
		line-height: var(--leading-display);
		text-align: right;
		font-variant-numeric: tabular-nums;
	}
	.way-count small {
		display: block;
		margin-top: var(--space-1);
		font-family: var(--font-body);
		font-size: var(--type-1);
		color: var(--text-muted);
	}

	.edition {
		display: grid;
		grid-template-columns: minmax(0, 1.3fr) minmax(0, 1fr);
		gap: var(--space-7);
		padding: var(--space-6);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-2);
		background: var(--surface-panel);
	}
	.edition h2 {
		font-size: var(--type-3);
		margin-bottom: var(--space-3);
	}
	.edition p {
		margin: 0 0 var(--space-3);
		color: var(--text-soft);
	}
	.links {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2) var(--space-5);
	}
	.meter {
		height: var(--size-meter);
		margin: var(--space-1) 0 var(--space-4);
		border-radius: var(--radius-round);
		background: var(--surface-slot);
		overflow: hidden;
	}
	.meter span {
		display: block;
		height: 100%;
		border-radius: var(--radius-round);
		background: var(--known);
	}

	@media (width < 64rem) {
		.art {
			aspect-ratio: auto;
			height: var(--size-hero-phone);
		}
		.art img {
			object-position: 42% 30%;
		}
		.hero-body {
			margin-top: calc(-1 * var(--space-7));
		}
		.edition {
			grid-template-columns: minmax(0, 1fr);
			gap: var(--space-4);
			padding: var(--space-panel);
		}
	}
</style>
