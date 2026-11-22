<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import DisciplineGroups from '#lib/skills/DisciplineGroups.svelte';
	let { data } = $props();
</script>

<SeoHead
	title="Disciplines | The Bloodline Compendium"
	description="Every discipline of The Bloodline's skill window, with its skills by level and its passives with their cost per rank."
/>
<h1 class="list-title">Disciplines</h1>
<DisciplineGroups disciplines={data.index.disciplines} kinds={data.kinds} heading="h2" />
