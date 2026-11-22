<script lang="ts">
	import RelationTable from '../RelationTable.svelte';
	import EntityLink from '../ui/EntityLink.svelte';
	import { formatNumber } from '../format.js';
	import { rankCosts } from '../rules/progression.js';
	import type { EntityReference, PlannerData, PlannerDiscipline } from '../site-data.js';
	import type { Evaluation } from './evaluate.js';
	import { levelOf } from './evaluate.js';
	import type { PlannerState } from './state.js';
	/**
	 * One discipline as the planner edits it: its planned level and points, its skills by the level that unlocks each
	 * with a box to unlock it, and its passives with buttons to buy or remove a rank. The rows follow the discipline
	 * page's, so a reader plans in the layout the game shows.
	 */
	let {
		discipline,
		data,
		state,
		evaluation,
		setLevel,
		toggleSkill,
		buyRank,
		removeRank
	}: {
		discipline: PlannerDiscipline;
		data: PlannerData;
		state: PlannerState;
		evaluation: Evaluation;
		setLevel: (level: number) => void;
		toggleSkill: (skill: string) => void;
		buyRank: (passive: string) => void;
		removeRank: (passive: string) => void;
	} = $props();
	const names = $derived(new Map(data.disciplines.map((entry) => [entry.key, entry.name])));
	const skills = $derived(new Map(data.skills.map((skill) => [skill.key, skill])));
	const level = $derived(levelOf(state, discipline.key));
	const points = $derived(evaluation.proficiency[discipline.key]);
	const slots = $derived([...discipline.slots].sort((a, b) => a.requiredLevel - b.requiredLevel));
	const passives = $derived(
		[...discipline.passives].sort((a, b) => a.requiredLevel - b.requiredLevel)
	);
	function reference(key: string): EntityReference {
		const skill = skills.get(key);
		return {
			kind: 'skill',
			key,
			name: skill?.name ?? 'A skill this game version lacks',
			slug: skill?.slug ?? null,
			icon: skill?.icon ?? null
		};
	}
	// A passive links to its row on the discipline page, where its tooltip shows each rank's cost.
	const passiveReference = (passive: PlannerDiscipline['passives'][number]): EntityReference => ({
		kind: 'passive',
		key: passive.key,
		name: passive.name,
		slug: null,
		icon: passive.icon,
		fragment: passive.anchor,
		host: {
			kind: 'discipline',
			key: discipline.key,
			name: discipline.name,
			slug: discipline.slug,
			icon: null,
			fragment: null
		}
	});
	// An entry that checks another discipline's level names it, as the discipline page does.
	const needs = (required: number, checked: string) =>
		`Unlocks at ${names.get(checked) ?? 'another discipline'} level ${formatNumber(required)}`;
	const unlocked = (key: string) => state.skills.includes(key);
	const ranksOf = (key: string) => state.passives[key] ?? 0;
</script>

<div class="tree">
	<div class="tree-head">
		<label class="level"
			><span class="field-label">{discipline.name} Level</span><input
				type="number"
				min="1"
				value={level}
				onchange={(event) => setLevel(Number(event.currentTarget.value))}
			/></label
		>
		{#if points}<p class="points">
				{#if points.remaining === null}Enter your Blood Magic points below to plan its passives.{:else}{formatNumber(
						points.remaining
					)} of {formatNumber(points.earned ?? 0)} proficiency points left{/if}
			</p>{/if}
	</div>
	{#if slots.length}<RelationTable
			items={slots}
			label="{discipline.name} skills"
			headings={['Skill', 'Unlocked']}
			collapse={false}
			>{#snippet row(
				slot,
				index
			)}{#if index === 0 || slots[index - 1].requiredLevel !== slot.requiredLevel}<tr
						class="group-heading"
						><th colspan="2" scope="colgroup">Level {formatNumber(slot.requiredLevel)}</th></tr
					>{/if}<tr class:locked={levelOf(state, slot.levelDiscipline) < slot.requiredLevel}
					><td
						><EntityLink
							entity={reference(slot.skill)}
							icon
						/>{#if slot.levelDiscipline !== discipline.key}<small
								>{needs(slot.requiredLevel, slot.levelDiscipline)}</small
							>{/if}</td
					><td class="numeric"
						><input
							type="checkbox"
							aria-label="Unlock {reference(slot.skill).name}"
							checked={unlocked(slot.skill)}
							onchange={(event) => {
								toggleSkill(slot.skill);
								// A refused unlock leaves the state as it was, so the box shows the state again.
								event.currentTarget.checked = unlocked(slot.skill);
							}}
						/></td
					></tr
				>{/snippet}</RelationTable
		>{/if}
	{#if passives.length}<RelationTable
			items={passives}
			label="{discipline.name} passives"
			headings={['Passive', 'Ranks']}
			collapse={false}
			>{#snippet row(
				passive,
				index
			)}{#if index === 0 || passives[index - 1].requiredLevel !== passive.requiredLevel}<tr
						class="group-heading"
						><th colspan="2" scope="colgroup">Level {formatNumber(passive.requiredLevel)}</th></tr
					>{/if}<tr class:locked={levelOf(state, passive.levelDiscipline) < passive.requiredLevel}
					><td
						><EntityLink
							entity={passiveReference(passive)}
							icon
						/>{#if passive.levelDiscipline !== discipline.key}<small
								>{needs(passive.requiredLevel, passive.levelDiscipline)}</small
							>{/if}</td
					><td class="numeric"
						><span class="ranks"
							><button
								type="button"
								aria-label="Remove a {passive.name} rank"
								disabled={ranksOf(passive.key) < 1}
								onclick={() => removeRank(passive.key)}>−</button
							><output aria-label="{passive.name} ranks"
								>{ranksOf(passive.key)} of {passive.ranks ?? 1}</output
							><button
								type="button"
								aria-label="Buy a {passive.name} rank"
								disabled={ranksOf(passive.key) >= rankCosts(passive).length}
								onclick={() => buyRank(passive.key)}>+</button
							></span
						></td
					></tr
				>{/snippet}</RelationTable
		>{/if}
</div>

<style>
	.tree {
		display: grid;
		gap: var(--gap-block);
	}
	.tree-head {
		display: flex;
		flex-wrap: wrap;
		align-items: end;
		gap: var(--gap-block) var(--gap-groups);
	}
	.level input {
		width: var(--size-filter-control);
	}
	.points {
		margin: 0;
		color: var(--text-muted);
	}
	.locked {
		color: var(--text-muted);
	}
	.ranks {
		display: inline-flex;
		align-items: center;
		gap: var(--gap-inline);
	}
	.ranks button {
		min-width: var(--size-control);
		padding: 0;
	}
	td small {
		display: block;
		color: var(--text-muted);
	}
</style>
