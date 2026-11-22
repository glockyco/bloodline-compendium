<script lang="ts">
	import { page } from '$app/state';
	import EntityLink from './EntityLink.svelte';
	import { linkParts } from '../links.js';
	/**
	 * Text written for the site, with each Markdown link to a typed target, such as [Blood Levels](mechanic:blood-levels),
	 * shown as a link to that entity. The catalog has resolved every target, and a kind without pages shows as text.
	 */
	let { text }: { text: string } = $props();
	const parts = $derived(linkParts(text, page.data.links ?? {}));
</script>

{#each parts as part, index (index)}{#if part.entity}<EntityLink entity={part.entity}
			>{part.text}</EntityLink
		>{:else}{part.text}{/if}{/each}
