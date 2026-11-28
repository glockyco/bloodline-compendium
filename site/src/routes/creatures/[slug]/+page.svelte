<script lang="ts">
	import CreaturePage from '#lib/creatures/CreaturePage.svelte';
	let { data } = $props();
</script>

<CreaturePage creature={data.creature} kinds={data.kinds} />
