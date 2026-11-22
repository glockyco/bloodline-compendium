<script lang="ts">
	import { sourceHeadings } from '#lib/labels.js';
	import type { ListFacet, SkillSummary, SourceKind } from '#lib/site-data.js';
	import SeoHead from '#lib/SeoHead.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import EntityList from '#lib/list/EntityList.svelte';
	import { kindPath } from '#lib/entities.js';
	import { skillListModel } from '#lib/skills/list.js';
	let { data } = $props();
	const index = $derived(data.index);
	const model = $derived(skillListModel(index));
	const categoryPlurals = $derived(
		Object.fromEntries(index.categories.map((category) => [category.slug, category.plural]))
	);
	function valueLabel(facet: ListFacet, value: string): string {
		return (
			facet.labels[value] ?? (facet.id === 'source' ? sourceHeadings[value as SourceKind] : value)
		);
	}
</script>

<SeoHead
	title="Skills | The Bloodline Compendium"
	description="Find every skill of The Bloodline by name, discipline and way to get it, with its cost, cooldown and damage."
/>
<h1 class="list-title">Skills</h1>
<EntityList
	rows={index.skills}
	{model}
	noun={{ one: 'skill', many: 'skills', column: 'Skill' }}
	{categoryPlurals}
	fixed={() => []}
	statusNotes={{ noKnownSource: 'No known source', notInNormalPlay: 'Not in normal play' }}
	reveal={{
		show: (count) => `Show ${count} more without a known way to get them`,
		hide: 'Hide skills without a known way to get them'
	}}
	{valueLabel}
	path={(query) => kindPath('skill', data.kinds, query)!}
>
	{#snippet name(skill: SkillSummary)}<EntityLink class="entity-name" entity={skill}
			>{#if skill.icon}<img
					src={'/icons/' + skill.icon + '-48.webp'}
					alt=""
					width="32"
					height="32"
					loading="lazy"
				/>{/if}<span data-game>{skill.name}</span></EntityLink
		>{/snippet}
</EntityList>

<style>
	.list-title {
		margin-bottom: var(--gap-groups);
	}
</style>
