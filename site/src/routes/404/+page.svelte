<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import { page } from '$app/state';
	import { kindPath } from '#lib/entities.js';
</script>

<SeoHead
	title="Page not found | The Bloodline Compendium"
	description="This page is not in the compendium."
	noindex
/>
<p class="eyebrow">404</p>
<h1>Page not found</h1>
<p class="intro">This page is not in the compendium.</p>
<ButtonLink href={kindPath('item', page.data.kinds)!}>Browse All Items</ButtonLink>
