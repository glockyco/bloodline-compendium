<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import Search from '#lib/Search.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	import { formatNumber } from '#lib/format.js';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import Card from '#lib/ui/Card.svelte';
	import CardGrid from '#lib/ui/CardGrid.svelte';
	import Count from '#lib/ui/Count.svelte';
	import IconSlot from '#lib/ui/IconSlot.svelte';
	import SectionHeading from '#lib/ui/SectionHeading.svelte';
	import { browseKinds, kindPath } from '#lib/entities.js';
	let { data } = $props();
	const itemsPath = $derived(kindPath('item', data.kinds)!);
</script>

<SeoHead
	title="The Bloodline Compendium | Wiki and Reference"
	description="Where to get every item and skill in The Bloodline, what they do and their stats. A fan-made wiki generated from the game's files."
	website
/>

<section class="hero" aria-labelledby="title">
	<div class="art">
		<img
			src="/art/hero-1920.webp"
			srcset="/art/hero-1280.webp 1280w, /art/hero-1920.webp 1920w, /art/hero-2560.webp 2560w, /art/hero-3840.webp 3840w"
			sizes="100vw"
			width="3840"
			height="1240"
			alt=""
			fetchpriority="high"
		/>
	</div>
	<div class="hero-body">
		<h1 id="title">The Bloodline Compendium</h1>
		<p class="lede">
			All {formatNumber(data.count)} items and {formatNumber(data.skills.known)} skills in The Bloodline,
			with where to get them and what they do.
		</p>
		<div class="hero-search">
			<Search
				variant="hero"
				placeholder="Search items, skills and more, for example {data.example}"
			/>
		</div>
		<div class="actions">
			{#each browseKinds(data.kinds) as entry (entry.path)}<ButtonLink href={entry.path}
					>Browse {entry.label}</ButtonLink
				>{/each}
			<a href="https://store.steampowered.com/app/1159290/The_Bloodline/" rel="external"
				>The Bloodline on Steam</a
			>
		</div>
		<ReleaseInfo release={data.release} />
	</div>
</section>

<div class="content">
	<section aria-labelledby="items-heading">
		<SectionHeading
			id="items-heading"
			title="Items"
			link={{ href: itemsPath, label: `${formatNumber(data.known)} items with a known source` }}
		/>
		<CardGrid rows={3}>
			{#each data.groups as group (group.slug)}
				<Card href={kindPath('item', data.kinds, `?category=${group.slug}`)!}>
					<span class="group-head"
						><h3>{group.name}</h3>
						<Count value={group.count} /></span
					>
					<span class="group-text">{group.description}</span>
					<!-- Five slots in every card, empty where a category has fewer icons, so rows keep one rhythm. -->
					<span class="slots" data-ui="slots">
						{#each [0, 1, 2, 3, 4] as slot (slot)}<IconSlot
								icon={group.icons[slot] ?? null}
							/>{/each}
					</span>
				</Card>
			{/each}
		</CardGrid>
		{#if data.minor.length}<p class="minor">
				<span>Also</span>
				{#each data.minor as category (category.slug)}<a
						href={kindPath('item', data.kinds, `?category=${category.slug}`)!}
						>{category.plural} <Count value={category.count} /></a
					>{/each}
			</p>{/if}
	</section>

	<section aria-labelledby="skills-heading">
		<SectionHeading
			id="skills-heading"
			title="Skills"
			link={{
				href: kindPath('skill', data.kinds)!,
				label: `${formatNumber(data.skills.known)} skills with a known way to learn them`
			}}
		/>
		<CardGrid rows={3} columns={4}>
			{#each data.skills.groups as group (group.slug)}
				<Card href={kindPath('discipline', data.kinds, '#' + group.slug)!}>
					<span class="group-head"
						><h3>{group.name}</h3>
						<Count value={group.count} /></span
					>
					<span class="group-text" data-game>{group.disciplines.join(', ')}</span>
					<span class="slots" data-ui="slots">
						{#each [0, 1, 2, 3, 4] as slot (slot)}<IconSlot
								icon={group.icons[slot] ?? null}
							/>{/each}
					</span>
				</Card>
			{/each}
		</CardGrid>
	</section>
</div>

<style>
	.hero {
		position: relative;
		margin: 0;
	}
	.art {
		position: relative;
		aspect-ratio: 3.1 / 1;
		min-height: var(--size-hero-min);
		max-height: var(--size-hero-max);
		overflow: hidden;
		background: var(--surface-raised);
	}
	.art img {
		display: block;
		width: 100%;
		height: 100%;
		object-fit: cover;
		object-position: 50% 30%;
	}
	.art::after {
		content: '';
		position: absolute;
		inset: auto 0 0;
		height: 40%;
		background: linear-gradient(var(--surface-fade), var(--surface) 96%);
	}
	.hero-body {
		position: relative;
		max-width: var(--size-page);
		margin: calc(-1 * var(--gap-page)) auto 0;
		padding: 0 var(--gutter);
	}
	h1 {
		margin: 0;
		font-size: var(--type-6);
		line-height: var(--leading-display);
		text-wrap: balance;
		text-shadow: var(--shadow-text);
	}
	.lede {
		max-width: var(--size-lede);
		margin: var(--gap-block) 0 var(--gap-section);
		font-size: var(--type-intro);
		line-height: var(--leading-intro);
		color: var(--text-soft);
	}
	.hero-search {
		max-width: var(--size-hero-search);
	}
	.actions {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--gap-block);
		margin: var(--gap-groups) 0;
	}

	.content {
		max-width: var(--size-page);
		margin: 0 auto;
		padding: 0 var(--gutter) var(--gap-page);
	}
	.content > section {
		margin: var(--gap-section) 0 0;
	}
	.group-head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: var(--gap-block);
	}
	h3 {
		margin: 0;
		font-size: var(--type-sub);
		line-height: var(--leading-heading);
	}
	.group-text {
		color: var(--text-muted);
		line-height: var(--leading-snug);
	}
	.slots {
		display: grid;
		grid-template-columns: repeat(5, minmax(0, 1fr));
		gap: var(--gap-inline);
		align-self: end;
	}
	.minor {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline) var(--gap-groups);
		margin: var(--gap-inline) 0 0;
		color: var(--text-muted);
	}

	@media (width < 64rem) {
		.art {
			aspect-ratio: auto;
			height: var(--size-hero-phone);
		}
		.art img {
			object-position: 42% 30%;
		}
		.hero-body {
			margin-top: calc(-1 * var(--gap-page));
		}
	}
</style>
