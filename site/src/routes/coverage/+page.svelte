<script lang="ts">
	import { resolve } from '$app/paths';
	import { gaps } from '#lib/labels.js';
	import ExpandableList from '#lib/ExpandableList.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	let { data } = $props();
</script>

<SeoHead
	title="Data coverage | The Bloodline Compendium"
	description="What is still unknown about items, sources and places in this reference."
/>
<p class="eyebrow">Know the limits</p>
<h1>Data coverage</h1>
<p class="intro">
	What this reference cannot yet tell you. Missing information is shown, not guessed.
</p>
<p>
	Every named inventory item is published. Sources are read from game data, not Blueprint scripts or
	a complete playthrough. Known test areas are excluded, but other places may still be unreachable.
</p>
<div class="coverage-stack">
	{#each data.coverage.gaps as gap (gap.kind)}<section class="panel" id={gap.kind}>
			<h2>{gaps[gap.kind].title} <span class="count">{gap.entries.length}</span></h2>
			<p class="muted">{gaps[gap.kind].explanation}</p>
			{#if gap.entries.length}
				<ExpandableList items={gap.entries} initial={8} noun="entries" className="gap-list">
					{#snippet row(entry)}
						{#if entry.itemSlug}<a href={resolve(`items/${entry.itemSlug}/`)}>{entry.label}</a
							>{:else}<span>{entry.label}</span>{/if}
						{#if entry.detail}<p>{entry.detail}</p>{/if}
					{/snippet}
				</ExpandableList>
			{:else}<p>No known gaps of this kind.</p>{/if}
		</section>{/each}
</div>
