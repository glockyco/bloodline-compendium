<script lang="ts">
	import { resolve } from '$app/paths';
	import { afterNavigate } from '$app/navigation';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import '../app.css';
	import Search from '#lib/Search.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	let { data, children } = $props();
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
<header>
	<div class="header-inner">
		<a class="brand" href={resolve('/')}>The Bloodline <span>Compendium</span></a>
		<nav aria-label="Main">
			<a
				href={resolve('items/')}
				aria-current={page.url.pathname.startsWith('/items/') ? 'page' : undefined}>Items</a
			><a href="https://ko-fi.com/wowmuch">Support</a>
		</nav>
	</div>
</header>
<div class="shell">
	<Search />
	<main id="main" tabindex="-1">{@render children()}</main>
</div>
<footer>
	<ReleaseInfo release={data.release} />
	<p>Unofficial fan project, not affiliated with the developer or publisher of The Bloodline.</p>
	<nav aria-label="Footer">
		<a href={resolve('coverage/')}>Coverage</a><a href={resolve('about/')}>About</a><a
			href="https://ko-fi.com/wowmuch">Support on Ko-fi</a
		>
	</nav>
</footer>
