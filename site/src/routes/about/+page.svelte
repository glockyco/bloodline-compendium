<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import { wrongFactHref } from '#lib/seo.js';
	let { data } = $props();
	const dateFormat = new Intl.DateTimeFormat('en', { dateStyle: 'long', timeZone: 'UTC' });
	const date = (value: string) => dateFormat.format(new Date(value));
	const steam = 'https://store.steampowered.com/app/1159290/The_Bloodline/';
</script>

<SeoHead
	title="About | The Bloodline Compendium"
	description="What The Bloodline Compendium is and how it is made."
/>
<header class="page-heading">
	<h1>About The Bloodline Compendium</h1>
	<p class="intro">
		The Bloodline Compendium is a fan-made wiki for The Bloodline, generated from the game's files.
	</p>
</header>
<div class="about-layout">
	<div class="sections">
		<section aria-labelledby="game">
			<h2 id="game">About The Bloodline</h2>
			<p>
				The Bloodline is a single-player sandbox RPG in an open fantasy world, played in first
				person. You follow questlines or roam freely, join guilds, build your village and fight with
				weapons or magic. Nearly every action you take has its own level. There are over 500 skills
				to unlock. Shieldbearer Studios makes the game, and 505 Pulse and 505 Games publish it. It
				has been in Steam Early Access since October&nbsp;5,&nbsp;2023.
			</p>
			<p><a href={steam} rel="external">Play The Bloodline on Steam</a></p>
		</section>
		<section aria-labelledby="how-made">
			<h2 id="how-made">How the Compendium Is Made</h2>
			<p>
				The Compendium is updated after each patch. This version covers the {data.release.title} from
				<time datetime={data.release.patchDate}>{date(data.release.patchDate)}</time>, and its data
				was read on <time datetime={data.release.dataDate}>{date(data.release.dataDate)}</time>.
				Quest and event rewards are not listed yet.
			</p>
			<p>
				<a href="https://github.com/glockyco/bloodline-compendium" rel="external">Code on GitHub</a>
			</p>
		</section>
		<section aria-labelledby="fan-project">
			<h2 id="fan-project">Unofficial Fan Project</h2>
			<p>
				The Bloodline Compendium is an unofficial fan project and is not affiliated with
				Shieldbearer Studios, 505 Pulse or 505 Games. The Bloodline and its names, artwork and
				assets belong to their owners.
			</p>
		</section>
	</div>
	<aside class="panel" aria-labelledby="more-links">
		<h2 id="more-links">More Links</h2>
		<ul>
			<li><a href={steam} rel="external">The Bloodline on Steam</a></li>
			<li><a href={data.release.patchNotesUrl} rel="external">Patch Notes</a></li>
			<li><a href={resolve('known-issues/')}>Known Issues</a></li>
			<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- an address on GitHub, not on this site. -->
			<li><a href={wrongFactHref('/about/')}>Report a wrong fact</a></li>
			<li><a href="https://ko-fi.com/wowmuch">Support on Ko-fi</a></li>
		</ul>
	</aside>
</div>

<style>
	/* The text at a reading measure with the links beside it, as on Afallon's About page. */
	.about-layout {
		display: grid;
		grid-template-columns: minmax(0, var(--size-measure)) var(--size-item-side);
		justify-content: space-between;
		align-items: start;
		gap: var(--gap-section);
		margin-top: var(--gap-section);
	}
	aside {
		display: grid;
		gap: var(--gap-block);
	}
	aside h2 {
		margin: 0;
		font-size: var(--type-sub);
	}
	ul {
		display: grid;
		gap: var(--gap-inline);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	@media (width < 64rem) {
		.about-layout {
			grid-template-columns: minmax(0, 1fr);
		}
	}
</style>
