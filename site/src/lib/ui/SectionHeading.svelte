<script lang="ts">
	import Count from './Count.svelte';
	let {
		id,
		title,
		count,
		link
	}: {
		id: string;
		title: string;
		count?: number;
		/** A link to the full list, such as All 2,087 items. */
		link?: { href: string; label: string };
	} = $props();
	const lastSpace = $derived(title.lastIndexOf(' '));
</script>

<div class="section-heading">
	<h2 {id}>
		{#if count === undefined}{title}{:else}{title.slice(0, lastSpace + 1)}<span class="heading-end"
				>{title.slice(lastSpace + 1)}&nbsp;<Count value={count} /></span
			>{/if}
	</h2>
	{#if link}
		<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- callers pass hrefs they resolved. -->
		<a href={link.href}>{link.label}</a>
	{/if}
</div>

<style>
	.section-heading {
		display: flex;
		flex-wrap: wrap;
		align-items: baseline;
		justify-content: space-between;
		gap: var(--gap-inline) var(--gap-block);
		margin-bottom: var(--gap-heading);
	}
	h2 {
		margin: 0;
		font-size: var(--type-section);
		line-height: var(--leading-heading);
		text-wrap: balance;
	}
	.heading-end {
		white-space: nowrap;
	}
</style>
