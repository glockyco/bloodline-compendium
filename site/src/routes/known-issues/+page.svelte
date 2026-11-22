<script lang="ts">
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import LinkedText from '#lib/ui/LinkedText.svelte';
	import ExpandableList from '#lib/ExpandableList.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import { formatNumber } from '#lib/format.js';
	import { kindCount } from '#lib/entities.js';
	let { data } = $props();
	const builds = (ids: number[]) =>
		ids
			.map((id) =>
				id === data.buildId && data.release ? `${data.release.title} (build ${id})` : `build ${id}`
			)
			.join(', ');
</script>

<SeoHead
	title="Known Issues | The Bloodline Compendium"
	description="Bugs in The Bloodline, each confirmed in the game, with what it affects and steps to reproduce it."
/>
<div class="sections" data-pagefind-body>
	<header class="page-heading">
		<h1 data-pagefind-meta="title">Known Issues</h1>
		<p class="intro">
			Bugs in The Bloodline, each confirmed in the game, with what it affects and steps to reproduce
			it.
		</p>
	</header>
	{#if data.issues.length}<nav class="issue-index surface" aria-label="Issues">
			<ol>
				{#each data.issues as { issue, affected } (issue.id)}<li>
						<a href={'#' + issue.id}>{issue.title}</a>{#if affected.length}<span class="muted"
								>{formatNumber(affected.length)}
								{kindCount(affected, data.kinds).noun}</span
							>{/if}
					</li>{/each}
			</ol>
		</nav>{/if}
	{#each data.issues as { issue, affected } (issue.id)}
		<!-- Entity pages link the whole issue, so the anchor holds its heading and its surface. -->
		<div class="issue-anchor" id={issue.id}>
			<Section id={issue.id + '-title'} title={issue.title}>
				<div class="surface issue">
					<p class="effect"><LinkedText text={issue.effect} /></p>
					{#if issue.workaround}<div class="field">
							<h3 class="field-label">Workaround</h3>
							<p><LinkedText text={issue.workaround} /></p>
						</div>{/if}
					{#if affected.length}<div class="field">
							<h3 class="field-label">
								Affected <span class="muted">{formatNumber(affected.length)}</span>
							</h3>
							<ExpandableList
								items={affected}
								initial={9}
								noun={kindCount(affected, data.kinds).many}
								label={issue.title}
								className="affected-list"
							>
								{#snippet row(entity)}<EntityLink {entity} icon />{/snippet}
							</ExpandableList>
						</div>{/if}
					<details class="disclosure">
						<summary>Steps to Reproduce</summary>
						<ol class="steps">
							{#each issue.steps as step, index (index)}<li><LinkedText text={step} /></li>{/each}
						</ol>
					</details>
					<details class="disclosure" data-developer>
						<summary>For Developers</summary>
						<dl>
							<div class="field">
								<dt class="field-label">Cause</dt>
								<dd><LinkedText text={issue.cause} /></dd>
							</div>
							<div class="field">
								<dt class="field-label">Functions</dt>
								<dd>
									<ul class="citations">
										{#each issue.citations as citation (citation)}<li>
												<code
													>{#each citation.split(/(?<=[/:.])/) as part, index (index)}{part}<wbr
														/>{/each}</code
												>
											</li>{/each}
									</ul>
								</dd>
							</div>
							<div class="field">
								<dt class="field-label">Confirmed on</dt>
								<dd>{builds(issue.builds)}</dd>
							</div>
							{#if issue.reportUrl}<div class="field">
									<dt class="field-label">Report</dt>
									<dd><a href={issue.reportUrl}>Bug report</a></dd>
								</div>{/if}
						</dl>
					</details>
				</div>
			</Section>
		</div>
	{:else}
		<p>No known issues are recorded for this game release.</p>
	{/each}
</div>

<style>
	/* Issues read as prose, so the page keeps a reading width. Each issue is one surface whose parts are labelled
	   fields, spaced apart more than a label is from its content. */
	.issue-index ol {
		display: grid;
		gap: var(--gap-inline);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	/* Each entry keeps its count in a column of its own, so a wrapped title never strands it. */
	.issue-index li {
		display: grid;
		grid-template-columns: minmax(0, 1fr) max-content;
		align-items: baseline;
		gap: var(--gap-block);
	}
	.issue-index span {
		font-size: var(--type-0);
	}
	.issue {
		display: grid;
		gap: var(--gap-groups);
	}
	.field {
		display: grid;
		align-content: start;
		gap: var(--gap-line);
	}
	.field :is(p, dd, ol, ul) {
		margin: 0;
	}
	.effect {
		margin: 0;
	}
	.steps {
		padding-left: var(--inset-loose);
	}
	/* Items sit in columns without bullets, each icon beside its name. */
	.issue :global(.affected-list) {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(var(--size-card-narrow), 1fr));
		gap: var(--gap-line) var(--gap-block);
		padding: 0;
		list-style: none;
	}
	.issue :global(.more-toggle) {
		display: block;
		padding-inline: 0;
	}
	dd {
		color: var(--text-soft);
	}
	.steps {
		margin: var(--gap-block) 0 0;
	}
	.disclosure dl {
		display: grid;
		gap: var(--gap-block);
		margin: var(--gap-block) 0 0;
	}
	/* A function path breaks only after a slash, colon or dot, where each part of it ends. */
	.citations {
		padding-left: var(--inset);
	}
</style>
