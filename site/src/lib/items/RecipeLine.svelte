<script lang="ts">
	import type { Source } from '../site-data.js';
	import { formatNumber } from '../format.js';
	import EntityLink from '../ui/EntityLink.svelte';
	import Ingredients from './Ingredients.svelte';
	import Conditions from '../Conditions.svelte';
	/** A recipe in full: what it consumes, where it is made, the profession it needs and what it gives. */
	let { source, id }: { source: Source; id: string } = $props();
	const recipe = $derived(source.recipe);
	// The recipe's own level shows in its facts, so only other requirements stay as conditions.
	const conditions = $derived(
		source.conditions.filter(
			(condition) => !(recipe?.level != null && condition.kind === 'proficiencyLevel')
		)
	);
</script>

<div class="recipe" {id} data-relation-id={source.relationId} data-relation-direction="incoming">
	<Ingredients inputs={source.inputs} />
	<dl class="recipe-facts">
		{#if recipe?.station}<div>
				<dt>Made at</dt>
				<dd>{recipe.station.name}</dd>
			</div>{/if}
		{#if recipe?.profession}<div>
				<dt>Profession</dt>
				<dd>
					<EntityLink
						entity={recipe.profession}
					/>{#if recipe.level != null}{`, level ${formatNumber(recipe.level)}`}{/if}
				</dd>
			</div>{/if}
		<div>
			<dt>Makes</dt>
			<dd>{formatNumber(recipe?.output ?? source.amount?.min ?? 1)}</dd>
		</div>
		{#if recipe?.experience}<div>
				<dt>Experience</dt>
				<dd>{formatNumber(recipe.experience)}</dd>
			</div>{/if}
	</dl>
	<Conditions {conditions} />
	{#if source.notes.length}<ul class="recipe-notes">
			{#each source.notes as note, index (index)}<li>{note}</li>{/each}
		</ul>{/if}
</div>

<style>
	.recipe {
		display: grid;
		gap: var(--gap-block);
		padding: var(--inset);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-2);
		background: var(--surface-panel);
	}
	/* Each fact is a field, its label above its value, as wide as its value, so a profession and its level stay on
	   one line. */
	.recipe-facts {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-block) var(--gap-section);
		margin: 0;
	}
	.recipe-facts div {
		display: grid;
		align-content: start;
		gap: var(--gap-line);
	}
	.recipe-facts dt {
		color: var(--text-muted);
		font-size: var(--type-meta);
		font-weight: var(--weight-strong);
		line-height: var(--leading-meta);
	}
	.recipe-facts dd {
		line-height: var(--leading-data);
	}
	.recipe-notes {
		margin: 0;
		padding-left: var(--inset-loose);
	}
</style>
