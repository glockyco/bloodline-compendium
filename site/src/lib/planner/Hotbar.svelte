<script lang="ts">
	import type { PlannerData } from '../site-data.js';
	import { hotbarSlots, type PlannerState } from './state.js';
	/**
	 * The game's skill bar, two rows of five slots, each holding one skill the character knows. A select per slot keeps
	 * it usable by keyboard, and choosing a skill already on the bar moves it.
	 */
	let {
		state,
		data,
		place
	}: {
		state: PlannerState;
		data: PlannerData;
		place: (slot: number, skill: string | null) => void;
	} = $props();
	const names = $derived(new Map(data.skills.map((skill) => [skill.key, skill.name])));
	const known = $derived(
		[...new Set([...state.skills, ...state.learned])]
			.map((key) => ({ key, name: names.get(key) ?? 'A skill this game version lacks' }))
			.sort((a, b) => a.name.localeCompare(b.name, 'en'))
	);
	const rows = [0, 1];
	const perRow = hotbarSlots / rows.length;
</script>

<div class="bar">
	{#each rows as row (row)}<div class="row" role="group" aria-label="Row {row + 1}">
			{#each Array.from({ length: perRow }, (_, index) => row * perRow + index) as slot (slot)}<label
					><span class="field-label">{slot - row * perRow + 1}</span><select
						aria-label="Row {row + 1}, slot {slot - row * perRow + 1}"
						value={state.hotbar[slot] ?? ''}
						onchange={(event) => {
							place(slot, event.currentTarget.value || null);
							// A refused placement leaves the slot as it was, so the select shows the state again.
							event.currentTarget.value = state.hotbar[slot] ?? '';
						}}
						><option value="">Empty</option>{#each known as skill (skill.key)}<option
								value={skill.key}>{skill.name}</option
							>{/each}</select
					></label
				>{/each}
		</div>{/each}
	{#if !known.length}<p class="muted">Unlock or learn a skill to place it on the bar.</p>{/if}
</div>

<style>
	.bar {
		display: grid;
		gap: var(--gap-block);
	}
	.row {
		display: grid;
		grid-template-columns: repeat(5, minmax(0, 1fr));
		gap: var(--gap-inline);
	}
	select {
		width: 100%;
	}
	@media (width < 40rem) {
		.row {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}
	}
</style>
