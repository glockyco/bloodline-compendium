<script lang="ts">
	import { resolve } from '$app/paths';
	import Section from '../ui/Section.svelte';
	import { bugRows } from './bugs.js';
	import LinkedText from '../ui/LinkedText.svelte';
	import type { Fact, IssueNote } from '../site-data.js';
	/**
	 * An entity page's Bugs: its known issues with a link to each, then every tooltip value no issue explains. The
	 * entity's name inside a sentence reads as game text.
	 */
	let {
		notes,
		facts = [],
		name,
		idPrefix = ''
	}: { notes: IssueNote[]; facts?: Fact[]; name: string; idPrefix?: string } = $props();
	const bugs = $derived(bugRows({ notes, facts }));
</script>

<!-- The page's own name is the game's text, so it keeps the game's wording inside the site's sentence. -->
{#snippet named(text: string)}{#each text.split(name) as part, index (index)}{#if index}<span
				data-game>{name}</span
			>{/if}<LinkedText text={part} />{/each}{/snippet}

{#if bugs.length}
	<Section id={idPrefix + 'bugs'} title="Bugs" count={bugs.length}>
		<ul class="surface bug-rows">
			{#each bugs as row (row.kind === 'issue' ? row.issue : row.fact.stat)}
				<li>
					{#if row.kind === 'issue'}<span>{@render named(row.sentence)}</span>
						{#if row.workaround}
							<span><strong>Workaround:</strong> {@render named(row.workaround)}</span>
						{/if}
						<small
							><!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- the resolved issue page with its section. -->
							<a href={resolve('known-issues/') + '#' + row.issue}>Read about this issue</a></small
						>
					{:else}<span>{row.sentence}</span>{#if row.fact.gameShowsIssue}<small
								><!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- the resolved issue page with its section. -->
								<a href={resolve('known-issues/') + '#' + row.fact.gameShowsIssue}
									>Read about this issue</a
								></small
							>{/if}
					{/if}
				</li>
			{/each}
		</ul>
	</Section>
{/if}
