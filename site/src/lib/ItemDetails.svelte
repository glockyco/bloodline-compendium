<script lang="ts">
	import type { DetailRow } from './game-card.js';
	import type { ItemPage } from './site-data.js';
	import { formatNumber } from './format.js';
	let {
		rows,
		wide = false,
		projectileGroups
	}: {
		rows: DetailRow[];
		wide?: boolean;
		projectileGroups?: ItemPage['projectileGroups'];
	} = $props();
</script>

<dl class="details" class:wide>
	{#each rows as row (row.key)}<div class:prose={row.prose}>
			<dt>{row.label}</dt>
			{#each row.values as value, index (index)}<dd>{value}</dd>{/each}
		</div>{/each}
</dl>
{#if projectileGroups && projectileGroups.length > 1}
	<div class="projectile-hits">
		<table>
			<caption>Projectile Hits</caption>
			<thead
				><tr
					><th scope="col">Projectiles</th><th scope="col">Body Hit Damage</th><th scope="col"
						>Head Hit Damage</th
					></tr
				></thead
			>
			<tbody>
				{#each projectileGroups as group, index (index)}
					<tr>
						<td
							>{formatNumber(group.count)}{#if group.condition}<span class="condition"
									>{group.condition}</span
								>{/if}</td
						>
						<td
							>{group.bodyHitDamage == null
								? 'Unknown'
								: `${formatNumber(group.bodyHitDamage)} per projectile`}</td
						>
						<td
							>{group.headHitDamage == null
								? 'Unknown'
								: `${formatNumber(group.headHitDamage)} per projectile`}</td
						>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
{/if}

<style>
	.condition {
		display: block;
		color: var(--text-soft);
	}
	.projectile-hits {
		overflow-x: auto;
		margin-top: var(--gap-block);
	}
	table {
		width: 100%;
		border-collapse: collapse;
		font-variant-numeric: tabular-nums;
	}
	caption {
		text-align: left;
		margin-bottom: var(--gap-line);
	}
	th,
	td {
		padding: var(--gap-line);
		text-align: right;
		vertical-align: top;
	}
	th {
		color: var(--text-soft);
	}
	.details {
		display: grid;
		gap: var(--gap-line);
		margin: 0;
	}
	/* A label and its value share a row, and a long value wraps in its own track on the right instead of under
	   the label. Several values of one row, such as effects, stack as its lines. */
	.details div {
		display: grid;
		grid-template-columns: max-content minmax(0, 1fr);
		align-items: baseline;
		column-gap: var(--gap-block);
	}
	dt {
		color: var(--text-soft);
	}
	dd {
		grid-column: 2;
		margin: 0;
		text-align: right;
		font-variant-numeric: tabular-nums;
	}
	/* A worded value, such as an effect, reads left to right from its own edge instead of hugging the right. */
	.prose dd {
		text-align: left;
	}
	/* In the main column, the labels share one track and every value starts at one edge, as a two-column table
	   reads, so a number does not sit a page width away from its label. */
	.wide {
		grid-template-columns: max-content minmax(0, 1fr);
		column-gap: var(--gap-section);
	}
	.wide div {
		display: contents;
	}
	.wide dt {
		grid-column: 1;
	}
	.wide dd {
		text-align: left;
	}
	/* The values of one row, such as effects, stand closer together than the rows do. */
	.wide dd + dd {
		margin-top: calc(-1 * var(--gap-line));
	}
</style>
