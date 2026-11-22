<script lang="ts">
	import Section from '../ui/Section.svelte';
	import SourceSections from '../entity/SourceSections.svelte';
	import UnconfirmedSources from '../entity/UnconfirmedSources.svelte';
	import BugsSection from '../entity/BugsSection.svelte';
	import SkillTooltip from './SkillTooltip.svelte';
	import { skillStatus } from './describe.js';
	import { skillRowAnchor } from './anchors.js';
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
</script>

<div class="detail-layout">
	<!-- Only a skill without a confirmed way to learn it opens with an answer, which its sources give otherwise. -->
	{#if status}<div class="answer" data-pagefind-ignore="all">
			<Section id={idPrefix + 'how-to-get-it'} title="How to Get It">
				<p class="surface">{status}</p>
			</Section>
		</div>{/if}
	<aside class="item-side" aria-label="Game card">
		<SkillTooltip {skill} heading="p" />
	</aside>
	<div class="item-relations" data-pagefind-ignore="all">
		<SourceSections {sources} {idPrefix} />
		<UnconfirmedSources sources={skill.sources} name={skill.name} {idPrefix} />
		<BugsSection notes={skill.notes} facts={skill.facts} name={skill.name} {idPrefix} />
	</div>
</div>
