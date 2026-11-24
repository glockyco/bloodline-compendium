<script lang="ts">
	import EntityLink from '../ui/EntityLink.svelte';
	import IconSlot from '../ui/IconSlot.svelte';
	import type { EntityKind } from '../site-data.js';
	/**
	 * A list row's name: its icon beside its name, which links to its page at the row's version, with a meta line,
	 * such as the type phones show there, and a status note under the name.
	 */
	let {
		row,
		meta = null,
		note = null
	}: {
		row: {
			kind: EntityKind;
			key: string;
			name: string;
			slug: string;
			icon: string | null;
			versionId?: string | null;
		};
		meta?: string | null;
		note?: string | null;
	} = $props();
</script>

<span class="list-name"
	><IconSlot icon={row.icon} size="list" /><span class="lines"
		><EntityLink
			class="entity-name"
			entity={{
				kind: row.kind,
				key: row.key,
				name: row.name,
				slug: row.slug,
				icon: row.icon,
				fragment: row.versionId ?? null
			}}><span data-game>{row.name}</span></EntityLink
		>{#if meta}<span class="meta">{meta}</span>{/if}{#if note}<small class="note">{note}</small
			>{/if}</span
	></span
>

<style>
	/* The icon stands beside the name and the lines under it, so they all start at the name's edge. */
	.list-name {
		display: grid;
		grid-template-columns: var(--size-icon-list) auto;
		align-items: center;
		gap: var(--gap-inline);
	}
	.lines {
		display: grid;
		min-width: 0;
	}
	/* Lines under a name read smaller than it, as a place under a name does in relation tables. */
	.meta,
	.note {
		color: var(--text-muted);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
		margin: 0;
	}
	.meta {
		display: none;
	}
	/* Phones show the type here, as the table's Type column is gone, and top-align the icon with the name so icons
	   line up from row to row whatever the block's height. */
	@media (width < 40rem) {
		.list-name:has(.meta, .note) {
			align-items: start;
		}
		.meta {
			display: block;
		}
	}
</style>
