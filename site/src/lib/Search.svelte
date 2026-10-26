<script lang="ts">
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import '@pagefind/component-ui/css';
	onMount(() => {
		void import('@pagefind/component-ui');
	});
</script>

<details class="search-panel">
	<summary>Search items</summary>
	<div class="search-content">
		<pagefind-searchbox bundle-path="/pagefind/" placeholder="Start typing an item name"
		></pagefind-searchbox><pagefind-results></pagefind-results><noscript
			>Search needs JavaScript. Browse the <a href={resolve('items/')}>item list</a> instead.</noscript
		>
	</div>
</details>
