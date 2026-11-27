<script lang="ts">
	import { page } from '$app/state';
	import { alignSide } from '#lib/ui/align-side.js';
	import type { MechanicTable } from '#lib/site-data.js';
	import Chance from '#lib/ui/Chance.svelte';
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import LinkedText from '#lib/ui/LinkedText.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import { plainText } from '#lib/links.js';
	let { data } = $props();
	const mechanic = $derived(data.mechanic);
	// A column of numbers or chances aligns right, as numbers do in every table of the site.
	const numericColumns = (table: MechanicTable) =>
		table.headings.map((_, column) => {
			const chances = table.chanceColumns?.[column];
			return chances
				? chances.every((cell) => cell.length === 1 && !cell[0].label)
				: table.rows.every((cells) => /^\d[\d,.]*(?: seconds)?$/.test(cells[column]));
		});
	const reference = $derived({
		kind: mechanic.kind,
		key: mechanic.key,
		name: mechanic.name,
		slug: mechanic.slug,
		icon: null
	});
</script>

<EntityPage
	entity={reference}
	kinds={data.kinds}
	title={mechanic.name}
	description={plainText(mechanic.intro)}
>
	<header class="page-heading">
		<h1>{mechanic.name}</h1>
		<p class="intro"><LinkedText text={mechanic.intro} /></p>
	</header>
	<div class="text-layout" use:alignSide>
		<nav class="text-side panel" data-side aria-label="{mechanic.name} and other mechanics">
			<section>
				<h2>On This Page</h2>
				<ol>
					{#each mechanic.sections as section (section.heading)}<li>
							<a href="#{section.anchor}">{section.heading}</a>
						</li>{/each}
				</ol>
			</section>
			{#if data.others.length}<section>
					<h2>More Mechanics</h2>
					<ul>
						{#each data.others as other (other.key)}<li>
								<EntityLink
									entity={{
										kind: 'mechanic',
										key: other.key,
										name: other.name,
										slug: other.slug,
										icon: null
									}}
								/>
							</li>{/each}
					</ul>
				</section>{/if}
		</nav>
		<div class="sections text-main" data-main>
			{#each mechanic.sections as section (section.heading)}<Section
					id={section.anchor!}
					title={section.heading}
				>
					{#each section.paragraphs as paragraph, index (index)}<p>
							<LinkedText text={paragraph} />
						</p>{/each}
					{#if section.table}
						{@const table = section.table}
						{@const numeric = numericColumns(table)}
						<RelationTable
							items={table.rows}
							label={section.heading}
							headings={table.headings}
							numeric={numeric.slice(1)}
							collapse={false}
						>
							{#snippet row(cells, index)}
								{@const anchor = table.anchors?.[index]}
								<tr id={anchor} class:anchor-target={anchor && page.url.hash === `#${anchor}`}>
									{#each cells as cell, column (column)}
										{@const chances = table.chanceColumns?.[column]?.[index]}
										<td
											class:numeric={numeric[column]}
											class:event-description={table.anchors && column > 0 && !numeric[column]}
										>
											{#if chances}
												<div>
													{#each chances as chance, entry (entry)}
														{@const id = `${section.anchor}-${index}-${column}-${entry}`}
														{#if chance.label}
															<div class="labelled-chance">
																{#if chance.labelAnchor}
																	<a href={`#${chance.labelAnchor}`}>{chance.label}</a>
																{:else}
																	<span>{chance.label}</span>
																{/if}
																<Chance min={chance.value} {id} />
															</div>
														{:else}
															<Chance min={chance.value} {id} />
														{/if}
													{/each}
												</div>
											{:else if column === 0 && table.rowNotes?.[index]}
												<div>
													<LinkedText text={cell} />
													<p class="event-note">{table.rowNotes[index]}</p>
												</div>
											{:else}
												<LinkedText text={cell} />
											{/if}
										</td>
									{/each}
								</tr>
							{/snippet}
						</RelationTable>
					{/if}
				</Section>{/each}
			<details class="disclosure" data-developer>
				<summary>For Developers</summary>
				<ul class="evidence">
					{#each mechanic.evidence as entry, index (index)}<li>{entry}</li>{/each}
				</ul>
			</details>
		</div>
	</div>
</EntityPage>

<style>
	.text-layout {
		margin-top: var(--gap-section);
	}
	tr:target,
	tr.anchor-target {
		background: var(--surface-raised);
	}
	@container (width < 40rem) {
		.event-description {
			flex-direction: column;
		}
		.event-description > div {
			align-self: stretch;
		}
	}
	.labelled-chance {
		display: flex;
		justify-content: space-between;
		gap: var(--gap-inline);
		white-space: nowrap;
	}
	.event-note {
		margin-top: var(--gap-inline);
		color: var(--text-soft);
	}
	.evidence {
		display: grid;
		gap: var(--gap-inline);
		color: var(--text-soft);
	}
</style>
