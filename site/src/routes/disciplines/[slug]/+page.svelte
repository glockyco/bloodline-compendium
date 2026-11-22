<script lang="ts">
	import { resolve } from '$app/paths';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import DisciplineTables from '#lib/skills/DisciplineTables.svelte';
	import BugsSection from '#lib/entity/BugsSection.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { disciplineContents } from '#lib/skills/describe.js';
	let { data } = $props();
	const discipline = $derived(data.discipline);
	const kind = $derived(data.kinds.find((entry) => entry.kind === discipline.kind)!);
	const listPath = $derived(kindPath(discipline.kind, data.kinds)!);
	const contents = $derived(
		disciplineContents({
			skills: discipline.skillRows.reduce((sum, row) => sum + row.slots.length, 0),
			passives: discipline.passiveRows.reduce((sum, row) => sum + row.passives.length, 0),
			spellsongs: discipline.spellsongs.length
		})
	);
	const description = $derived(
		`${discipline.name} is a ${discipline.category.name} discipline in The Bloodline.` +
			(contents ? ` It has ${contents}.` : '')
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
<EntityArticle
	{kind}
	name={discipline.name}
	icon={discipline.icon ?? null}
	detail={discipline.category.name}
>
	<div class="item-heading">
		<h1 data-game>{discipline.name}</h1>
		<p class="identity">
			<a href={kindPath(discipline.kind, data.kinds, '#' + discipline.category.slug)!}
				>{discipline.category.name}</a
			>
		</p>
	</div>
	<div class="item-relations" data-pagefind-ignore="all">
		<DisciplineTables {discipline} skillFacts={data.skillFacts} />
		<BugsSection notes={discipline.notes} name={discipline.name} />
	</div>
</EntityArticle>
