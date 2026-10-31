<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import { page } from '$app/state';
	import { kindPath } from '#lib/entities.js';
</script>

<SeoHead
	title="Page not found | The Bloodline Compendium"
	description="This page does not exist."
	noindex
/>
<div class="sections">
	<header class="page-heading">
		<p class="eyebrow">404</p>
		<h1>Page not found</h1>
		<p class="intro">
			This page does not exist. Search for an item above, or browse the item list.
		</p>
	</header>
	<div><ButtonLink href={kindPath('item', page.data.kinds)!}>Browse All Items</ButtonLink></div>
</div>
