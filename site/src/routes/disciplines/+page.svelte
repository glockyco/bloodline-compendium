<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import Section from '#lib/ui/Section.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid from '#lib/ui/CardGrid.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import Tabs from '#lib/ui/Tabs.svelte';
	import { entityPath } from '#lib/entities.js';
	import { plural } from '#lib/format.js';
	import type { DisciplineSummary } from '#lib/site-data.js';
	let { data } = $props();
	const disciplines = $derived(data.index.disciplines);
	const groups = $derived(
		[...new Map(disciplines.map((entry) => [entry.category.slug, entry.category])).values()].map(
			(category) => ({
				category,
				members: disciplines.filter((entry) => entry.category.slug === category.slug)
			})
		)
	);
	const counts = (discipline: DisciplineSummary) =>
		[
			discipline.skills ? plural(discipline.skills, 'skill', 'skills') : null,
			discipline.passives ? plural(discipline.passives, 'passive', 'passives') : null,
			discipline.spellsongs ? plural(discipline.spellsongs, 'spellsong', 'spellsongs') : null
		]
			.filter(Boolean)
			.join(', ');
	const path = (discipline: DisciplineSummary) =>
		entityPath(
			{
				kind: 'discipline',
				key: discipline.key,
				name: discipline.name,
				slug: discipline.slug,
				icon: null
			},
			data.kinds
		)!;
</script>

{#snippet card(discipline: DisciplineSummary, level: 'h2' | 'h3')}
	<Card href={path(discipline)}>
		<svelte:element this={level} class="card-head"
			><IconSlot icon={discipline.icon ?? null} /><span data-game>{discipline.name}</span
			></svelte:element
		>
		<p class="card-line">
			{counts(discipline)}
		</p>
	</Card>
{/snippet}

<SeoHead
	title="Disciplines | The Bloodline Compendium"
	description="Every discipline of The Bloodline's skill window, with its skills by level and its passives with their cost per rank."
/>
<header class="page-heading list-title">
	<h1>Disciplines</h1>
	<p class="intro">
		The {disciplines.length} disciplines of the skill window, each with its skills by level and its passives
		with their cost per rank.
	</p>
</header>
<!-- The game's skill window groups disciplines in these categories, one tab each. -->
<Tabs
	tabs={groups.map((group) => ({
		id: group.category.slug,
		label: group.category.name,
		count: group.members.length
	}))}
	label="Skill window categories"
	initial={groups[0].category.slug}
>
	{#snippet panel(id, tabbed)}{@const group = groups.find((entry) => entry.category.slug === id)!}
		{#if tabbed}<CardGrid rows={2} columns={4} label={group.category.name}
				>{#each group.members as discipline (discipline.key)}{@render card(
						discipline,
						'h2'
					)}{/each}</CardGrid
			>{:else}<Section {id} title={group.category.name} count={group.members.length}
				><CardGrid rows={2} columns={4} label={group.category.name}
					>{#each group.members as discipline (discipline.key)}{@render card(
							discipline,
							'h3'
						)}{/each}</CardGrid
				></Section
			>{/if}{/snippet}
</Tabs>

<style>
	.card-head {
		display: flex;
		align-items: center;
		gap: var(--gap-block);
		margin: 0;
		font-size: var(--type-sub);
		line-height: var(--leading-heading);
	}
	.card-line {
		margin: 0;
		color: var(--text-muted);
	}
</style>
