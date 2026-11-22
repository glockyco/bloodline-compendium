<script lang="ts">
	import { resolve } from '$app/paths';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import Section from '#lib/ui/Section.svelte';
	import SourceSections from '#lib/entity/SourceSections.svelte';
	import UnconfirmedSources from '#lib/entity/UnconfirmedSources.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import SkillTooltip from '#lib/skills/SkillTooltip.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { titleCase } from '#lib/labels.js';
	import { skillStatusSentence } from '#lib/skills/describe.js';
	let { data } = $props();
	const skill = $derived(data.skill);
	const kind = $derived(data.kinds.find((entry) => entry.kind === skill.kind)!);
	const listPath = $derived(kindPath(skill.kind, data.kinds)!);
	// A skill without a confirmed way to learn it says so, which every source section would otherwise answer.
	const status = $derived(
		skill.sources.some((source) => !source.unconfirmed) ? null : skillStatusSentence[skill.status]
	);
	const tree = $derived(skill.sources.find((source) => source.kind === 'skillTree'));
	const description = $derived(
		tree
			? `${skill.name}, a ${tree.name} skill of The Bloodline that unlocks at ${tree.conditions[0]?.label ?? 'a known level'}.`
			: `${skill.name}, a skill of The Bloodline. ${status ?? 'Learned outside the skill trees.'}`
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
	title={skill.name +
		(skill.qualifier ? ' (' + skill.qualifier + ')' : '') +
		' Skill | The Bloodline Compendium'}
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
<EntityArticle {kind}>
	<div class="item-heading">
		<h1 data-pagefind-meta="title" data-game>{skill.name}</h1>
		<p class="identity">
			<a
				href={kindPath(
					skill.kind,
					data.kinds,
					`?category=${encodeURIComponent(data.category.slug)}`
				)!}>{titleCase(data.category.name)}</a
			>{#if tree}<span aria-hidden="true">·</span><span data-game>{tree.name}</span>{/if}
		</p>
	</div>
	<div class="detail-layout">
		<!-- Only a skill without a confirmed way to learn it opens with an answer, which its sources give otherwise. -->
		{#if status}<div class="answer" data-pagefind-ignore="all">
				<Section id="how-to-get-it" title="How to Get It">
					<p class="surface">{status}</p>
				</Section>
			</div>{/if}
		<aside class="item-side" aria-label="Game card">
			<SkillTooltip skill={{ ...skill }} heading="p" />
		</aside>
		<div class="item-relations" data-pagefind-ignore="all">
			<SourceSections sources={skill.sources} />
			<UnconfirmedSources sources={skill.sources} name={skill.name} />
		</div>
	</div>
</EntityArticle>
