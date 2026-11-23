<script lang="ts">
	import SeoHead from '#lib/SeoHead.svelte';
	import Search from '#lib/Search.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	import ButtonLink from '#lib/ui/ButtonLink.svelte';
	import IconTile from '#lib/ui/IconTile.svelte';
	import Section from '#lib/ui/Section.svelte';
	import { formatNumber } from '#lib/format.js';
	let { data } = $props();
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
			The Bloodline is a sandbox fantasy RPG where nearly everything you do has a level, with {formatNumber(
				data.skillCount
			)} skills to unlock. This wiki is generated from the game's files.
		</p>
		<div class="hero-search">
			<Search variant="hero" placeholder="Search items, skills and more" />
		</div>
		<div class="actions">
			{#each data.actions as action, index (action.href)}<ButtonLink
					href={action.href}
					secondary={index > 0}>{action.label}</ButtonLink
				>{/each}
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
	{#each data.sections as section (section.kind)}<Section id={section.id} title={section.title}
			>{#snippet action()}<a href={section.all.href}>{section.all.text}</a>{/snippet}
			<ul class="tile-grid">
				{#each section.tiles as tile (tile.href)}<li>
						<IconTile
							href={tile.href}
							icon={tile.icon}
							name={tile.name}
							meta={tile.meta}
							game={tile.game}
						/>
					</li>{/each}
			</ul>
		</Section>{/each}
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
		max-width: var(--size-measure);
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
		display: grid;
		gap: var(--gap-section);
		max-width: var(--size-page);
		margin: 0 auto;
		padding: var(--gap-section) var(--gutter) var(--gap-page);
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
