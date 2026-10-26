<script lang="ts">
	import { resolve } from '$app/paths';
	import { gaps } from '#lib/labels.js';
	let { data } = $props();
</script>

<svelte:head
	><title>Data coverage | The Bloodline Compendium</title><meta
		name="description"
		content="What is still unknown about items, sources and places in this reference."
	/></svelte:head
>
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
			{#if gap.entries.length}<ul class="gap-list">
					{#each gap.entries as entry, index (index)}<li>
							{#if entry.itemSlug}<a href={resolve(`items/${entry.itemSlug}/`)}>{entry.label}</a
								>{:else}<span>{entry.label}</span>{/if}{#if entry.detail}<p>{entry.detail}</p>{/if}
						</li>{/each}
				</ul>{:else}<p>No known gaps of this kind.</p>{/if}
		</section>{/each}
</div>
