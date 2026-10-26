<script lang="ts">
	import type { RelationCondition } from './site-data.js';
	import EntityLink from './ui/EntityLink.svelte';
	import { formatNumber } from './format.js';
	let { conditions }: { conditions: RelationCondition[] } = $props();
</script>

{#if conditions.length}<ul class="relation-conditions">
		{#each conditions as condition, index (index)}<li>
				{condition.label}{#if condition.entity}
					<EntityLink entity={condition.entity} />{/if}{#if condition.value !== null}
					{formatNumber(condition.value)}{/if}
			</li>{/each}
	</ul>{/if}
