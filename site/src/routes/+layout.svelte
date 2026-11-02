<script lang="ts">
	import { resolve } from '$app/paths';
	import { afterNavigate } from '$app/navigation';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import '@fontsource/young-serif/400.css';
	import '@fontsource/arbutus-slab/400.css';
	import '@fontsource-variable/figtree';
	import '../app.css';
	import Search from '#lib/Search.svelte';
	import EntityPreviews from '#lib/EntityPreviews.svelte';
	import { markHydrated } from '#lib/hydration.js';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	import CompendiumSwitcher from '#lib/CompendiumSwitcher.svelte';
	import { iconUrl, otherCompendiums } from '#lib/compendiums.js';
	import { kindPath } from '#lib/entities.js';
	import { wrongFactHref } from '#lib/seo.js';
	let { data, children } = $props();
	const itemsPath = $derived(kindPath('item', data.kinds)!);
	// Home opens with the game's art, so its header sits on the art and its search sits in the hero.
	const home = $derived(page.url.pathname === '/');
	// The item list has its own name search, so below desktop width it drops the header's search to reach its
	// results sooner.
	const list = $derived(page.url.pathname === itemsPath);
	const current = (path: string) => (page.url.pathname.startsWith(path) ? 'page' : undefined);
	const reportHref = $derived(wrongFactHref(page.url.pathname));
	function revealFragment() {
		if (!window.location.hash) return;
		let id: string;
		try {
			id = decodeURIComponent(window.location.hash.slice(1));
		} catch {
			return;
		}
		const target = document.getElementById(id);
		if (!target) return;
		let ancestor = target.parentElement;
		while (ancestor) {
			if (ancestor instanceof HTMLDetailsElement) ancestor.open = true;
			ancestor = ancestor.parentElement;
		}
		target.scrollIntoView();
	}
	afterNavigate(revealFragment);
	onMount(() => {
		markHydrated();
		revealFragment();
		window.addEventListener('hashchange', revealFragment);
		return () => window.removeEventListener('hashchange', revealFragment);
	});
</script>

