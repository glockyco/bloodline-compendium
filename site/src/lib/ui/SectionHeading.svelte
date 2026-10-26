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
</script>

<div class="section-heading">
	<h2 {id}>
		{title}
		{#if count !== undefined}<Count value={count} />{/if}
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
		margin-bottom: var(--gap-groups);
	}
	h2 {
		margin: 0;
		font-size: var(--type-4);
		line-height: var(--leading-title);
		text-wrap: balance;
	}
</style>
