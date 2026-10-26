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
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	import { kindPath } from '#lib/entities.js';
	let { data, children } = $props();
	const itemsPath = $derived(kindPath('item', data.kinds)!);
	// Home opens with the game's art, so its header sits on the art and its search sits in the hero.
	const home = $derived(page.url.pathname === '/');
	const current = (path: string) => (page.url.pathname.startsWith(path) ? 'page' : undefined);
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
			<a href={resolve('coverage/')} aria-current={current('/coverage/')}>Coverage</a>
			<a href={resolve('about/')} aria-current={current('/about/')}>About</a>
		</nav>
		{#if !home}<div class="header-search"><Search /></div>{/if}
		<a class="support" href="https://ko-fi.com/wowmuch">Support</a>
	</div>
</header>
<main id="main" tabindex="-1" class:shell={!home}>{@render children()}</main>
<footer class="site-footer">
	<div class="footer-inner">
		<nav aria-label="Footer">
			<a href={itemsPath}>Items</a><a href={resolve('coverage/')}>Coverage</a><a
				href={resolve('about/')}>About</a
			><a href="https://ko-fi.com/wowmuch">Support on Ko-fi</a>
		</nav>
		<ReleaseInfo release={data.release} />
		<p>
			Unofficial fan project, not affiliated with Shieldbearer Studios or 505 Games. Art on the home
			page is from The Bloodline's Steam page.
		</p>
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
		gap: var(--space-6);
		max-width: var(--size-page);
		min-height: var(--size-header);
		margin: 0 auto;
		padding: var(--space-3) var(--space-gutter);
	}
	.brand {
		display: flex;
		align-items: center;
		gap: var(--space-3);
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
		gap: var(--space-5);
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
	main.shell {
		max-width: var(--size-page);
		min-height: var(--size-page-min-height);
		margin: 0 auto;
		padding: var(--space-5) var(--space-gutter) var(--space-8);
	}
	.site-footer {
		margin-top: var(--space-section);
		border-top: var(--border-width) solid var(--border);
		color: var(--text-muted);
	}
	.footer-inner {
		display: grid;
		gap: var(--space-3);
		max-width: var(--size-page);
		margin: 0 auto;
		padding: var(--space-6) var(--space-gutter) var(--space-7);
	}
	.footer-inner nav {
		flex-wrap: wrap;
		gap: var(--space-2) var(--space-5);
	}
	.footer-inner nav a {
		min-height: 0;
		color: var(--accent);
		font-weight: var(--weight-regular);
		text-decoration: underline;
	}
	.footer-inner p {
		margin: 0;
	}
	@media (width < 64rem) {
		.bar {
			flex-wrap: wrap;
			gap: 0 var(--space-4);
		}
		nav {
			order: 3;
			width: 100%;
		}
		.over-art nav {
			display: none;
		}
		.header-search {
			order: 4;
			flex-basis: 100%;
			max-width: none;
			padding-bottom: var(--space-1);
		}
	}
</style>
