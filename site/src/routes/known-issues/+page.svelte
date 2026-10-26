<script lang="ts">
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Count from '#lib/ui/Count.svelte';
	import ExpandableList from '#lib/ExpandableList.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	let { data } = $props();
	const builds = (ids: number[]) =>
		ids
			.map((id) =>
				id === data.buildId && data.release ? `${data.release.name} (build ${id})` : `build ${id}`
			)
			.join(', ');
</script>

<SeoHead
	title="Known Issues | The Bloodline Compendium"
	description="Defects in The Bloodline that this reference reproduced in the game, with their effect on players, workarounds and steps to see them yourself."
/>
<div data-pagefind-body>
	<p class="eyebrow">Problems in the game itself</p>
	<h1 data-pagefind-meta="title">Known Issues</h1>
	<p class="intro">
		These are defects in The Bloodline that we reproduced in the running game. Item pages show what
		the game actually applies and link the issues that affect them.
	</p>
	<div class="issue-stack">
		{#each data.issues as { issue, affected } (issue.id)}
			<section class="panel issue" id={issue.id} aria-labelledby={issue.id + '-title'}>
				<h2 id={issue.id + '-title'}>{issue.title}</h2>
				<p>{issue.effect}</p>
				{#if issue.workaround}<p>Workaround: {issue.workaround}</p>{/if}
				{#if affected.length}
					<h3>Affected items <Count value={affected.length} /></h3>
					<ExpandableList items={affected} initial={8} noun="items" className="affected-list">
						{#snippet row(entity)}<EntityLink {entity} />{/snippet}
					</ExpandableList>
				{/if}
				<h3>Steps without mods</h3>
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
							{#if issue.reportUrl}<a href={issue.reportUrl}>Reported to the developer</a>{:else}Not
								reported to the developer yet.{/if}
						</dd>
					</div>
				</dl>
			</section>
		{:else}
			<p>No known issues are recorded for this game release.</p>
		{/each}
	</div>
</div>

<style>
	.issue-stack {
		display: grid;
		gap: var(--space-6);
		margin-top: var(--space-6);
	}
	.issue {
		display: grid;
		gap: var(--space-3);
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
		gap: var(--space-3);
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
		padding-left: var(--space-4);
		overflow-wrap: anywhere;
	}
</style>
