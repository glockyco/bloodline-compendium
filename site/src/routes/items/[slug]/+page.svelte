<script lang="ts">
	import { resolve } from '$app/paths';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import EntityVersions from '#lib/entity/EntityVersions.svelte';
	import ItemVersion from '#lib/items/ItemVersion.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { titleCase } from '#lib/labels.js';
	import { describeItem } from '#lib/items/describe.js';

	let { data } = $props();
	const versions = $derived([data.item, ...(data.item.versions ?? [])]);
	let selected = $state(0);
	const item = $derived(versions[selected] ?? data.item);
	const kind = $derived(data.kinds.find((entry) => entry.kind === item.kind)!);
	const itemsPath = $derived(kindPath(item.kind, data.kinds)!);
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

<SeoHead
	title={item.name + ' | The Bloodline Compendium'}
	{description}
	entity={{ kind: item.kind, key: item.key, name: item.name, slug: item.slug, icon: item.icon }}
	{kind}
	render={item.renders[0]?.hash}
	breadcrumbs={[
		{ name: 'Home', path: '/' },
		{ name: 'Items', path: itemsPath },
		{ name: item.name, path: entityPath(item, data.kinds)! }
	]}
/>
<nav class="breadcrumbs" aria-label="Breadcrumb">
	<ol>
		<li><a href={resolve('/')}>Home</a></li>
		<li><a href={itemsPath}>Items</a></li>
		<li aria-current="page" data-game>{item.name}</li>
	</ol>
</nav>
<EntityArticle
	{kind}
	name={data.item.name}
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
</EntityArticle>
