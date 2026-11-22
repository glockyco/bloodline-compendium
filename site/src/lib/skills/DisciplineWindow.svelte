<script lang="ts">
	import type { DisciplinePage } from '../site-data.js';
	import EntityLink from '../ui/EntityLink.svelte';
	import { formatNumber } from '../format.js';
	import { rankCosts } from '../rules/progression.js';
	import Tune from './Tune.svelte';
	/**
	 * A discipline laid out as the game's tree window: its skills in rows by the discipline level each row's label
	 * names, beside its passives in rows by proficiency level. Lute shows its spellsongs in place of skill rows.
	 */
	let { discipline }: { discipline: DisciplinePage } = $props();
	const level = $derived(discipline.levelName ?? discipline.name);
	// The relations each entry shows, by the key of the entity at their other end, so the static page carries them.
	const relationIds = $derived(
		Map.groupBy(discipline.relations, (relation) => relation.entity.key ?? '')
	);
	const ids = (key: string | null) =>
		(relationIds.get(key ?? '') ?? []).map((relation) => relation.id).join(' ') || undefined;
</script>

<div class="window">
	{#if discipline.skillRows.length}
		<section class="panel" aria-labelledby="skills-heading">
			<h2 id="skills-heading">Skills</h2>
			<ol class="rows">
				{#each discipline.skillRows as row (row.level)}
					<li class="row">
						<h3>{level} Level {formatNumber(row.level)}</h3>
						<ul class="entries">
							{#each row.slots as slot (slot.skill.key)}
								<li
									class="skill"
									data-relation-id={ids(slot.skill.key)}
									data-relation-direction="outgoing"
								>
									<EntityLink class="entity-name" entity={slot.skill} icon />
									{#if slot.requiredLevel !== row.level || slot.checkedLevel !== level}
										<small>Unlocks at {slot.checkedLevel} level {formatNumber(slot.requiredLevel)}</small>
									{/if}
								</li>
							{/each}
						</ul>
					</li>
				{/each}
			</ol>
		</section>
	{/if}
	{#if discipline.spellsongs.length}
		<section class="panel" aria-labelledby="spellsongs-heading">
			<h2 id="spellsongs-heading">Spellsongs</h2>
			<ul class="songs">
				{#each discipline.spellsongs as song (song.key)}
					<li class="song" id={song.anchor}>
						<h3 data-game>{song.name}</h3>
						<Tune tune={song.tune} />
						{#if song.description}<p data-game>{song.description}</p>{/if}
						<p class="meta">
							{formatNumber(song.charges)}
							{song.charges === 1 ? 'charge' : 'charges'}, {formatNumber(song.experience)} Lute experience
						</p>
						<p class="meta">
							{#if song.sources.length}Note sheet in {#each song.sources as source, index (source.relationId)}{index
										? ', '
										: ''}<span data-relation-id={source.relationId} data-relation-direction="incoming"
										>{source.places[0]?.name ?? 'an unknown place'}</span
									>{/each}{:else}No known source{/if}
						</p>
					</li>
				{/each}
			</ul>
		</section>
	{/if}
	{#if discipline.passiveRows.length}
		<section class="panel" aria-labelledby="passives-heading">
			<h2 id="passives-heading">Proficiency</h2>
			<ol class="rows">
				{#each discipline.passiveRows as row (row.level)}
					<li class="row">
						<h3>Proficiency Level {formatNumber(row.level)}</h3>
						<ul class="entries passives">
							{#each row.passives as passive (passive.key)}
								{@const costs = rankCosts(passive)}
								<li class="passive" id={passive.anchor}>
									<p class="passive-name">
										{#if passive.icon}<img
												src="/icons/{passive.icon}-48.webp"
												alt=""
												width="24"
												height="24"
												loading="lazy"
											/>{/if}<span data-game>{passive.name}</span>
									</p>
									{#if passive.description}<p data-game>{passive.description}</p>{/if}
									<p class="meta">
										{costs.length === 1
											? `${formatNumber(costs[0])} ${costs[0] === 1 ? 'point' : 'points'}`
											: `${formatNumber(costs.length)} ranks: ${costs.map(formatNumber).join(', ')} points`}{#if passive.requiredLevel !== row.level}, unlocks at {passive.checkedLevel}
											level {formatNumber(passive.requiredLevel)}{/if}
									</p>
								</li>
							{/each}
						</ul>
					</li>
				{/each}
			</ol>
		</section>
	{/if}
</div>

<style>
	/* The game's window: skills on the left and proficiency on the right on wide screens, one after the other on
	   narrow ones. */
	.window {
		display: grid;
		gap: var(--gap-section);
		align-items: start;
	}
	@media (width >= 64rem) {
		.window {
			grid-template-columns: repeat(auto-fit, minmax(0, 1fr));
		}
	}
	.panel {
		display: grid;
		gap: var(--gap-heading);
		min-width: 0;
	}
	h2 {
		margin: 0;
		font-size: var(--type-section);
		line-height: var(--leading-heading);
	}
	.rows,
	.entries,
	.songs {
		display: grid;
		gap: var(--gap-block);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.row,
	.song {
		display: grid;
		gap: var(--gap-inline);
		padding: var(--inset);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
	}
	h3 {
		margin: 0;
		color: var(--text-muted);
		font-size: var(--type-0);
		font-weight: var(--weight-strong);
		line-height: var(--leading-meta);
	}
	.entries:not(.passives) {
		grid-template-columns: repeat(auto-fill, minmax(var(--size-card-narrow), 1fr));
	}
	.skill {
		display: grid;
		gap: var(--gap-line);
	}
	.passive {
		display: grid;
		gap: var(--gap-line);
	}
	.passive-name {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
		margin: 0;
		font-weight: var(--weight-strong);
	}
	p {
		margin: 0;
	}
	small,
	.meta {
		color: var(--text-muted);
		font-size: var(--type-0);
	}
	.song h3 {
		color: var(--text);
		font-size: var(--type-1);
	}
</style>
