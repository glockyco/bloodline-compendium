<script lang="ts">
	import { page } from '$app/state';
	import EntityLink from './EntityLink.svelte';
	import { linkParts } from '../links.js';
	/**
	 * Text written for the site, with each Markdown link to a typed target, such as [Blood Levels](mechanic:blood-levels),
	 * shown as a link to that entity. The catalog has resolved every target, and a kind without pages shows as text.
	 * What the text puts in double quotes is the game's own wording, such as a line an NPC says, and is marked as game
	 * text.
	 */
	let { text }: { text: string } = $props();
	const parts = $derived(linkParts(text, page.data.links ?? {}));
	const quoted = (plain: string) => plain.split(/("[^"]*")/);
</script>

{#each parts as part, index (index)}{#if part.entity}<EntityLink entity={part.entity}
			>{part.text}</EntityLink
		>{:else}{#each quoted(part.text) as piece, at (at)}{#if at % 2}<span data-game>{piece}</span
				>{:else}{piece}{/if}{/each}{/if}{/each}
