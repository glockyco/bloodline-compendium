<script lang="ts">
	import EntityLink from '../ui/EntityLink.svelte';
	import IconSlot from '../ui/IconSlot.svelte';
	import type { EntityKind } from '../site-data.js';
	/** A list row's name: its icon and name, linking to its page at the row's version. */
	let {
		row
	}: {
		row: {
			kind: EntityKind;
			key: string;
			name: string;
			slug: string;
			icon: string | null;
			versionId?: string | null;
		};
	} = $props();
</script>

<EntityLink
	class="entity-name"
	entity={{
		kind: row.kind,
		key: row.key,
		name: row.name,
		slug: row.slug,
		icon: row.icon,
		fragment: row.versionId ?? null
	}}><IconSlot icon={row.icon} size="list" /><span data-game>{row.name}</span></EntityLink
>
