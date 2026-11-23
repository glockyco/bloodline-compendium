<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { formatNumber, plural } from '../format.js';
	import { activeCharacter, loadStore, localBackend, type Character } from './store.js';
	import type { SkillsSection } from './state.js';
	/**
	 * What the planner's active character has against this page: a skill's required level, or a discipline's level and
	 * unlocked skills. It reads only the characters stored in the browser, never the planner's inputs, and shows
	 * nothing without an active character or without JavaScript.
	 */
	let {
		skill = null,
		discipline = null
	}: {
		/** A skill page's skill and the levels its trees need, by discipline key. */
		skill?: { key: string; trees: { discipline: string; name: string; level: number }[] } | null;
		/** A discipline page's discipline and the keys of its skills. */
		discipline?: { key: string; name: string; skills: string[] } | null;
	} = $props();
	let character: Character | null = $state(null);
	onMount(() => {
		character = activeCharacter(loadStore(localBackend()).store);
	});
	// A character saved by an earlier planner may lack a field, which reads as empty.
	const skills: Partial<SkillsSection> | undefined = $derived(
		(character as Character | null)?.state.skills
	);
	const levelIn = (key: string) => skills?.disciplines?.[key] ?? 1;
	const line = $derived.by(() => {
		if (!character) return null;
		const name = character.name;
		if (skill) {
			if ((skills?.unlocked ?? []).includes(skill.key)) return `${name} has unlocked this skill.`;
			if ((skills?.learned ?? []).includes(skill.key)) return `${name} knows this skill.`;
			const tree = skill.trees[0];
			if (!tree) return `${name} has not learned this skill.`;
			const level = levelIn(tree.discipline);
			return level >= tree.level
				? `${name} can unlock this skill: ${tree.name} is at level ${formatNumber(level)}, and it needs ${formatNumber(tree.level)}.`
				: `${name} needs ${tree.name} level ${formatNumber(tree.level)} for this skill. It is at level ${formatNumber(level)}.`;
		}
		if (discipline) {
			const unlocked = discipline.skills.filter((key) =>
				(skills?.unlocked ?? []).includes(key)
			).length;
			return `${name} has ${discipline.name} at level ${formatNumber(levelIn(discipline.key))}, with ${formatNumber(unlocked)} of its ${plural(discipline.skills.length, 'skill', 'skills')} unlocked.`;
		}
		return null;
	});
</script>

{#if line}<p class="cue">
		{line} <a href={resolve('planner/')}>Open in the Character Planner</a>
	</p>{/if}

<style>
	.cue {
		margin: 0;
		padding: var(--inset-cell) var(--inset);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
	}
</style>
