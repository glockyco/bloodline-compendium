<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import { page } from '$app/state';
	import { browseKinds } from '#lib/entities.js';
	// Every kind with pages, as the header lists them.
	const browse = $derived(browseKinds(page.data.kinds));
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
		<p class="intro">This page does not exist. Search above, or browse a list.</p>
	</header>
	<div class="browse">
		{#each browse as entry (entry.path)}<ButtonLink href={entry.path}
				>Browse {entry.label}</ButtonLink
			>{/each}
	</div>
</div>

<style>
	.browse {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-block);
	}
</style>
