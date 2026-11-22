<script lang="ts">
	import { resolve } from '$app/paths';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import SkillTooltip from '#lib/skills/SkillTooltip.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { sourceHeadings } from '#lib/labels.js';
	import { formatNumber } from '#lib/format.js';
	import type { Source, SourceKind } from '#lib/site-data.js';
	let { data } = $props();
	const skill = $derived(data.skill);
	const kind = $derived(data.kinds.find((entry) => entry.kind === skill.kind)!);
	const listPath = $derived(kindPath(skill.kind, data.kinds)!);
	const groups = $derived(
		Object.entries(
			Object.groupBy(
				skill.sources.filter((source) => !source.unconfirmed),
				(source) => source.kind
			)
		) as [SourceKind, Source[]][]
	);
	const unconfirmed = $derived(skill.sources.filter((source) => source.unconfirmed));
	const status = $derived(
		skill.status === 'notInNormalPlay'
			? 'Cannot be obtained in normal play.'
			: skill.status === 'noKnownSource'
				? 'No known source.'
				: null
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
	title={skill.name + (skill.qualifier ? ' (' + skill.qualifier + ')' : '') + ' Skill | The Bloodline Compendium'}
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
	<h1 data-pagefind-meta="title" data-game>{skill.name}</h1>
	<div class="layout">
		<div class="main">
			{#if status}<p class="status">{status}</p>{/if}
			{#each groups as [sourceKind, sources] (sourceKind)}
				<section aria-labelledby={'how-' + sourceKind}>
					<h2 id={'how-' + sourceKind}>{sourceHeadings[sourceKind]}</h2>
					<ul>
						{#each sources as source (source.relationId)}
							<li data-relation-id={source.relationId} data-relation-direction="incoming">
								{#if source.kind === 'skillTree'}<EntityLink entity={source.entity} />, {source
										.conditions[0]?.label}, {formatNumber(source.cost?.amount ?? 1)} skill point
								{:else if source.places.length}<EntityLink entity={source.entity} />{#if source.kind === 'dialogue'}
										in {#each source.places as place, index (place.key ?? place.name)}{index
												? ', '
												: ''}<EntityLink entity={place} />{/each}{/if}
								{:else}<EntityLink entity={source.entity} />{/if}
							</li>
						{/each}
					</ul>
				</section>
			{/each}
			{#if unconfirmed.length}
				<section aria-labelledby="how-unconfirmed">
					<h2 id="how-unconfirmed">Named in the Game Files</h2>
					<p class="status">The game files name these, but whether they give the skill in play is unknown.</p>
					<ul>
						{#each unconfirmed as source (source.relationId)}
							<li data-relation-id={source.relationId} data-relation-direction="incoming">
								<EntityLink entity={source.entity} />
							</li>
						{/each}
					</ul>
				</section>
			{/if}
		</div>
		<aside class="side">
			<SkillTooltip skill={{ ...skill }} heading="p" />
		</aside>
	</div>
</EntityArticle>

<style>
	.layout {
		display: grid;
		gap: var(--gap-section);
		align-items: start;
	}
	@media (width >= 64rem) {
		.layout {
			grid-template-columns: minmax(0, 1fr) var(--size-item-side);
		}
		.side {
			grid-row: 1;
			grid-column: 2;
		}
	}
	.main {
		display: grid;
		gap: var(--gap-groups);
	}
	section {
		display: grid;
		gap: var(--gap-inline);
	}
	h2 {
		margin: 0;
		font-size: var(--type-section);
	}
	ul {
		margin: 0;
		padding-left: var(--inset);
	}
	.status {
		margin: 0;
		color: var(--text-soft);
	}
</style>
