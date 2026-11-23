<script lang="ts">
	import { tick } from 'svelte';
	import type { PlannerData } from '../site-data.js';
	import { hotbarSlots, type PlannerState } from './state.js';
	/**
	 * The game's skill bar, two rows of five slots, drawn as the game draws it: an icon tile with the slot's key, and
	 * the skill's name below. Choosing a slot opens a chooser with a search field, listing the skills the character
	 * knows and then the skills a book, a conversation or an event teaches. Choosing a taught skill marks it known,
	 * because the bar is what a player marks it for. Focus moves to the search field, Enter there places the first
	 * match, and closing returns focus to the slot.
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
	type Choice = { key: string; name: string; icon: string | null };
	const skills = $derived(new Map(data.skills.map((skill) => [skill.key, skill])));
	const byName = (a: Choice, b: Choice) => a.name.localeCompare(b.name, 'en');
	const knownKeys = $derived(new Set([...plan.skills.unlocked, ...plan.skills.learned]));
	const known = $derived(
		[...knownKeys]
			.map((key) => ({
				key,
				name: skills.get(key)?.name ?? 'A skill this game version lacks',
				icon: skills.get(key)?.icon ?? null
			}))
			.sort(byName)
	);
	const taught = $derived(
		data.skills
			.filter((skill) => skill.learned && !knownKeys.has(skill.key))
			.map((skill) => ({ key: skill.key, name: skill.name, icon: skill.icon }))
			.sort(byName)
	);
	let query = $state('');
	const matches = (entry: Choice) =>
		entry.name.toLocaleLowerCase('en').includes(query.trim().toLocaleLowerCase('en'));
	const groups = $derived(
		[
			{ title: 'Known', entries: known.filter(matches) },
			{ title: 'Taught by a Book or Event', entries: taught.filter(matches) }
		].filter((group) => group.entries.length)
	);
	const perRow = hotbarSlots / 2;
	let open: number | null = $state(null);
	const slotButtons: HTMLButtonElement[] = $state([]);
	let search: HTMLInputElement | undefined = $state();
	const label = (slot: number) =>
		`Row ${Math.floor(slot / perRow) + 1}, slot ${(slot % perRow) + 1}`;
	async function toggle(slot: number) {
		open = open === slot ? null : slot;
		query = '';
		if (open === null) return;
		await tick();
		search?.focus();
	}
	function close(slot: number) {
		open = null;
		slotButtons[slot]?.focus();
	}
	function choose(skill: string | null) {
		place(open!, skill);
		close(open!);
	}
	function keydown(event: KeyboardEvent) {
		if (event.key === 'Escape' && open !== null) close(open);
	}
</script>

<div class="bar">
	{#each [0, 1] as row (row)}<div class="row" role="group" aria-label="Row {row + 1}">
			{#each Array.from({ length: perRow }, (_, index) => row * perRow + index) as slot (slot)}
				{@const key = plan.skillBars.main[slot]}
				{@const skill = key ? skills.get(key) : undefined}<button
					type="button"
					class="slot"
					class:open={open === slot}
					class:filled={key !== null}
					bind:this={slotButtons[slot]}
					aria-label="{label(slot)}: {key
						? (skill?.name ?? 'a skill this game version lacks')
						: 'empty'}"
					aria-expanded={open === slot}
					aria-controls={open === slot ? 'hotbar-picker' : undefined}
					onclick={() => toggle(slot)}
					><span class="tile"
						><span class="number">{(slot % perRow) + 1}</span>{#if skill?.icon}<img
								src="/icons/{skill.icon}-160.webp"
								alt=""
								width="48"
								height="48"
								loading="lazy"
							/>{/if}</span
					><span class="name" data-game={skill ? true : undefined}
						>{key ? (skill?.name ?? 'Unknown skill') : ''}</span
					></button
				>{/each}
		</div>{/each}
	{#if open !== null}<div
			class="picker"
			id="hotbar-picker"
			role="group"
			aria-label="Skills for {label(open)}"
		>
			<div class="picker-head">
				<input
					type="search"
					bind:this={search}
					bind:value={query}
					aria-label="Search skills"
					placeholder="Search skills"
					onkeydown={(event) => {
						keydown(event);
						const first = groups[0]?.entries[0];
						if (event.key !== 'Enter' || !first) return;
						// The key would otherwise press the slot that receives focus and reopen the chooser.
						event.preventDefault();
						choose(first.key);
					}}
				/>{#if plan.skillBars.main[open]}<button
						type="button"
						onkeydown={keydown}
						onclick={() => choose(null)}>Empty This Slot</button
					>{/if}
			</div>
			{#each groups as group (group.title)}<h3 class="group-title">{group.title}</h3>
				<div class="choices">
					{#each group.entries as entry (entry.key)}<button
							type="button"
							aria-pressed={plan.skillBars.main[open] === entry.key}
							onkeydown={keydown}
							onclick={() => choose(entry.key)}
							>{#if entry.icon}<img
									src="/icons/{entry.icon}-48.webp"
									alt=""
									width="24"
									height="24"
									loading="lazy"
								/>{/if}<span data-game>{entry.name}</span></button
						>{/each}
				</div>{:else}<p class="muted">
					{query.trim()
						? 'No skill matches.'
						: 'Unlock a skill in the Skills tab to place it on the bar.'}
				</p>{/each}
		</div>{/if}
</div>

<style>
	.bar {
		display: grid;
		gap: var(--gap-block);
	}
	.row {
		display: grid;
		grid-template-columns: repeat(5, minmax(0, var(--size-version-slot)));
		gap: var(--gap-block);
	}
	.slot {
		display: grid;
		justify-items: center;
		align-content: start;
		gap: var(--gap-inline);
		min-width: 0;
		padding: 0;
		border: 0;
		background: none;
		text-align: center;
	}
	.tile {
		position: relative;
		display: grid;
		place-items: center;
		width: 100%;
		aspect-ratio: 1;
		border: var(--marker-width) solid var(--slot-edge);
		border-radius: var(--radius-1);
		background: var(--surface-slot);
	}
	.filled .tile {
		border-color: var(--tooltip-frame);
	}
	.slot.open .tile,
	.slot:hover .tile {
		border-color: var(--accent);
	}
	.tile img {
		width: 70%;
		height: auto;
	}
	.number {
		position: absolute;
		top: 0;
		left: 0;
		padding: 0 var(--gap-inline);
		border-bottom-right-radius: var(--radius-1);
		background: var(--surface-panel);
		color: var(--text-muted);
		line-height: var(--leading-meta);
		font-size: var(--type-0);
	}
	.name {
		font-size: var(--type-0);
		line-height: var(--leading-meta);
		overflow-wrap: normal;
		hyphens: auto;
	}
	.picker {
		display: grid;
		gap: var(--gap-block);
		padding: var(--inset);
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
	}
	.picker-head {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline);
	}
	.picker-head input {
		flex: 1 1 var(--size-filter-control);
	}
	.group-title {
		margin: 0;
		color: var(--text-muted);
		font-size: var(--type-0);
	}
	.choices {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline);
	}
	.choices button {
		display: inline-flex;
		align-items: center;
		gap: var(--gap-inline);
	}
	.choices img {
		width: var(--size-icon-inline);
		height: var(--size-icon-inline);
	}
</style>
