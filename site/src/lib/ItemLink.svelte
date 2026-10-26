<script lang="ts">
	import { resolve } from '$app/paths';
	import type { ItemLink } from './site-data.js';
	let { item }: { item: ItemLink } = $props();
</script>

{#if item.slug}<a href={resolve(`items/${item.slug}/`)}>{item.name}</a>{:else}{item.name}{/if}
