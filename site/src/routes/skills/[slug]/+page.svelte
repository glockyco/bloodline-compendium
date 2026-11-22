<script lang="ts">
	import { resolve } from '$app/paths';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import EntityVersions from '#lib/entity/EntityVersions.svelte';
	import SkillVersion from '#lib/skills/SkillVersion.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { titleCase } from '#lib/labels.js';
	import { skillStatus } from '#lib/skills/describe.js';
	let { data } = $props();
	const versions = $derived([data.skill, ...(data.skill.versions ?? [])]);
	let selected = $state(0);
	const skill = $derived(versions[selected] ?? data.skill);
	const kind = $derived(data.kinds.find((entry) => entry.kind === skill.kind)!);
	const listPath = $derived(kindPath(skill.kind, data.kinds)!);
	const tree = $derived(skill.sources.find((source) => source.kind === 'skillTree'));
	const description = $derived(
		tree
			? `${skill.name}, a ${tree.name} skill of The Bloodline that unlocks at ${tree.conditions[0]?.label ?? 'a known level'}.`
			: `${skill.name}, a skill of The Bloodline. ${skillStatus(skill) ?? 'Learned outside the skill trees.'}`
	);
	const reference = $derived({
		kind: skill.kind,
		key: skill.key,
		name: skill.name,
		slug: skill.slug,
		icon: skill.icon
	});
</script>

<SeoHead
	title={skill.name + ' Skill | The Bloodline Compendium'}
	{description}
	entity={reference}
	{kind}
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Skills', path: listPath },
		{ name: skill.name, path: entityPath(reference, data.kinds)! }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li><a href={listPath}>Skills</a></li>
		<li aria-current="page" data-game>{skill.name}</li>
	</ol>
</nav>
<EntityArticle
	{kind}
	name={data.skill.name}
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
				>{#if tree}<span aria-hidden="true">·</span><span data-game>{tree.name}</span>{/if}
			</p>
		{/snippet}
		{#snippet panel(version, idPrefix)}<SkillVersion skill={version} {idPrefix} />{/snippet}
	</EntityVersions>
</EntityArticle>
