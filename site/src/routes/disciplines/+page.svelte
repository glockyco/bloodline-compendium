<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import SectionHeading from '#lib/ui/SectionHeading.svelte';
	import CardGrid from '#lib/ui/CardGrid.svelte';
	import Card from '#lib/ui/Card.svelte';
	import { entityPath } from '#lib/entities.js';
	import { disciplineContents } from '#lib/skills/describe.js';
	let { data } = $props();
	// The categories in the skill window's order, which the catalog keeps.
	const categories = $derived(
		[...new Map(data.index.disciplines.map((entry) => [entry.category.slug, entry.category]))].map(
			([, category]) => category
		)
	);
</script>

<SeoHead
	title="Disciplines | The Bloodline Compendium"
	description="Every discipline of The Bloodline's skill window, with its skills by level and its passives with their cost per rank."
/>
<h1 class="list-title">Disciplines</h1>
<div class="card-sections">
	{#each categories as category (category.slug)}
		<!-- Each category's anchor is the one a discipline page's category links to. -->
		<section aria-labelledby={category.slug}>
			<SectionHeading id={category.slug} title={category.name} />
			<CardGrid rows={2}>
				{#each data.index.disciplines.filter((entry) => entry.category.slug === category.slug) as discipline (discipline.key)}
					<Card
						href={entityPath(
							{
								kind: 'discipline',
								key: discipline.key,
								name: discipline.name,
								slug: discipline.slug,
								icon: null
							},
							data.kinds
						)!}
					>
						<h3 data-game>{discipline.name}</h3>
						<span class="muted">{disciplineContents(discipline)}</span>
					</Card>
				{/each}
			</CardGrid>
		</section>
	{/each}
</div>
