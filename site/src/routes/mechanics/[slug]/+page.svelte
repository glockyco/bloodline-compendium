<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import LinkedText from '#lib/ui/LinkedText.svelte';
	import EntityLink from '#lib/ui/EntityLink.svelte';
	import Section from '#lib/ui/Section.svelte';
	import RelationTable from '#lib/RelationTable.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { plainText } from '#lib/links.js';
	let { data } = $props();
	const mechanic = $derived(data.mechanic);
	const kind = $derived(data.kinds.find((entry) => entry.kind === mechanic.kind)!);
	const listPath = $derived(kindPath(mechanic.kind, data.kinds)!);
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

<SeoHead
	title={mechanic.name + ' | The Bloodline Compendium'}
	description={plainText(mechanic.intro)}
	entity={reference}
	{kind}
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Mechanics', path: listPath },
		{ name: mechanic.name, path: entityPath(reference, data.kinds)! }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li><a href={listPath}>Mechanics</a></li>
		<li aria-current="page">{mechanic.name}</li>
	</ol>
</nav>
<EntityArticle {kind} name={mechanic.name}>
	<header class="page-heading">
		<h1>{mechanic.name}</h1>
		<p class="intro"><LinkedText text={mechanic.intro} /></p>
	</header>
	<div class="text-layout">
		<nav class="text-side panel" aria-label="{mechanic.name} and other mechanics">
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
		<div class="sections text-main">
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
</EntityArticle>

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
