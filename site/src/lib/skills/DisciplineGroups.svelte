<script lang="ts">
	import IconTile from '../ui/IconTile.svelte';
	import { entityPath } from '../entities.js';
	import { plural } from '../format.js';
	import type { DisciplineSummary, EntityKindDefinition } from '../site-data.js';
	/**
	 * Disciplines grouped by the skill window's categories in its order, one column of tiles each, as the home page and
	 * the discipline list show them. Each category's heading is the anchor a discipline's category links to.
	 */
	let {
		disciplines,
		kinds,
		heading = 'h3'
	}: {
		disciplines: DisciplineSummary[];
		kinds: EntityKindDefinition[];
		heading?: 'h2' | 'h3';
	} = $props();
	const groups = $derived(
		[...new Map(disciplines.map((entry) => [entry.category.slug, entry.category])).values()].map(
			(category) => ({
				category,
				members: disciplines.filter((entry) => entry.category.slug === category.slug)
			})
		)
	);
	const meta = (discipline: DisciplineSummary) =>
		discipline.skills
			? plural(discipline.skills, 'skill', 'skills')
			: plural(discipline.spellsongs, 'spellsong', 'spellsongs');
</script>

<div class="discipline-groups">
	{#each groups as { category, members } (category.slug)}<section aria-labelledby={category.slug}>
			<svelte:element this={heading} id={category.slug} class="group-name"
				>{category.name}</svelte:element
			>
			<ul class="tile-list">
				{#each members as discipline (discipline.key)}<li>
						<IconTile
							href={entityPath(
								{
									kind: 'discipline',
									key: discipline.key,
									name: discipline.name,
									slug: discipline.slug,
									icon: null
								},
								kinds
							)!}
							icon={discipline.icon ?? null}
							name={discipline.name}
							meta={meta(discipline)}
							game
						/>
					</li>{/each}
			</ul>
		</section>{/each}
</div>

<style>
	/* Four columns at full width, one per category of the skill window, which narrow screens stack. */
	.discipline-groups {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, var(--size-card-narrow)), 1fr));
		gap: var(--gap-section) var(--gap-block);
		align-items: start;
	}
	section {
		display: grid;
		gap: var(--gap-inline);
	}
	.group-name {
		margin: 0;
		font-size: var(--type-sub);
		line-height: var(--leading-heading);
	}
	/* One column under each category at full width, and two on a phone, where categories stack. */
	.tile-list {
		grid-template-columns: repeat(auto-fill, minmax(var(--size-tile), 1fr));
		display: grid;
		gap: var(--gap-inline);
		margin: 0;
		padding: 0;
		list-style: none;
	}
</style>
