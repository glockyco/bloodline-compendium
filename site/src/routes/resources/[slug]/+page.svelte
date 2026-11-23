<script lang="ts">
	import ResourcePage from '#lib/resources/ResourcePage.svelte';
	let { data } = $props();
</script>

<ResourcePage resource={data.resource} kinds={data.kinds} />
