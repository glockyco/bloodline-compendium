<script lang="ts">
	import type { RelationCondition } from './site-data.js';
	import EntityLink from './ui/EntityLink.svelte';
	let { conditions }: { conditions: RelationCondition[] } = $props();
</script>

{#if conditions.length}<ul class="relation-conditions">
		{#each conditions as condition, index (index)}
			{@const entity = condition.entity}
			{@const nameIndex = entity ? condition.label.indexOf(entity.name) : -1}
			<li>
				{#if entity && nameIndex >= 0}
					{condition.label.slice(0, nameIndex)}<EntityLink {entity} />{condition.label.slice(
						nameIndex + entity.name.length
					)}
				{:else if entity}
					<!-- Svelte drops whitespace at the start of a block, so the space travels with the label. -->
					{`${condition.label} (`}<EntityLink {entity} />)
				{:else}
					{condition.label}
				{/if}
			</li>{/each}
	</ul>{/if}
