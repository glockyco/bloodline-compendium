<script lang="ts">
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import EntityVersions from '#lib/entity/EntityVersions.svelte';
	import SkillVersion from '#lib/skills/SkillVersion.svelte';
	import CharacterCue from '#lib/planner/CharacterCue.svelte';
	import { kindPath } from '#lib/entities.js';
	import { titleCase } from '#lib/labels.js';
	import { skillStatus } from '#lib/skills/describe.js';
	let { data } = $props();
	const versions = $derived([data.skill, ...(data.skill.versions ?? [])]);
	let selected = $state(0);
	const skill = $derived(versions[selected] ?? data.skill);
	const tree = $derived(skill.sources.find((source) => source.kind === 'skillTree'));
	const description = $derived(
		tree
			? `${skill.name}, a ${tree.name} skill of The Bloodline that unlocks at ${tree.conditions[0]?.label ?? 'a known level'}.`
			: `${skill.name}, a skill of The Bloodline. ${skillStatus(skill) ?? 'Learned outside the skill trees.'}`
	);
	// The trees that hold the skill and the level each needs, which the active character's cue compares.
	const trees = $derived(
		data.skill.sources.flatMap((source) =>
			source.kind === 'skillTree'
				? source.conditions.flatMap((condition) =>
						condition.kind === 'disciplineLevel' && condition.entity?.key && condition.value != null
							? [
									{
										discipline: condition.entity.key,
										name: condition.entity.name,
										level: condition.value
									}
								]
							: []
					)
				: []
		)
	);
	const reference = $derived({
		kind: skill.kind,
		key: skill.key,
		name: skill.name,
		slug: skill.slug,
		icon: skill.icon
	});
</script>

<EntityPage
	entity={reference}
	kinds={data.kinds}
	title={skill.name + ' Skill'}
	{description}
	game
	searchName={data.skill.name}
	icon={data.skill.icon}
	detail={titleCase(data.category.name) + (tree ? ' · ' + tree.name : '')}
>
	<EntityVersions {versions} name={skill.name} bind:selected>
		{#snippet heading()}
			<h1 data-game>{skill.name}</h1>
			<p class="identity">
				<a
					href={kindPath(
						skill.kind,
						data.kinds,
						`?category=${encodeURIComponent(data.category.slug)}`
					)!}>{titleCase(data.category.name)}</a
				>{#if tree?.entity}<span aria-hidden="true">·</span><EntityLink entity={tree.entity} />{/if}
			</p>
			<CharacterCue skill={{ key: data.skill.key, trees }} />
		{/snippet}
		{#snippet panel(version, idPrefix)}<SkillVersion skill={version} {idPrefix} />{/snippet}
	</EntityVersions>
</EntityPage>