<a class="skip" href="#main">Skip to content</a>
<header class="site-header" class:over-art={home}>
	<div class="bar">
		<a class="brand" href={resolve('/')}>
			<img
				class="emblem"
				src="/brand/emblem-48.webp"
				srcset="/brand/emblem-48.webp 1x, /brand/emblem-96.webp 2x"
				width="96"
				height="96"
				alt=""
			/>
			<span class="brand-name"
				><span class="game">The Bloodline</span><span class="kind">Compendium</span></span
			>
		</a>
		<nav aria-label="Main">
			<a href={itemsPath} aria-current={current(itemsPath)}>Items</a>
		</nav>
		<CompendiumSwitcher />
		{#if !home}<div class="header-search" class:on-list={list}><Search /></div>{/if}
		<a class="support" href="https://ko-fi.com/wowmuch">Support</a>
	</div>
</header>
<main id="main" tabindex="-1" class:shell={!home}>{@render children()}</main>
<EntityPreviews />
<footer class="site-footer">
	<div class="footer-inner">
		<h2>The Bloodline Compendium</h2>
		<section aria-labelledby="footer-site">
			<h3 id="footer-site">This site</h3>
			<ul>
				<li><a href={itemsPath}>Items</a></li>
				<li><a href={resolve('known-issues/')}>Known Issues</a></li>
				<li><a href={resolve('about/')}>About</a></li>
			</ul>
		</section>
		<section aria-labelledby="footer-project">
			<h3 id="footer-project">Project</h3>
			<ul>
				<li><a href="https://github.com/glockyco/bloodline-compendium">Code on GitHub</a></li>
				<li><a href="https://ko-fi.com/wowmuch">Support on Ko-fi</a></li>
				<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- an address on GitHub, not on this site. -->
				<li><a href={reportHref}>Report a wrong fact</a></li>
			</ul>
		</section>
		<section aria-labelledby="footer-game">
			<h3 id="footer-game">Game &amp; community</h3>
			<ul>
				<li>
					<a href="https://store.steampowered.com/app/1159290/The_Bloodline/"
						>The Bloodline on Steam</a
					>
				</li>
			</ul>
		</section>
		<section class="others-band" aria-labelledby="footer-others">
			<h3 id="footer-others">Other compendiums</h3>
			<ul class="others">
				{#each otherCompendiums as compendium (compendium.icon)}
					<li>
						<a href={compendium.url} rel="external">
							<img src={iconUrl(compendium)} alt="" width="128" height="128" loading="lazy" />
							{compendium.name}
						</a>
					</li>
				{/each}
			</ul>
		</section>
		<div class="notes">
			<ReleaseInfo release={data.release} />
			<p>Unofficial fan project, not affiliated with Shieldbearer Studios or 505 Games.</p>
			<p>Art on the home page is from The Bloodline's Steam page.</p>
		</div>
	</div>
</footer>

<style>
	.site-header {
		position: relative;
		z-index: var(--layer-search);
		background: var(--surface);
		border-bottom: var(--border-width) solid var(--border);
	}
	.site-header.over-art {
		position: absolute;
		inset: 0 0 auto;
		background: linear-gradient(var(--surface-veil), var(--surface-fade));
		border-bottom: 0;
	}
	.bar {
		display: flex;
		align-items: center;
		gap: var(--gap-section);
		max-width: var(--size-page);
		min-height: var(--size-header);
		margin: 0 auto;
		padding: var(--inset) var(--gutter);
	}
	.brand {
		display: flex;
		align-items: center;
		gap: var(--gap-block);
		color: var(--text);
		text-decoration: none;
	}
	.emblem {
		display: block;
		flex: none;
		width: var(--size-emblem);
		height: var(--size-emblem);
	}
	.brand-name {
		display: grid;
		font-family: var(--font-heading);
		line-height: var(--leading-display);
	}
	.game {
		font-size: var(--type-2);
	}
	.kind {
		font-size: var(--type-1);
		color: var(--gold);
	}
	nav {
		display: flex;
		gap: var(--gap-groups);
	}
	nav a,
	.support {
		display: inline-flex;
		align-items: center;
		min-height: var(--size-control);
		color: var(--text);
		font-weight: var(--weight-strong);
		text-decoration: none;
	}
	nav a:hover,
	.support:hover {
		color: var(--text);
		text-decoration: underline;
	}
	nav a[aria-current='page'] {
		box-shadow: inset 0 calc(-1 * var(--marker-width)) 0 var(--gold);
	}
	.header-search {
		flex: 1;
		max-width: var(--size-header-search);
		margin-left: auto;
	}
	.support {
		margin-left: auto;
		color: var(--text-soft);
	}
	.header-search + .support {
		margin-left: 0;
	}
	/* The page fills the window, and the main region takes the room the footer leaves. */
	main {
		flex: 1 0 auto;
	}
	main.shell {
		width: 100%;
		max-width: var(--size-page);
		margin: 0 auto;
		padding: var(--inset-loose) var(--gutter) var(--gap-page);
	}
	.site-footer {
		border-top: var(--border-width) solid var(--border);
		color: var(--text-muted);
	}
	/* The footer is quieter than the page. Each group of links is a band with its label beside it. */
	.footer-inner {
		max-width: var(--size-page);
		margin: 0 auto;
		padding: var(--inset-loose) var(--gutter) var(--gap-groups);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
	}
	.footer-inner h2 {
		margin-bottom: var(--gap-inline);
		color: var(--text);
		font-size: var(--type-2);
	}
	.footer-inner section {
		display: grid;
		grid-template-columns: var(--size-footer-label) minmax(0, 1fr);
		align-items: baseline;
		gap: var(--gap-inline) var(--gap-groups);
		padding: var(--gap-block) 0;
		border-bottom: var(--border-width) solid var(--border);
	}
	.footer-inner h3 {
		color: var(--text);
		font-size: var(--type-1);
	}
	.footer-inner ul {
		display: flex;
		flex-wrap: wrap;
		gap: 0 var(--gap-groups);
		padding: 0;
		list-style: none;
	}
	.footer-inner li {
		line-height: inherit;
	}
	.site-footer a {
		display: inline-flex;
		align-items: center;
		min-height: var(--size-chip);
		color: var(--text-soft);
		text-decoration: none;
	}
	.site-footer a:hover {
		color: var(--text);
		text-decoration: underline;
	}
	/* The other compendiums as equal cards with their logos, three in a row and full-width rows on phones. Their
	   label sits at the middle of the cards rather than on the baseline of their names. */
	.footer-inner .others-band {
		align-items: center;
	}
	.footer-inner .others {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		gap: var(--gap-block);
	}
	.others a {
		gap: var(--gap-inline);
		width: 100%;
		min-height: var(--size-control);
		padding: 0 var(--inset);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
		color: var(--text);
		white-space: nowrap;
		transition:
			border-color var(--duration-hover),
			background-color var(--duration-hover);
	}
	.others a:hover {
		border-color: var(--border-control);
		background: var(--surface-raised);
		text-decoration: none;
	}
	.others img {
		flex: none;
		width: var(--size-icon-row);
		height: var(--size-icon-row);
	}
	.notes {
		display: grid;
		gap: var(--gap-line);
		padding-top: var(--gap-block);
	}
	.notes p {
		line-height: inherit;
		text-wrap: pretty;
	}
	/* Below desktop width the search takes a row of its own under the brand and navigation, and the footer's
	   labels go above their links. */
	@media (width < 64rem) {
		.footer-inner section {
			grid-template-columns: minmax(0, 1fr);
		}
		.bar {
			flex-wrap: wrap;
			gap: var(--gap-block) var(--gap-section);
		}
		.header-search {
			order: 4;
			flex-basis: 100%;
			max-width: none;
		}
		.header-search + .support {
			margin-left: auto;
		}
		.header-search.on-list {
			display: none;
		}
	}
	/* A phone keeps the brand and navigation on one row above the search. Support stays in the footer. */
	@media (width < 40rem) {
		.footer-inner .others {
			grid-template-columns: minmax(0, 1fr);
		}
		.site-footer a {
			min-height: var(--size-control);
		}
		.bar {
			gap: var(--gap-line) var(--gap-groups);
		}
		.bar > nav {
			margin-left: auto;
		}
		.support {
			display: none;
		}
	}
</style>
