<script lang="ts">
	/**
	 * Finds what gives an item in a place: part of a name, or one kind of item, narrows every card to the things that
	 * give it. The kinds are those of the place's items, each with how many there are.
	 */
	let {
		kinds,
		query = $bindable(''),
		selected = $bindable(null)
	}: {
		kinds: { slug: string; label: string; count: number }[];
		query?: string;
		selected?: string | null;
	} = $props();
</script>

<div class="finder surface">
	<label class="search"
		><span class="field-label">Find what gives an item</span><input
			type="search"
			bind:value={query}
			placeholder="Item name"
		/></label
	>
	<div class="kinds" role="group" aria-label="Kind of item">
		<button
			type="button"
			class="chip"
			aria-pressed={selected === null}
			onclick={() => (selected = null)}>Any kind</button
		>
		{#each kinds as kind (kind.slug)}<button
				type="button"
				class="chip"
				aria-pressed={selected === kind.slug}
				onclick={() => (selected = selected === kind.slug ? null : kind.slug)}
				>{kind.label} <span class="count">{kind.count}</span></button
			>{/each}
	</div>
</div>

<style>
	.finder {
		display: grid;
		gap: var(--gap-block);
	}
	.search {
		display: grid;
		gap: var(--gap-line);
		max-width: var(--size-name-search);
	}
	.kinds {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline);
	}
	.chip {
		display: inline-flex;
		align-items: center;
		gap: var(--gap-inline);
		min-height: var(--size-chip);
		padding: 0 var(--inset);
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-round);
		background: var(--surface-panel);
		color: var(--text-soft);
		font-size: var(--type-meta);
	}
	.chip[aria-pressed='true'] {
		border-color: var(--accent);
		background: var(--surface-raised);
		color: var(--text);
	}
	.count {
		color: var(--text-muted);
	}
</style>
