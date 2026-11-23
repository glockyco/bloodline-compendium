<script lang="ts">
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import Ingredients from '#lib/items/Ingredients.svelte';
	import { entityPath } from '#lib/entities.js';
	import { formatNumber } from '#lib/format.js';
	import type { professionPage } from '#lib/server/professions.js';
	import type { EntityKindDefinition } from '#lib/site-data.js';
	/** A profession's page, or one band of its levels, which its first page and its band pages share. */
	let {
		data
	}: {
		data: Awaited<ReturnType<typeof professionPage>> & { kinds: EntityKindDefinition[] };
	} = $props();
	const profession = $derived(data.profession);
	const reference = $derived({
		kind: profession.kind,
		key: profession.key,
		name: profession.name,
		slug: profession.slug,
		icon: null
	});
	const path = $derived(entityPath(reference, data.kinds)!);
	const rows = $derived(data.rows);
	const total = $derived(data.total);
	const icon = $derived(profession.icon);
	const leveled = $derived(profession.minLevel != null);
	const title = $derived(profession.farming ? 'Crops' : 'Recipes');
	const levelName = (level: number | null) => (level == null ? 'None' : formatNumber(level));
	const range = (min: number, max: number) =>
		min === max ? formatNumber(min) : `${formatNumber(min)} to ${formatNumber(max)}`;
	const bandPath = (segment: string | null) => (segment ? `${path}${segment}/` : path);
	const group = (row: (typeof rows)[number]) => (leveled ? 'Level ' + levelName(row.level) : null);
	const counts = $derived(
		rows.reduce((map, row) => map.set(group(row), (map.get(group(row)) ?? 0) + 1), new Map())
	);
	// A column shows only when some row has something in it, as fish need nothing and crops give no experience.
	const hasInputs = $derived(rows.some((row) => row.inputs.length));
	const hasExperience = $derived(rows.some((row) => row.experience != null));
	const headings = $derived([
		profession.farming ? 'Crop' : 'Item',
		...(hasInputs ? [profession.farming ? 'Planted' : 'Ingredients'] : []),
		...(hasExperience ? ['Experience'] : [])
	]);
	const bandTitle = $derived(
		data.band && data.bands.length ? `, Levels ${range(data.band.min, data.band.max)}` : ''
	);
	// Each band of a long profession is its own page, so its description names its levels.
	const description = $derived(
		data.band && data.bands.length
			? `${profession.name} ${title.toLowerCase()} for levels ${range(data.band.min, data.band.max)} in The Bloodline, with ingredients and experience.`
			: `Every ${profession.name} ${title.toLowerCase().replace(/s$/, '')} in The Bloodline` +
					(leveled ? ', with its level, ingredients and experience.' : ', with its ingredients.')
	);
</script>

<EntityPage
	entity={reference}
	kinds={data.kinds}
	title={profession.name + bandTitle}
	{description}
	{icon}
>
	<header class="item-heading">
		<div>
			<h1>{profession.name}</h1>
			<dl class="facts">
				{#if profession.stations.length}<div>
						<dt>Made at</dt>
						<dd>
							{#each profession.stations as station, index (station.key)}{index
									? ', '
									: ''}<EntityLink entity={station} />{/each}
						</dd>
					</div>{/if}
				{#if leveled}<div>
						<dt>Levels</dt>
						<dd>{range(profession.minLevel!, profession.maxLevel!)}</dd>
					</div>{/if}
			</dl>
		</div>
	</header>
	<div class="item-relations" data-pagefind-ignore="all">
		<Section id="recipes" {title} count={total}
			>{#snippet action()}
				<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- the list's address is built from the kind registry. -->
				<a href={data.all}>Sort and filter in the item list</a>
			{/snippet}
			{#if data.bands.length}<nav class="bands" aria-label="{profession.name} levels">
					<!-- eslint-disable svelte/no-navigation-without-resolve -- band pages sit under the profession's registry path. -->
					<ul>
						{#each data.bands as band (band.min)}<li>
								<a
									href={bandPath(band.segment)}
									aria-current={band.min === data.band?.min ? 'page' : undefined}
									>Levels {range(band.min, band.max)}</a
								>
							</li>{/each}
					</ul>
					<!-- eslint-enable svelte/no-navigation-without-resolve -->
				</nav>{/if}
			<RelationTable
				items={rows}
				label={title}
				{headings}
				numeric={headings.slice(1).map((heading) => heading === 'Experience')}
				collapse={false}
				>{#snippet row(entry, index)}{@const name =
						group(entry)}{#if name != null && (index === 0 || group(rows[index - 1]) !== name)}<tr
							class="group-heading"
							id={'level-' + (entry.level ?? 'none')}
							><th colspan={headings.length} scope="colgroup"
								>{name}
								<span class="group-count">{formatNumber(counts.get(name))}</span></th
							></tr
						>{/if}<tr
						><td
							><EntityLink
								entity={{ ...entry.product, kind: 'item' }}
								icon
							/>{#if entry.output && entry.output > 1}<small
									>Makes {formatNumber(entry.output)}</small
								>{/if}</td
						>{#if hasInputs}<td class:absent={!entry.inputs.length}
								>{#if entry.inputs.length}<Ingredients
										inputs={entry.inputs}
										inline
									/>{:else}None{/if}</td
							>{/if}{#if hasExperience}<td class="numeric" class:absent={entry.experience == null}
								>{entry.experience == null ? 'Unknown' : formatNumber(entry.experience)}</td
							>{/if}</tr
					>{/snippet}</RelationTable
			>
		</Section>
	</div>
</EntityPage>

<style>
	h1 {
		margin: 0;
	}
	.facts {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-line) var(--gap-groups);
		margin: var(--gap-line) 0 0;
	}
	.facts div {
		display: flex;
		gap: var(--gap-inline);
	}
	.facts dt {
		color: var(--text-muted);
	}
	.facts dd {
		margin: 0;
	}
	/* The bands read as tabs above the table, the current one marked as the header marks its page. */
	.bands ul {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.bands a {
		display: inline-flex;
		align-items: center;
		min-height: var(--size-control-compact);
		padding: 0 var(--inset-cell);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
		text-decoration: none;
	}
	.bands a:hover {
		border-color: var(--border-control);
	}
	.bands a[aria-current='page'] {
		border-color: var(--accent);
		color: var(--text);
		font-weight: var(--weight-strong);
	}
	.group-count {
		margin-left: var(--gap-inline);
		color: var(--text-muted);
		font-weight: var(--weight-regular);
	}
</style>
