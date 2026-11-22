<script lang="ts">
	import ProfessionPage from '#lib/professions/ProfessionPage.svelte';
	let { data } = $props();
</script>

<ProfessionPage {data} />
