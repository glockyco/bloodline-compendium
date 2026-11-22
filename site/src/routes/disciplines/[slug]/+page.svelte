<script lang="ts">
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import DisciplineTables from '#lib/skills/DisciplineTables.svelte';
	import BugsSection from '#lib/entity/BugsSection.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import LinkedText from '#lib/ui/LinkedText.svelte';
	import { kindPath } from '#lib/entities.js';
	import { disciplineContents } from '#lib/skills/describe.js';
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
	<div class="item-heading icon-heading">
		<IconSlot icon={discipline.icon ?? null} />
		<div>
			<h1 data-game>{discipline.name}</h1>
			<p class="identity">
				<a href={kindPath(discipline.kind, data.kinds, '#' + discipline.category.slug)!}
					>{discipline.category.name}</a
				>
			</p>
		</div>
		<p class="intro discipline-intro">
			<LinkedText
				text="A discipline [levels up as you use it](mechanic:discipline-levels): fight with its weapons or cast its spells. Its level unlocks the skills below, and each level gives [proficiency points](mechanic:proficiency-points) for its passives."
			/>
		</p>
	</div>
	<div class="item-relations" data-pagefind-ignore="all">
		<DisciplineTables {discipline} skillFacts={data.skillFacts} />
		<BugsSection notes={discipline.notes} name={discipline.name} />
	</div>
</EntityPage>

<style>
	/* The intro spans the heading under the icon and the title, as a phone has no room beside the icon. */
	.discipline-intro {
		grid-column: 1 / -1;
	}
</style>
