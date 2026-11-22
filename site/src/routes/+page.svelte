<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import Search from '#lib/Search.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	import { formatNumber, plural } from '#lib/format.js';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import IconTile from '#lib/ui/IconTile.svelte';
	import SectionHeading from '#lib/ui/SectionHeading.svelte';
	import { entityPath, kindPath } from '#lib/entities.js';
	let { data } = $props();
	const itemsPath = $derived(kindPath('item', data.kinds)!);
	const skillsPath = $derived(kindPath('skill', data.kinds)!);
	const disciplinePath = (discipline: (typeof data.skills.disciplines)[number]) =>
		entityPath(
			{
				kind: 'discipline',
				key: discipline.key,
				name: discipline.name,
				slug: discipline.slug,
				icon: null
			},
			data.kinds
		)!;
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
			The Bloodline is a sandbox fantasy RPG where nearly everything you do has a level, with over
			500 skills to unlock. This wiki is generated from the game's files.
		</p>
		<div class="hero-search">
			<Search
				variant="hero"
				placeholder="Search items, skills and more, for example {data.example}"
			/>
		</div>
		<div class="actions">
			<ButtonLink href={itemsPath}>Browse Items</ButtonLink>
			<ButtonLink href={skillsPath} secondary>Browse Skills</ButtonLink>
		</div>
		<p class="links">
			<a href="https://store.steampowered.com/app/1159290/The_Bloodline/" rel="external"
				>The Bloodline on Steam</a
			>
		</p>
		<ReleaseInfo release={data.release} />
	</div>
</section>

<div class="content">
	<section aria-labelledby="items-heading">
		<SectionHeading
			id="items-heading"
			title="Items"
			link={{ href: itemsPath, label: `All ${formatNumber(data.known)} items` }}
		/>
		<ul class="tile-grid">
			{#each data.categories as category (category.slug)}<li>
					<IconTile
						href={kindPath('item', data.kinds, `?category=${category.slug}`)!}
						icon={category.icon}
						name={category.name}
						meta={plural(category.count, 'item', 'items')}
					/>
				</li>{/each}
		</ul>
	</section>

	<section aria-labelledby="disciplines-heading">
		<SectionHeading
			id="disciplines-heading"
			title="Disciplines"
			link={{ href: skillsPath, label: `All ${formatNumber(data.skills.known)} skills` }}
		/>
		<ul class="tile-grid">
			{#each data.skills.disciplines as discipline (discipline.key)}<li>
					<IconTile
						href={disciplinePath(discipline)}
						icon={discipline.icon ?? null}
						name={discipline.name}
						meta={`${discipline.category.name} · ${discipline.skills ? plural(discipline.skills, 'skill', 'skills') : plural(discipline.spellsongs, 'spellsong', 'spellsongs')}`}
						game
					/>
				</li>{/each}
		</ul>
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
	.links {
		margin: 0 0 var(--gap-block);
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
