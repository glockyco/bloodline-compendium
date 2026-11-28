<script lang="ts">
	import ItemTooltip from '../items/ItemTooltip.svelte';
	import SkillTooltip from '../skills/SkillTooltip.svelte';
	import PassiveTooltip from '../skills/PassiveTooltip.svelte';
	import SpellsongTooltip from '../skills/SpellsongTooltip.svelte';
	import DisciplineTooltip from '../skills/DisciplineTooltip.svelte';
	import SummaryTooltip from './SummaryTooltip.svelte';
	import type { Preview } from './previews.js';
	/** Shows a preview in its kind's tooltip, as Afallon's presenter picks a tooltip per kind. */
	let { preview }: { preview: Preview } = $props();
</script>

{#if preview.kind === 'item'}<ItemTooltip item={preview} heading="p" />
	{#if preview.route}<p class="route">{preview.route}</p>{/if}
{:else if preview.kind === 'skill'}<SkillTooltip skill={preview} heading="p" />
	{#if preview.route}<p class="route">{preview.route}</p>{/if}
{:else if preview.kind === 'passive'}<PassiveTooltip passive={preview} />
{:else if preview.kind === 'spellsong'}<SpellsongTooltip song={preview} />
	{#if preview.route}<p class="route">{preview.route}</p>{/if}
{:else if preview.kind === 'discipline'}<DisciplineTooltip discipline={preview} />
{:else}<SummaryTooltip {preview} />{/if}

<style>
	.route {
		margin: var(--gap-block) var(--inset) 0;
		padding-top: var(--inset);
		border-top: var(--border-width) solid var(--border);
		color: var(--text-soft);
	}
</style>
