<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
</script>

<SeoHead
	title="Page not found | The Bloodline Compendium"
	description="This page is not in the compendium."
	noindex
/>
<p class="eyebrow">404</p>
<h1>Page not found</h1>
<p class="intro">This page is not in the compendium.</p>
<a class="button" href={resolve('items/')}>Browse all items</a>
