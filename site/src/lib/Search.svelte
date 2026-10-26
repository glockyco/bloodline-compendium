<script lang="ts">
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import '@pagefind/component-ui/css';
	onMount(() => {
		void import('@pagefind/component-ui');
	});
</script>

<section class="search-panel" aria-label="Search the compendium">
	<p class="search-label">Search the Compendium</p>
	<div class="search-content">
		<pagefind-searchbox
			bundle-path="/pagefind/"
			placeholder="Search item names and descriptions"
			aria-label="Search items"
		></pagefind-searchbox>
		<noscript
			>Search needs JavaScript. Browse the <a href={resolve('items/')}>item list</a> instead.</noscript
		>
	</div>
</section>
