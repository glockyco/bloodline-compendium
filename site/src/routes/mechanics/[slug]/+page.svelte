<script lang="ts">
	import { alignSide } from '#lib/ui/align-side.js';
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import LinkedText from '#lib/ui/LinkedText.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import { plainText } from '#lib/links.js';
	let { data } = $props();
	const mechanic = $derived(data.mechanic);
	// A column of numbers aligns right, as numbers do in every table of the site.
	const numericColumns = (table: { headings: string[]; rows: string[][] }) =>
		table.headings.map((_, column) =>
			table.rows.every((cells) => /^\d[\d,.]*$/.test(cells[column]))
		);
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
					{#if section.table}<RelationTable
							items={section.table.rows}
							label={section.heading}
							headings={section.table.headings}
							numeric={numericColumns(section.table).slice(1)}
							collapse={false}
							>{#snippet row(cells)}<tr
									>{#each cells as cell, index (index)}<td
											class:numeric={numericColumns(section.table!)[index]}
											><LinkedText text={cell} /></td
										>{/each}</tr
								>{/snippet}</RelationTable
						>{/if}
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
	.evidence {
		display: grid;
		gap: var(--gap-inline);
		color: var(--text-soft);
	}
</style>
