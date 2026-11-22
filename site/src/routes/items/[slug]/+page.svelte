<script lang="ts">
	import EntityPage from '#lib/entity/EntityPage.svelte';
	import EntityVersions from '#lib/entity/EntityVersions.svelte';
	import ItemVersion from '#lib/items/ItemVersion.svelte';
	import { kindPath } from '#lib/entities.js';
	import { titleCase } from '#lib/labels.js';
	import { describeItem } from '#lib/items/describe.js';

	let { data } = $props();
	const versions = $derived([data.item, ...(data.item.versions ?? [])]);
	let selected = $state(0);
	const item = $derived(versions[selected] ?? data.item);
	const description = $derived(
		describeItem(
			item,
			item.sources.filter((source) => !source.unconfirmed),
			new Set(data.sharedMerchantNames)
		)
	);
	const showType = $derived(
		item.type.toLocaleLowerCase('en') !== item.category.name.toLocaleLowerCase('en')
	);
</script>

<EntityPage
	entity={{ kind: item.kind, key: item.key, name: item.name, slug: item.slug, icon: item.icon }}
	kinds={data.kinds}
	title={item.name}
	{description}
	render={item.renders[0]?.hash}
	game
	searchName={data.item.name}
	icon={data.item.icon}
	detail={titleCase(data.item.category.name) +
		(data.item.type.toLocaleLowerCase('en') !== data.item.category.name.toLocaleLowerCase('en')
			? ' · ' + titleCase(data.item.type)
			: '')}
>
	<EntityVersions {versions} name={item.name} bind:selected>
		{#snippet heading()}
			<h1 data-game>{item.name}</h1>
			<p class="identity">
				<a
					href={kindPath(
						item.kind,
						data.kinds,
						`?category=${encodeURIComponent(item.category.slug)}`
					)!}>{titleCase(item.category.name)}</a
				>{#if showType}<span aria-hidden="true">·</span><span data-game>{titleCase(item.type)}</span
					>{/if}
			</p>
		{/snippet}
		{#snippet panel(version, idPrefix)}<ItemVersion item={version} {idPrefix} />{/snippet}
	</EntityVersions>
</EntityPage>
