<script lang="ts">
	import { page } from '$app/state';
	import type { EntityReference, EntityKindDefinition } from './site-data.js';
	import { SITE_ORIGIN } from './seo.js';
	let {
		title,
		description,
		noindex = false,
		breadcrumbs = [],
		website = false,
		entity,
		kind
	}: {
		title: string;
		description: string;
		noindex?: boolean;
		breadcrumbs?: { name: string; path: string }[];
		website?: boolean;
		entity?: EntityReference;
		kind?: EntityKindDefinition;
	} = $props();
	const canonical = $derived(SITE_ORIGIN + page.url.pathname);
	const image = SITE_ORIGIN + '/og-default.png';
	const structuredData = $derived([
		...(entity && kind?.structuredDataType
			? [
					{
						'@context': 'https://schema.org',
						'@type': kind.structuredDataType,
						name: entity.name,
						identifier: entity.key,
						url: canonical
					}
				]
			: []),
		...(website
			? [
					{
						'@context': 'https://schema.org',
						'@type': 'WebSite',
						name: 'The Bloodline Compendium',
						url: SITE_ORIGIN + '/'
					}
				]
			: []),
		...(breadcrumbs.length
			? [
					{
						'@context': 'https://schema.org',
						'@type': 'BreadcrumbList',
						itemListElement: breadcrumbs.map((entry, index) => ({
							'@type': 'ListItem',
							position: index + 1,
							name: entry.name,
							item: SITE_ORIGIN + entry.path
						}))
					}
				]
			: [])
	]);
</script>

<svelte:head>
	<title>{title}</title>
	<meta name="description" content={description} />
	{#if noindex}<meta name="robots" content="noindex" />{/if}
	{#if !noindex}<link rel="canonical" href={canonical} />{/if}
	<meta property="og:type" content="website" />
	<meta property="og:site_name" content="The Bloodline Compendium" />
	<meta property="og:title" content={title} />
	<meta property="og:description" content={description} />
	<meta property="og:url" content={canonical} />
	<meta property="og:image" content={image} />
	<meta property="og:image:width" content="1200" />
	<meta property="og:image:height" content="630" />
	<meta property="og:image:alt" content="The Bloodline Compendium book emblem" />
	<meta name="twitter:card" content="summary_large_image" />
	<meta name="twitter:title" content={title} />
	<meta name="twitter:description" content={description} />
	<meta name="twitter:image" content={image} />
	<meta name="twitter:image:alt" content="The Bloodline Compendium book emblem" />
	{#each structuredData as entry, index (index)}
		<!-- eslint-disable-next-line svelte/no-at-html-tags -- Serialized JSON has '<' escaped to prevent script-tag breakout. The split closing tag avoids the Svelte parser's raw script handling. -->
		{@html '<script type="application/ld+json">' +
			JSON.stringify(entry).replace(/</g, '\\u003c') +
			'</' +
			'script>'}
	{/each}
	<link rel="icon" type="image/png" sizes="16x16" href="/favicon-16x16.png" />
	<link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png" />
	<link rel="icon" type="image/png" sizes="192x192" href="/favicon-192x192.png" />
	<link rel="icon" type="image/png" sizes="512x512" href="/favicon-512x512.png" />
	<link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png" />
</svelte:head>
