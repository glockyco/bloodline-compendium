<script lang="ts">
	import type { PlannerData } from '../site-data.js';
	import { hotbarSlots, type PlannerState } from './state.js';
	/**
	 * The game's skill bar, two rows of five slots. Each slot shows its skill's icon and whole name. Choosing a slot
	 * lists the skills the character knows, and choosing one places it there, moving it from any other slot.
	 */
	let {
		plan,
		data,
		place
	}: {
		plan: PlannerState;
		data: PlannerData;
		place: (slot: number, skill: string | null) => void;
	} = $props();
	const skills = $derived(new Map(data.skills.map((skill) => [skill.key, skill])));
	const known = $derived(
		[...new Set([...plan.skills, ...plan.learned])]
			.map((key) => ({
				key,
				name: skills.get(key)?.name ?? 'A skill this game version lacks',
				icon: skills.get(key)?.icon ?? null
			}))
			.sort((a, b) => a.name.localeCompare(b.name, 'en'))
	);
	const perRow = hotbarSlots / 2;
	let open: number | null = $state(null);
	const label = (slot: number) =>
		`Row ${Math.floor(slot / perRow) + 1}, slot ${(slot % perRow) + 1}`;
</script>

<div class="bar">
	{#each [0, 1] as row (row)}<div class="row" role="group" aria-label="Row {row + 1}">
			{#each Array.from({ length: perRow }, (_, index) => row * perRow + index) as slot (slot)}
				{@const skill = plan.hotbar[slot] ? skills.get(plan.hotbar[slot]!) : undefined}<button
					type="button"
					class="slot"
					class:open={open === slot}
					aria-label="{label(slot)}: {plan.hotbar[slot]
						? (skill?.name ?? 'a skill this game version lacks')
						: 'empty'}"
					aria-expanded={open === slot}
					onclick={() => (open = open === slot ? null : slot)}
					><span class="number">{(slot % perRow) + 1}</span>{#if skill?.icon}<img
							src="/icons/{skill.icon}-48.webp"
							alt=""
							width="24"
							height="24"
							loading="lazy"
						/>{/if}<span class="name" data-game={skill ? true : undefined}
						>{plan.hotbar[slot] ? (skill?.name ?? 'Unknown skill') : 'Empty'}</span
					></button
				>{/each}
		</div>{/each}
	{#if open !== null}<div class="picker" role="group" aria-label="Skills for {label(open)}">
			{#each known as entry (entry.key)}<button
					type="button"
					aria-pressed={plan.hotbar[open] === entry.key}
					onclick={() => {
						place(open!, entry.key);
						open = null;
					}}
					>{#if entry.icon}<img
							src="/icons/{entry.icon}-48.webp"
							alt=""
							width="24"
							height="24"
							loading="lazy"
						/>{/if}<span data-game>{entry.name}</span></button
				>{/each}
			{#if plan.hotbar[open]}<button
					type="button"
					onclick={() => {
						place(open!, null);
						open = null;
					}}>Empty This Slot</button
				>{/if}
			{#if !known.length}<p class="muted">Unlock or learn a skill to place it on the bar.</p>{/if}
		</div>{/if}
</div>

<style>
	.bar {
		display: grid;
		gap: var(--gap-inline);
	}
	.row {
		display: grid;
		grid-template-columns: repeat(5, minmax(0, 1fr));
		gap: var(--gap-inline);
	}
	.slot {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr);
		grid-template-rows: auto auto;
		align-content: start;
		align-items: center;
		justify-items: start;
		gap: var(--gap-line) var(--gap-inline);
		min-height: var(--size-control-large);
		text-align: start;
	}
	.slot.open {
		border-color: var(--accent);
	}
	.number {
		grid-column: 1 / -1;
		color: var(--text-muted);
		font-size: var(--type-meta);
	}
	.name {
		overflow-wrap: anywhere;
	}
	.slot:not(:has(img)) .name {
		grid-column: 1 / -1;
	}
	img {
		width: var(--size-icon-inline);
		height: var(--size-icon-inline);
	}
	.picker {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline);
		padding-top: var(--gap-inline);
	}
	.picker button {
		display: inline-flex;
		align-items: center;
		gap: var(--gap-inline);
	}
	@media (width < 40rem) {
		.row {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}
	}
</style>
