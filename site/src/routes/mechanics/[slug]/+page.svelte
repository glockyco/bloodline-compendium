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
						{@const chanceMatrix =
							table.chanceContext && numeric.every((value, column) => !column || value)}
						{#if table.chanceContext}
							<p>{table.chanceContext}</p>
						{/if}
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
										{@const cellLink = table.cellLinks?.[column]?.[index]}
										{@const rowNote = column === 0 ? table.rowNotes?.[index] : undefined}
										{@const showArrival = column === 0 && (chanceMatrix || table.arrivalLink)}
										<td
											class:numeric={numeric[column]}
											class:event-description={table.anchors && column > 0 && !numeric[column]}
											data-game={table.gameColumns?.includes(column) ? '' : undefined}
										>
											{#if chances}
												<div>
													{#if table.chanceContext && !numeric[column]}
														<p class="chance-context">{table.chanceContext}</p>
													{/if}
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
															{#if table.chanceContext}
																<span class="matrix-terrain" aria-hidden="true"
																	>{table.headings[column]}</span
																>
															{/if}
														{/if}
													{/each}
												</div>
											{:else if cellLink && !cellLink.label}
												<a href={`#${cellLink.anchor}`}>{cell}</a>
											{:else if rowNote || cellLink || showArrival}
												<div>
													<LinkedText text={cell} />
													{#if rowNote}<p class="event-note">{rowNote}</p>{/if}
													{#if cellLink}
														<p class="event-note">
															<a href={`#${cellLink.anchor}`}>{cellLink.label}</a>
														</p>
													{/if}
													{#if showArrival}
														<div class="arrival-context">
															{#if chanceMatrix}<p>{table.chanceContext}</p>{/if}
															{#if table.arrivalLink}
																<a href={`#${table.arrivalLink.anchor}`}
																	>{table.arrivalLink.label}</a
																>
															{/if}
														</div>
													{/if}
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
	.chance-context,
	.arrival-context,
	.matrix-terrain {
		display: none;
	}
	:is(tr:target, tr.anchor-target) .chance-context {
		display: block;
		margin-block-end: var(--gap-line);
		color: var(--text-soft);
	}
	:is(tr:target, tr.anchor-target) .arrival-context {
		display: block;
		margin-block-start: var(--gap-inline);
		color: var(--text-soft);
	}
	@container (width >= 40rem) {
		:is(tr:target, tr.anchor-target) .matrix-terrain {
			display: block;
			color: var(--text-muted);
			font-size: var(--type-meta);
		}
	}
	.event-note {
		margin-top: var(--gap-inline);
		color: var(--text-soft);
	}
</style>
