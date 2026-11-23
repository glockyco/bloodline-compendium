<script lang="ts">
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import DisciplineTables from '#lib/skills/DisciplineTables.svelte';
	import BugsSection from '#lib/entity/BugsSection.svelte';
	import LinkedText from '#lib/ui/LinkedText.svelte';
	import CharacterCue from '#lib/planner/CharacterCue.svelte';
	import { kindPath } from '#lib/entities.js';
	import { disciplineContents, disciplineIntro } from '#lib/skills/describe.js';
	let { data } = $props();
	const discipline = $derived(data.discipline);
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

<EntityPage
	entity={reference}
	kinds={data.kinds}
	title={discipline.name + ' Discipline'}
	{description}
	game
	icon={discipline.icon ?? null}
	detail={discipline.category.name}
>
	<header class="item-heading">
		<h1 data-game>{discipline.name}</h1>
		<p class="identity">
			<a href={kindPath(discipline.kind, data.kinds, '#' + discipline.category.slug)!}
				>{discipline.category.name}</a
			>
		</p>
		<p class="intro">
			<LinkedText text={disciplineIntro(discipline)} />
		</p>
		<CharacterCue
			discipline={{
				key: discipline.key,
				name: discipline.name,
				skills: discipline.skillRows.flatMap((row) =>
					row.slots.flatMap((slot) => slot.skill.key ?? [])
				)
			}}
		/>
	</header>
	<div class="item-relations" data-pagefind-ignore="all">
		<DisciplineTables {discipline} skillFacts={data.skillFacts} />
		<BugsSection notes={discipline.notes} name={discipline.name} />
	</div>
</EntityPage>
