<script lang="ts">
	import { onMount } from 'svelte';
	import { SvelteURL } from 'svelte/reactivity';
	import { afterNavigate, replaceState } from '$app/navigation';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import EntityArticle from '#lib/ui/EntityArticle.svelte';
	import ItemVersion from '#lib/ItemVersion.svelte';
	import SeoHead from '#lib/SeoHead.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	import { titleCase } from '#lib/labels.js';
	import { describeItem } from '#lib/describe.js';

	let { data } = $props();
	const versions = $derived([data.item, ...(data.item.versions ?? [])]);
	let selected = $state(0);
	const item = $derived(versions[selected] ?? data.item);
	const kind = $derived(data.kinds.find((entry) => entry.kind === item.kind)!);
	const itemsPath = $derived(kindPath(item.kind, data.kinds)!);
	const description = $derived(
		describeItem(
			item,
			item.sources.filter((source) => !source.unconfirmed)
		)
	);
	const showType = $derived(
		item.type.toLocaleLowerCase('en') !== item.category.name.toLocaleLowerCase('en')
	);

	function selectFragment() {
		const fragment = window.location.hash.slice(1);
		const index = versions.findIndex((version) => version.versionId === fragment);
		selected = index < 0 ? 0 : index;
	}
	function select(index: number, focus = false) {
		selected = index;
		const url = new SvelteURL(page.url.href);
		url.hash = versions[index].versionId ?? '';
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- only the current resolved page's fragment changes.
		replaceState(url, page.state);
		if (focus) document.getElementById(`tab-${versions[index].versionId}`)?.focus();
	}
	function navigateTabs(event: KeyboardEvent, index: number) {
		let target: number;
		switch (event.key) {
			case 'ArrowRight':
				target = (index + 1) % versions.length;
				break;
			case 'ArrowLeft':
				target = (index + versions.length - 1) % versions.length;
				break;
			case 'Home':
				target = 0;
				break;
			case 'End':
				target = versions.length - 1;
				break;
			default:
				return;
		}
		event.preventDefault();
		select(target, true);
	}
	afterNavigate(selectFragment);
	onMount(() => {
		selectFragment();
		window.addEventListener('hashchange', selectFragment);
		return () => window.removeEventListener('hashchange', selectFragment);
	});
</script>

<SeoHead
	title={item.name + ' | The Bloodline Compendium'}
	{description}
	entity={{ kind: item.kind, key: item.key, name: item.name, slug: item.slug, icon: item.icon }}
	{kind}
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
<EntityArticle {kind}>
	<div class="item-heading">
		<h1 data-pagefind-meta="title" data-game>{item.name}</h1>
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
		{#if versions.length > 1}
			<div class="version-tabs" role="tablist" aria-label={item.name}>
				{#each versions as version, index (version.key)}
					<button
						type="button"
						role="tab"
						id={`tab-${version.versionId}`}
						aria-controls={version.versionId!}
						aria-selected={selected === index}
						aria-label={version.versionLabel ? undefined : (version.versionName ?? undefined)}
						tabindex={selected === index ? 0 : -1}
						onclick={() => select(index)}
						onkeydown={(event) => navigateTabs(event, index)}
					>
						<span class="version-icon"
							>{#if version.icon}<img
									src={`/icons/${version.icon}-160.webp`}
									alt=""
									width="48"
									height="48"
								/>{/if}</span
						>
						{#if version.versionLabel}<span>{version.versionLabel}</span>{/if}
					</button>
				{/each}
			</div>
		{/if}
	</div>
	{#each versions as version, index (version.key)}
		{#if versions.length > 1}
			<div
				id={version.versionId!}
				role="tabpanel"
				aria-labelledby={`tab-${version.versionId}`}
				hidden={selected !== index}
				tabindex="0"
			>
				<ItemVersion item={version} idPrefix={`${version.versionId}-`} />
			</div>
		{:else}
			<ItemVersion item={version} />
		{/if}
	{/each}
</EntityArticle>

<style>
	.version-tabs {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-block);
		margin-top: var(--gap-groups);
	}
	.version-tabs button {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: var(--gap-inline);
		width: var(--size-version-slot);
		padding: var(--inset-cell);
		border: var(--marker-width) solid var(--slot-edge);
		border-radius: var(--radius-1);
		background: var(--surface-slot);
		color: var(--text-muted);
		font-size: var(--type-0);
		line-height: var(--leading-meta);
		text-align: center;
	}
	.version-tabs button[aria-selected='true'] {
		border-color: var(--tooltip-frame);
		background: radial-gradient(var(--tooltip-surface), var(--tooltip-surface-edge));
		color: var(--tooltip-text);
	}
	.version-icon {
		display: grid;
		place-items: center;
		width: var(--size-icon-version);
		height: var(--size-icon-version);
	}
	.version-icon img {
		width: 100%;
		height: 100%;
		object-fit: contain;
	}
</style>
