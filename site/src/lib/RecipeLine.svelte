<script lang="ts">
	import type { Source } from './site-data.js';
	import { formatNumber } from './format.js';
	import EntityLink from './ui/EntityLink.svelte';
	import Conditions from './Conditions.svelte';
	/** A recipe in full: what it consumes, where it is made, the skill it needs and what it gives. */
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
	<ul class="ingredients" aria-label="Ingredients">
		{#each source.inputs as input, index (index)}<li
				data-relation-id={input.relationId}
				data-relation-direction={input.relationId ? 'incoming' : undefined}
			>
				<!-- The icon, quantity and name are one target, as a thumb aims at the whole ingredient. -->
				<EntityLink entity={input.item} icon
					><span class="quantity">{formatNumber(input.quantity)}</span>
					<span>{input.item.name}</span></EntityLink
				>
			</li>{/each}
	</ul>
	<dl class="recipe-facts">
		{#if recipe?.station}<div>
				<dt>Made at</dt>
				<dd>{recipe.station.name}</dd>
			</div>{/if}
		{#if recipe?.discipline}<div>
				<dt>Skill</dt>
				<dd>
					{recipe.discipline}{#if recipe.level != null}, level {formatNumber(recipe.level)}{/if}
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
	.ingredients {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline) var(--gap-block);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.ingredients li {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
	}
	.ingredients :global(a) {
		min-height: var(--size-control-compact);
	}
	.ingredients :global(.entity-icon) {
		width: var(--size-icon-row);
		height: var(--size-icon-row);
		border-radius: var(--radius-1);
		background: var(--surface-slot);
	}
	.quantity {
		font-weight: var(--weight-strong);
	}
	/* Each fact is a field, its label above its value, in columns of one width. */
	.recipe-facts {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(var(--size-filter-control), 1fr));
		gap: var(--gap-block) var(--gap-groups);
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
