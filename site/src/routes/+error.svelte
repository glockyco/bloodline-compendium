<script lang="ts">
	import { page } from '$app/state';
	import MissingPage from '#lib/MissingPage.svelte';
</script>

{#if page.status === 404}<MissingPage />{:else}
	<p class="eyebrow">{page.status}</p>
	<h1>Something went wrong</h1>
	<p class="intro">This page could not be shown. Try again, or browse the item list.</p>
{/if}
