<script lang="ts">
	import { alignSide } from '#lib/ui/align-side.js';
	import Section from '../ui/Section.svelte';
	import SourceSections from '../entity/SourceSections.svelte';
	import UnconfirmedSources from '../entity/UnconfirmedSources.svelte';
	import BugsSection from '../entity/BugsSection.svelte';
	import SkillTooltip from './SkillTooltip.svelte';
	import { skillStatus } from './describe.js';
	import { skillRowAnchor } from './anchors.js';
	import EntityLink from '../ui/EntityLink.svelte';
	import { formatNumber } from '../format.js';
	import type { SkillPage } from '../site-data.js';
	/** One version of a skill page: how to learn it beside its game card, as an item version reads. */
	let { skill, idPrefix = '' }: { skill: SkillPage; idPrefix?: string } = $props();
	const status = $derived(skillStatus(skill));
	// A tree source links the skill's own row on its discipline page, not the top of the page.
	const sources = $derived(
		skill.sources.map((source) =>
			source.kind === 'skillTree' && source.entity
				? { ...source, entity: { ...source.entity, fragment: skillRowAnchor(skill.slug) } }
				: source
		)
	);
	// The damage passives' bonuses add up, so the most they give together is the sum of every rank.
	const scaling = $derived(skill.damageScaling);
	const mostBonus = $derived(
		scaling ? scaling.passives.reduce((sum, entry) => sum + entry.perRank * entry.ranks, 0) : 0
	);
</script>

<div class="detail-layout" use:alignSide>
	<!-- Only a skill without a confirmed way to learn it opens with an answer, which its sources give otherwise. -->
	{#if status}<div class="answer" data-main data-pagefind-ignore="all">
			<Section id={idPrefix + 'how-to-get-it'} title="How to Get It">
				<p class="surface">{status}</p>
			</Section>
		</div>{/if}
	<aside class="item-side" data-side aria-label="Game card">
		<SkillTooltip {skill} heading="p" />
	</aside>
	<div class="item-relations" data-main data-pagefind-ignore="all">
		<SourceSections {sources} {idPrefix} />
		{#if scaling}<Section id={idPrefix + 'damage'} title="Damage">
				<p class="surface">
					{scaling.discipline.name}'s damage passives raise {skill.name}'s damage:
					{#each scaling.passives as entry, index (entry.passive.key)}{index
							? index === scaling.passives.length - 1
								? ' and '
								: ', '
							: ''}<EntityLink entity={entry.passive} />{/each}. Together they add up to {formatNumber(
						Math.round(mostBonus * 1000) / 10
					)}% more damage at full ranks.
				</p>
			</Section>{/if}
		<UnconfirmedSources sources={skill.sources} {idPrefix} />
		<BugsSection notes={skill.notes} facts={skill.facts} name={skill.name} {idPrefix} />
	</div>
</div>
