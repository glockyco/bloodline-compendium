<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import Search from '#lib/Search.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
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
			<a class="primary" href={resolve('items/')}>Browse All Items</a>
			<a href="https://store.steampowered.com/app/1159290/The_Bloodline/" rel="external"
				>The Bloodline on Steam</a
			>
		</div>
		<ReleaseInfo release={data.release} />
	</div>
</section>

<div class="content">
	<section aria-labelledby="items-heading">
		<div class="section-head">
			<h2 id="items-heading">Items</h2>
			<a href={resolve('items/')}>All {formatNumber(data.count)} items</a>
		</div>
		<ul class="groups">
			{#each data.groups as group (group.slug)}
				<li class="group">
					<a href={resolve(`items/?category=${group.slug}`)}>
						<span class="group-head"
							><h3>{group.name}</h3>
							<span class="count">{formatNumber(group.count)}</span></span
						>
						<span class="group-text">{group.description}</span>
						<span class="slots">
							{#each group.icons as icon (icon)}<span class="slot"
									><img
										src="/icons/{icon}-160.webp"
										alt=""
										width="80"
										height="80"
										loading="lazy"
									/></span
								>{/each}
						</span>
					</a>
				</li>
			{/each}
		</ul>
		{#if data.minor.length}<p class="minor">
				<span>Also</span>
				{#each data.minor as category (category.slug)}<a
						href={resolve(`items/?category=${category.slug}`)}
						>{category.plural} <span class="count">{formatNumber(category.count)}</span></a
					>{/each}
			</p>{/if}
	</section>

	<section aria-labelledby="ways-heading">
		<div class="section-head"><h2 id="ways-heading">How to Get Items</h2></div>
		<ul class="ways">
			{#each data.ways as way (way.key)}
				<li>
					<a
						class="way"
						href={resolve(`items/?${way.kinds.map((kind) => `source=${kind}`).join('&')}`)}
					>
						<span class="stack">
							{#each way.icons as icon (icon)}<span class="slot"
									><img
										src="/icons/{icon}-48.webp"
										alt=""
										width="40"
										height="40"
										loading="lazy"
									/></span
								>{/each}
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
	.primary {
		display: inline-flex;
		align-items: center;
		min-height: var(--size-control);
		padding: 0 var(--space-5);
		border-radius: var(--radius-1);
		background: var(--brand);
		color: var(--text-on-brand);
		font-weight: var(--weight-strong);
		text-decoration: none;
	}
	.primary:hover {
		background: var(--brand-hover);
		color: var(--text-on-brand);
	}

	.content {
		max-width: var(--size-page);
		margin: 0 auto;
		padding: 0 var(--space-gutter);
	}
	.content > section {
		margin: var(--space-section) 0 0;
	}
	.section-head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: var(--space-4);
		margin-bottom: var(--space-5);
	}
	h2 {
		margin: 0;
		font-size: var(--type-4);
		line-height: var(--leading-title);
	}
	ul {
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.groups {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(min(100%, var(--size-card)), 1fr));
		grid-auto-rows: auto auto auto;
		gap: 0 var(--space-4);
	}
	/* Each card spans three shared rows, so headings, descriptions and icon rows line up across a row of cards. */
	.group {
		display: grid;
		grid-row: span 3;
		grid-template-rows: subgrid;
		margin-bottom: var(--space-4);
	}
	.group a {
		display: grid;
		grid-row: span 3;
		grid-template-rows: subgrid;
		gap: var(--space-3);
		padding: var(--space-panel);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-2);
		background: var(--surface-panel);
		color: var(--text);
		text-decoration: none;
		transition:
			border-color var(--duration-hover),
			background-color var(--duration-hover);
	}
	.group a:hover {
		border-color: var(--border-control);
		background: var(--surface-raised);
		color: var(--text);
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
	.count {
		color: var(--text-muted);
		font-variant-numeric: tabular-nums;
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
	.slot {
		display: grid;
		place-items: center;
		aspect-ratio: 1;
		border-radius: var(--radius-1);
		background: var(--surface-slot);
		box-shadow: inset 0 0 0 var(--border-width) var(--slot-edge);
		overflow: hidden;
	}
	.slot img {
		width: 88%;
		height: 88%;
		object-fit: contain;
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
