<script lang="ts">
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import ExpandableList from '#lib/ExpandableList.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import { formatNumber } from '#lib/format.js';
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
	description="This page lists bugs in The Bloodline that affect items. Each bug was confirmed in the game and comes with steps to reproduce it."
/>
<div class="sections" data-pagefind-body>
	<header class="page-heading">
		<p class="eyebrow">Problems in the game itself</p>
		<h1 data-pagefind-meta="title">Known Issues</h1>
		<p class="intro">
			This page lists bugs in The Bloodline that affect items. Each bug was confirmed in the game
			and comes with steps to reproduce it.
		</p>
	</header>
	{#if data.issues.length}<nav class="issue-index surface" aria-label="Issues">
			<ol>
				{#each data.issues as { issue, affected } (issue.id)}<li>
						<a href={'#' + issue.id}>{issue.title}</a>{#if affected.length}<span class="muted"
								>{formatNumber(affected.length)}
								{affected.length === 1 ? 'item' : 'items'}</span
							>{/if}
					</li>{/each}
			</ol>
		</nav>{/if}
	{#each data.issues as { issue, affected } (issue.id)}
		<!-- Item pages link the whole issue, so the anchor holds its heading and its surface. -->
		<div class="issue-anchor" id={issue.id}>
			<Section id={issue.id + '-title'} title={issue.title}>
				<div class="surface issue">
					<p class="effect">{issue.effect}</p>
					{#if issue.workaround}<div class="field">
							<h3 class="field-label">Workaround</h3>
							<p>{issue.workaround}</p>
						</div>{/if}
					{#if affected.length}<div class="field">
							<h3 class="field-label">
								Affected items <span class="muted">{formatNumber(affected.length)}</span>
							</h3>
							<ExpandableList items={affected} initial={9} noun="items" className="affected-list">
								{#snippet row(entity)}<EntityLink {entity} icon />{/snippet}
							</ExpandableList>
						</div>{/if}
					<div class="field">
						<h3 class="field-label">Steps to see it</h3>
						<ol class="steps">
							{#each issue.steps as step, index (index)}<li>{step}</li>{/each}
						</ol>
					</div>
					<dl class="outcome">
						<div class="field">
							<dt class="field-label">Expected</dt>
							<dd>{issue.expected}</dd>
						</div>
						<div class="field">
							<dt class="field-label">Observed in the game</dt>
							<dd>{issue.observed}</dd>
						</div>
					</dl>
					<details class="developer" data-developer>
						<summary>For developers</summary>
						<dl>
							<div class="field">
								<dt class="field-label">Cause, derived from the game's code</dt>
								<dd>{issue.cause}</dd>
							</div>
							<div class="field">
								<dt class="field-label">Functions</dt>
								<dd>
									<ul class="citations">
										{#each issue.citations as citation (citation)}<li>
												<code>{citation}</code>
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
	.outcome {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(var(--size-card-narrow), 1fr));
		gap: var(--gap-groups);
		margin: 0;
	}
	dd {
		color: var(--text-soft);
	}
	.developer {
		padding-top: var(--gap-block);
		border-top: var(--border-width) solid var(--border);
	}
	.developer summary {
		cursor: pointer;
		color: var(--text-soft);
		font-weight: var(--weight-strong);
	}
	.developer dl {
		display: grid;
		gap: var(--gap-block);
		margin: var(--gap-block) 0 0;
	}
	.citations {
		padding-left: var(--inset);
		overflow-wrap: anywhere;
	}
</style>
