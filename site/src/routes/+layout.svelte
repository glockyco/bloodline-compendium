<script lang="ts">
	import { resolve } from '$app/paths';
	import '../app.css';
	import Search from '#lib/Search.svelte';
	let { data, children } = $props();
</script>

<svelte:head><meta name="theme-color" content="#142c2b" /></svelte:head>
<a class="skip" href="#main">Skip to content</a>
<header>
	<div class="header-inner">
		<a class="brand" href={resolve('/')}>The Bloodline <span>Compendium</span></a>
		<nav aria-label="Main">
			<a href={resolve('items/')}>Items</a><a href={resolve('coverage/')}>Coverage</a>
		</nav>
		<span class="build-label">Steam build {data.buildId}</span>
	</div>
</header>
<div class="shell">
	<Search />
	<main id="main">{@render children()}</main>
</div>
<footer>
	<div>Unofficial fan project. Not affiliated with the game's creators.</div>
	<a href={resolve('/build-info.json')}>Build information</a><a href={resolve('coverage/')}
		>Data coverage</a
	>
</footer>
