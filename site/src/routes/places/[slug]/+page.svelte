<script lang="ts">
	import PlacePage from '#lib/places/PlacePage.svelte';
	let { data } = $props();
</script>

<PlacePage place={data.place} kinds={data.kinds} categories={data.categories} />
