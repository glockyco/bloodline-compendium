<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import Planner from '#lib/planner/Planner.svelte';
	let { data } = $props();
</script>

<SeoHead
	title="Skill Planner | The Bloodline Compendium"
	description="Plan a character for The Bloodline: its level, attributes, disciplines, skills, passives and skill bar, checked against the game's rules, and share it as a link."
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Skill Planner', path: '/planner/' }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li aria-current="page">Skill Planner</li>
	</ol>
</nav>
<header class="page-heading">
	<h1>Skill Planner</h1>
	<p class="intro">
		Plan a character's level, attributes, skills and passives. Each choice follows the game's rules,
		and a link shares the build.
	</p>
</header>
<Planner data={data.planner} kinds={data.kinds} />
