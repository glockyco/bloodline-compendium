<script lang="ts">
	import { resolve } from '$app/paths';
</script>

<svelte:head
	><title>Page not found | The Bloodline Compendium</title><meta
		name="robots"
		content="noindex"
	/></svelte:head
>
<p class="eyebrow">404</p>
<h1>Page not found</h1>
<p class="intro">This page is not in the compendium.</p>
<a class="button" href={resolve('items/')}>Browse all items</a>
