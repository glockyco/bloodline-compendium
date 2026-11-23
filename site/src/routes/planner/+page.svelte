<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import Planner from '#lib/planner/Planner.svelte';
	let { data } = $props();
</script>

<SeoHead
	title="Character Planner | The Bloodline Compendium"
	description="Plan a character for The Bloodline by the game's rules and share it as a link."
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Character Planner', path: '/planner/' }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li aria-current="page">Character Planner</li>
	</ol>
</nav>
<header class="page-heading">
	<h1>Character Planner</h1>
	<p class="intro">Plan a character by the game's rules and share it as a link.</p>
</header>
<Planner data={data.planner} kinds={data.kinds} />
