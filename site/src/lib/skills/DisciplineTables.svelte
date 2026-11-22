<script lang="ts">
	import type {
		DisciplinePage,
		EntityReference,
		Fact,
		RelationCondition,
		Stat
	} from '../site-data.js';
	import RelationTable from '../RelationTable.svelte';
	import Section from '../ui/Section.svelte';
	import EntityLink from '../ui/EntityLink.svelte';
	import Conditions from '../Conditions.svelte';
	import CardGrid from '../ui/CardGrid.svelte';
	import Card from '../ui/Card.svelte';
	import { formatNumber, plural } from '../format.js';
	import { rankCosts } from '../rules/progression.js';
	import Tune from './Tune.svelte';
	/**
	 * A discipline's skills and passives in tables grouped by the level row the game's window shows them in, and its
	 * spellsongs as cards, as the game's grid shows them. Skills show the skill list's stat columns that any has.
	 */
	let {
		discipline,
		skillFacts
	}: {
		discipline: DisciplinePage;
		skillFacts: Record<string, Fact[]>;
	} = $props();
	// The relations each entry shows, by the key of the entity at their other end, so the static page carries them.
	const relationIds = $derived(
		Map.groupBy(discipline.relations, (relation) => relation.entity.key ?? '')
	);
	const ids = (key: string | null) =>
		(relationIds.get(key ?? '') ?? []).map((relation) => relation.id).join(' ') || undefined;
	const slots = $derived(
		discipline.skillRows.flatMap((row) => row.slots.map((slot) => ({ row: row.level, slot })))
	);
	// Passives group by the level that unlocks each, which the game checks, rather than the row its window shows
	// them in (run rowgate1).
	const passives = $derived(
		discipline.passiveRows
			.flatMap((row) => row.passives)
			.map((passive) => ({ row: passive.requiredLevel, passive }))
			.sort((a, b) => a.row - b.row)
	);
	const factOf = (key: string | null, stat: Stat) =>
		(skillFacts[key ?? ''] ?? []).find((entry) => entry.stat === stat);
	// The skill list's stat columns, in its order, that any skill of this discipline has.
	const statOrder: Stat[] = ['baseDamage', 'manaCost', 'staminaCost', 'cooldown'];
	const statColumns = $derived(
		statOrder.flatMap((stat) => {
			const found = slots
				.map(({ slot }) => factOf(slot.skill.key, stat))
				.find((entry) => entry !== undefined);
			return found ? [{ stat, label: found.label, unit: found.unit ?? null }] : [];
		})
	);
	// A cell reads as a list cell does: none for a stat the skill lacks, unknown for one the game does not tell.
	function cell(key: string | null, stat: Stat): string {
		const found = factOf(key, stat);
		if (!found) return 'None';
		return found.number == null ? 'Unknown' : formatNumber(found.number);
	}
	// An entry whose own level differs from its row's label says the level it needs, as a condition. One that checks
	// another discipline's level links to it.
	function ownLevel(required: number, checked: EntityReference, row: number): RelationCondition[] {
		const other = checked.key !== discipline.key;
		return required !== row || other
			? [
					{
						kind: 'disciplineLevel',
						entity: other ? checked : null,
						value: required,
						label: `Unlocks at ${checked.name} level ${formatNumber(required)}`
					}
				]
			: [];
	}
	const where = (song: DisciplinePage['spellsongs'][number]) => song.sources;
</script>

{#if slots.length}<Section id="skills" title="Skills" count={slots.length}>
		<RelationTable
			items={slots}
			label="Skills"
			headings={['Skill', ...statColumns]}
			collapse={false}
			>{#snippet row({ row, slot }, index)}{#if index === 0 || slots[index - 1].row !== row}<tr
						class="group-heading"
						><th colspan={statColumns.length + 1} scope="colgroup">Level {formatNumber(row)}</th
						></tr
					>{/if}<tr
					id={'skill-' + slot.skill.slug}
					data-relation-id={ids(slot.skill.key)}
					data-relation-direction="outgoing"
					><td
						><EntityLink entity={slot.skill} icon /><Conditions
							conditions={ownLevel(slot.requiredLevel, slot.levelDiscipline, row)}
						/></td
					>{#each statColumns as column (column.stat)}<td
							class="numeric"
							class:absent={factOf(slot.skill.key, column.stat)?.number == null}
							class:one={factOf(slot.skill.key, column.stat)?.number === 1}
							>{cell(slot.skill.key, column.stat)}</td
						>{/each}</tr
				>{/snippet}</RelationTable
		>
	</Section>{/if}
{#if discipline.spellsongs.length}<Section
		id="spellsongs"
		title="Spellsongs"
		count={discipline.spellsongs.length}
	>
		<CardGrid rows={4} label="Spellsongs">
			{#each discipline.spellsongs as song (song.key)}<Card>
					<h3 id={song.anchor} data-game>{song.name}</h3>
					<Tune tune={song.tune} />
					<span data-game>{song.description ?? ''}</span>
					<span class="muted"
						>{plural(song.charges, 'charge', 'charges')} · {#each where(song) as source, index (source.relationId)}{index
								? ', '
								: ''}<span data-relation-id={source.relationId} data-relation-direction="incoming"
								>Note sheet in {#if source.places[0]}<EntityLink
										entity={source.places[0]}
									/>{:else}an unknown place{/if}</span
							>{:else}No known source{/each}</span
					>
				</Card>{/each}
		</CardGrid>
	</Section>{/if}
{#if passives.length}<Section
		id="passives"
		title="Passives"
		count={passives.length}
		line="You buy passives with this discipline's [proficiency points](mechanic:proficiency-points)."
	>
		<RelationTable
			items={passives}
			label="Passives"
			stackPairs
			headings={['Passive', 'Points per Rank']}
			collapse={false}
			>{#snippet row(
				{ row, passive },
				index
			)}{#if index === 0 || passives[index - 1].row !== row}<tr class="group-heading"
						><th colspan="2" scope="colgroup">Level {formatNumber(row)}</th></tr
					>{/if}<tr id={passive.anchor}
					><td
						><span class="relation-name passive-name with-icon"
							>{#if passive.icon}<img
									src="/icons/{passive.icon}-48.webp"
									alt=""
									width="24"
									height="24"
									loading="lazy"
								/>{/if}<span data-game>{passive.name}</span></span
						>{#if passive.description}<small data-game>{passive.description}</small>{/if}<Conditions
							conditions={ownLevel(passive.requiredLevel, passive.levelDiscipline, row)}
						/></td
					><td class="numeric">{rankCosts(passive).map(formatNumber).join(', ')}</td></tr
				>{/snippet}</RelationTable
		>
	</Section>{/if}

<style>
	.passive-name {
		align-items: center;
		gap: var(--gap-inline);
	}
	.passive-name img {
		width: var(--size-icon-inline);
		height: var(--size-icon-inline);
	}
</style>
