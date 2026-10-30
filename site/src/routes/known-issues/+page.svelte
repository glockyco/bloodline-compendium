<script lang="ts">
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Count from '#lib/ui/Count.svelte';
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
	description="Defects in The Bloodline, each reproduced in the game, with their effect on players, workarounds and steps to see them yourself."
/>
<div class="sections" data-pagefind-body>
	<header class="page-heading">
		<p class="eyebrow">Problems in the game itself</p>
		<h1 data-pagefind-meta="title">Known Issues</h1>
		<p class="intro">
			Defects in The Bloodline, each reproduced in the game. Item pages show what the game applies
			and link the issues that affect them.
		</p>
	</header>
	{#if data.issues.length}<nav class="issue-index panel" aria-label="Issues">
			<ol>
				{#each data.issues as { issue, affected } (issue.id)}<li>
						<a href={'#' + issue.id}>{issue.title}</a>{#if affected.length}<small
								>{formatNumber(affected.length)}
								{affected.length === 1 ? 'item' : 'items'}</small
							>{/if}
					</li>{/each}
			</ol>
		</nav>{/if}
	<div class="issue-stack">
		{#each data.issues as { issue, affected } (issue.id)}
			<section class="panel issue" id={issue.id} aria-labelledby={issue.id + '-title'}>
				<h2 id={issue.id + '-title'}>{issue.title}</h2>
				<p>{issue.effect}</p>
				{#if issue.workaround}<p>Workaround: {issue.workaround}</p>{/if}
				{#if affected.length}
					<h3>Affected items <Count value={affected.length} /></h3>
					<ExpandableList items={affected} initial={8} noun="items" className="affected-list">
						{#snippet row(entity)}<EntityLink {entity} icon />{/snippet}
					</ExpandableList>
				{/if}
				<h3>Steps to See It</h3>
				<ol>
					{#each issue.steps as step, index (index)}<li>{step}</li>{/each}
				</ol>
				<dl>
					<div>
						<dt>Expected</dt>
						<dd>{issue.expected}</dd>
					</div>
					<div>
						<dt>Observed in the game</dt>
						<dd>{issue.observed}</dd>
					</div>
				</dl>
				<details class="developer" data-developer>
					<summary>For Developers</summary>
					<dl>
						<div>
							<dt>Cause, derived from the game's code</dt>
							<dd>{issue.cause}</dd>
						</div>
						<div>
							<dt>Functions</dt>
							<dd>
								<ul class="citations">
									{#each issue.citations as citation (citation)}<li>
											<code>{citation}</code>
										</li>{/each}
								</ul>
							</dd>
						</div>
						<div>
							<dt>Confirmed on</dt>
							<dd>{builds(issue.builds)}</dd>
						</div>
						<div>
							<dt>Report</dt>
							<dd>
								{#if issue.reportUrl}<a href={issue.reportUrl}>Reported to the developer</a
									>{:else}Not reported to the developer yet.{/if}
							</dd>
						</div>
					</dl>
				</details>
			</section>
		{:else}
			<p>No known issues are recorded for this game release.</p>
		{/each}
	</div>
</div>

<style>
	.issue-index ol {
		display: grid;
		gap: var(--gap-inline);
		margin: 0;
		padding-left: var(--inset-loose);
	}
	.issue-index small {
		margin-left: var(--gap-inline);
		color: var(--text-soft);
	}
	.issue-index {
		margin-top: var(--gap-groups);
	}
	.developer {
		display: grid;
		gap: var(--gap-block);
	}
	.developer summary {
		cursor: pointer;
		font-weight: var(--weight-strong);
	}
	.developer dl {
		margin-top: var(--gap-block);
	}
	.issue-stack {
		display: grid;
		gap: var(--gap-section);
		margin-top: var(--gap-section);
	}
	.issue {
		display: grid;
		gap: var(--gap-block);
	}
	h2,
	h3,
	p,
	ol,
	dl {
		margin: 0;
	}
	dl {
		display: grid;
		gap: var(--gap-block);
	}
	dt {
		font-weight: var(--weight-strong);
	}
	dd {
		margin: 0;
		color: var(--text-soft);
	}
	.citations {
		margin: 0;
		padding-left: var(--inset);
		overflow-wrap: anywhere;
	}
</style>
