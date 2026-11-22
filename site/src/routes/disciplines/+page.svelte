<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
	// The categories in the skill window's order, which the catalog keeps.
	const categories = $derived([
		...new Set(data.index.disciplines.map((discipline) => discipline.category))
	]);
	function counts(discipline: (typeof data.index.disciplines)[number]): string {
		return [
			discipline.skills ? `${formatNumber(discipline.skills)} skills` : null,
			discipline.passives ? `${formatNumber(discipline.passives)} passives` : null,
			discipline.spellsongs ? `${formatNumber(discipline.spellsongs)} spellsongs` : null
		]
			.filter(Boolean)
			.join(', ');
	}
</script>

<SeoHead
	title="Disciplines | The Bloodline Compendium"
	description="Every discipline of The Bloodline's skill window, with its skills by level and its passives with their cost per rank."
/>
<h1 class="list-title">Disciplines</h1>
<div class="categories">
	{#each categories as category (category)}
		<section aria-labelledby={'category-' + category}>
			<h2 id={'category-' + category}>{category}</h2>
			<ul>
				{#each data.index.disciplines.filter((discipline) => discipline.category === category) as discipline (discipline.key)}
					<li>
						<EntityLink
							class="entity-name"
							entity={{
								kind: 'discipline',
								key: discipline.key,
								name: discipline.name,
								slug: discipline.slug,
								icon: null
							}}
						/>
						<small>{counts(discipline)}</small>
					</li>
				{/each}
			</ul>
		</section>
	{/each}
</div>

<style>
	.list-title {
		margin-bottom: var(--gap-groups);
	}
	.categories {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(var(--size-card-narrow), 1fr));
		gap: var(--gap-section);
		align-items: start;
	}
	section {
		display: grid;
		gap: var(--gap-block);
	}
	h2 {
		margin: 0;
		font-size: var(--type-section);
	}
	ul {
		display: grid;
		gap: var(--gap-block);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	li {
		display: grid;
		gap: var(--gap-line);
	}
	small {
		color: var(--text-muted);
		font-size: var(--type-0);
	}
</style>
