<script lang="ts">
	import { resolve } from '$app/paths';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import DisciplineWindow from '#lib/skills/DisciplineWindow.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
	const discipline = $derived(data.discipline);
	const kind = $derived(data.kinds.find((entry) => entry.kind === discipline.kind)!);
	const listPath = $derived(kindPath(discipline.kind, data.kinds)!);
	const skills = $derived(discipline.skillRows.reduce((sum, row) => sum + row.slots.length, 0));
	const passives = $derived(discipline.passiveRows.reduce((sum, row) => sum + row.passives.length, 0));
	const description = $derived(
		[
			`${discipline.name}, a ${discipline.category} discipline of The Bloodline`,
			skills ? `${formatNumber(skills)} skills by level` : null,
			passives ? `${formatNumber(passives)} passives with their cost per rank` : null,
			discipline.spellsongs.length ? `${formatNumber(discipline.spellsongs.length)} spellsongs` : null
		]
			.filter(Boolean)
			.join(', ') + '.'
	);
	const reference = $derived({
		kind: discipline.kind,
		key: discipline.key,
		name: discipline.name,
		slug: discipline.slug,
		icon: null
	});
</script>

<SeoHead
	title={discipline.name + ' Discipline | The Bloodline Compendium'}
	{description}
	entity={reference}
	{kind}
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Disciplines', path: listPath },
		{ name: discipline.name, path: entityPath(reference, data.kinds)! }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li><a href={listPath}>Disciplines</a></li>
		<li aria-current="page" data-game>{discipline.name}</li>
	</ol>
</nav>
<EntityArticle {kind}>
	<div class="heading">
		<h1 data-pagefind-meta="title" data-game>{discipline.name}</h1>
		<p class="identity">{discipline.category}</p>
	</div>
	{#if discipline.notes.length}
		<section class="bugs" aria-labelledby="bugs-heading">
			<h2 id="bugs-heading">Bugs</h2>
			<ul>
				{#each discipline.notes as note (note.issue)}
					<li>
						{note.effect}
						<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- the resolved issue page with its section. -->
						<a href={resolve('known-issues/') + '#' + note.issue}>Read about this issue</a>
					</li>
				{/each}
			</ul>
		</section>
	{/if}
	<DisciplineWindow {discipline} />
</EntityArticle>

<style>
	.heading {
		display: grid;
		gap: var(--gap-line);
		margin-bottom: var(--gap-groups);
	}
	.identity {
		margin: 0;
		color: var(--text-muted);
	}
	.bugs {
		display: grid;
		gap: var(--gap-inline);
		margin-bottom: var(--gap-groups);
	}
	.bugs h2 {
		margin: 0;
		font-size: var(--type-section);
	}
	.bugs ul {
		margin: 0;
		padding-left: var(--inset);
	}
</style>
