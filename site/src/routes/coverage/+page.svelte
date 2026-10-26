<script lang="ts">
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import { gaps, sourceHeadings } from '#lib/labels.js';
	import Count from '#lib/ui/Count.svelte';
	import ExpandableList from '#lib/ExpandableList.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	let { data } = $props();
</script>

<SeoHead
	title="Coverage | The Bloodline Compendium"
	description="What is still unknown about items, sources and places in this reference."
/>
<p class="eyebrow">Know the limits</p>
<h1>Coverage</h1>
<p class="intro">
	What this reference cannot yet tell you. Missing information is shown, not guessed.
</p>
<p>
	Every named inventory item has a page. Sources are read from game data, not a complete
	playthrough. Some scripts are not yet understood. Known test areas are excluded, but other places
	may still be unreachable.
</p>
<div class="coverage-stack">
	{#each data.coverage.gaps as gap (gap.kind)}<section class="panel" id={gap.kind}>
			<h2>{gaps[gap.kind].title} <Count value={gap.entries.length} /></h2>
			<p class="muted">{gaps[gap.kind].explanation}</p>
			{#if gap.entries.length}
				<ExpandableList items={gap.entries} initial={8} noun="entries" className="gap-list">
					{#snippet row(entry)}
						{#if entry.entity}<EntityLink entity={entry.entity} />{:else}<span data-game
								>{entry.label}</span
							>{/if}
						{#if entry.source}<p>
								{sourceHeadings[entry.source]} <span data-game>{entry.detail}</span>
							</p>{:else if entry.detail}<p>{entry.detail}</p>{/if}
					{/snippet}
				</ExpandableList>
			{:else}<p>No known gaps of this kind.</p>{/if}
		</section>{/each}
</div>
