<script lang="ts">
	import type { DisciplinePage, Fact } from '../site-data.js';
	import RelationTable from '../RelationTable.svelte';
	import Section from '../ui/Section.svelte';
	import EntityLink from '../ui/EntityLink.svelte';
	import { formatNumber } from '../format.js';
	import { factValue } from '../stats.js';
	import { rankCosts } from '../rules/progression.js';
	import Tune from './Tune.svelte';
	/**
	 * A discipline as item pages show relations: its skills, passives and spellsongs each in one table, grouped by the
	 * level row the game's window shows them in.
	 */
	let {
		discipline,
		skillFacts
	}: { discipline: DisciplinePage; skillFacts: Record<string, Fact[]> } = $props();
	const level = $derived(discipline.levelName ?? discipline.name);
	// The relations each entry shows, by the key of the entity at their other end, so the static page carries them.
	const relationIds = $derived(
		Map.groupBy(discipline.relations, (relation) => relation.entity.key ?? '')
	);
	const ids = (key: string | null) =>
		(relationIds.get(key ?? '') ?? []).map((relation) => relation.id).join(' ') || undefined;
	const slots = $derived(
		discipline.skillRows.flatMap((row) => row.slots.map((slot) => ({ row: row.level, slot })))
	);
	const passives = $derived(
		discipline.passiveRows.flatMap((row) =>
			row.passives.map((passive) => ({ row: row.level, passive }))
		)
	);
	const fact = (key: string | null, ...stats: string[]) =>
		(skillFacts[key ?? ''] ?? []).find((entry) => stats.includes(entry.stat));
	function cost(key: string | null): string {
		const found = fact(key, 'manaCost', 'staminaCost');
		if (!found) return '';
		const value = factValue(found);
		return value == null ? 'Unknown' : `${value} ${found.stat === 'manaCost' ? 'mana' : 'stamina'}`;
	}
	function points(costs: number[]): string {
		if (costs.every((value) => value === 0)) return 'Free';
		return costs.map(formatNumber).join(', ');
	}
</script>

{#if slots.length}<Section id="skills" title="Skills" count={slots.length}>
		<RelationTable
			items={slots}
			label="Skills"
			headings={['Skill', 'Cooldown', 'Cost']}
			collapse={false}
			>{#snippet row({ row, slot }, index)}{#if index === 0 || slots[index - 1].row !== row}<tr
						class="place-heading"
						><th colspan="3" scope="colgroup">{level} Level {formatNumber(row)}</th></tr
					>{/if}<tr data-relation-id={ids(slot.skill.key)} data-relation-direction="outgoing"
					><td
						><EntityLink
							entity={slot.skill}
							icon
						/>{#if slot.requiredLevel !== row || slot.checkedLevel !== level}<small
								>Unlocks at {slot.checkedLevel} level {formatNumber(slot.requiredLevel)}</small
							>{/if}</td
					><td class="numeric"
						>{fact(slot.skill.key, 'cooldown')
							? factValue(fact(slot.skill.key, 'cooldown')!)
							: ''}</td
					><td class="numeric">{cost(slot.skill.key)}</td></tr
				>{/snippet}</RelationTable
		>
	</Section>{/if}
{#if discipline.spellsongs.length}<Section
		id="spellsongs"
		title="Spellsongs"
		count={discipline.spellsongs.length}
	>
		<RelationTable
			items={discipline.spellsongs}
			label="Spellsongs"
			headings={['Spellsong', 'Tune', 'Charges', 'Note Sheet']}
			numeric={[false, true, false]}
			collapse={false}
			>{#snippet row(song)}<tr id={song.anchor}
					><td
						><span class="relation-name" data-game>{song.name}</span>{#if song.description}<small
								data-game>{song.description}</small
							>{/if}</td
					><td><Tune tune={song.tune} /></td><td class="numeric">{formatNumber(song.charges)}</td
					><td
						>{#each song.sources as source, index (source.relationId)}{index ? ', ' : ''}<span
								data-relation-id={source.relationId}
								data-relation-direction="incoming"
								>{source.places[0]?.name ?? 'Location unknown'}</span
							>{:else}<span class="muted">No known source</span>{/each}</td
					></tr
				>{/snippet}</RelationTable
		>
	</Section>{/if}
{#if passives.length}<Section id="passives" title="Passives" count={passives.length}>
		<RelationTable
			items={passives}
			label="Passives"
			headings={['Passive', 'Points per Rank']}
			collapse={false}
			>{#snippet row(
				{ row, passive },
				index
			)}{#if index === 0 || passives[index - 1].row !== row}<tr class="place-heading"
						><th colspan="2" scope="colgroup">Proficiency Level {formatNumber(row)}</th></tr
					>{/if}<tr id={passive.anchor}
					><td
						><span class="relation-name passive-name"
							>{#if passive.icon}<img
									src="/icons/{passive.icon}-48.webp"
									alt=""
									width="24"
									height="24"
									loading="lazy"
								/>{/if}<span data-game>{passive.name}</span></span
						>{#if passive.description}<small data-game>{passive.description}</small
							>{/if}{#if passive.requiredLevel !== row}<small
								>Unlocks at {passive.checkedLevel} level {formatNumber(
									passive.requiredLevel
								)}</small
							>{/if}</td
					><td class="numeric">{points(rankCosts(passive))}</td></tr
				>{/snippet}</RelationTable
		>
	</Section>{/if}

<style>
	.passive-name {
		display: inline-flex;
		align-items: center;
		gap: var(--gap-inline);
	}
	.passive-name img {
		width: var(--size-icon-inline);
		height: var(--size-icon-inline);
	}
</style>
