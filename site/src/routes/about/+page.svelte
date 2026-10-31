<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	let { data } = $props();
</script>

<SeoHead
	title="About | The Bloodline Compendium"
	description="How this unofficial The Bloodline item reference reads the game, what it cannot tell you and which game release it describes."
/>
<div class="sections">
	<header class="page-heading">
		<h1>About</h1>
		<p class="intro">
			The Bloodline Compendium is an unofficial item reference for players of <a
				href="https://store.steampowered.com/app/1159290/The_Bloodline/">The Bloodline</a
			>, a single-player sandbox adventure RPG with exploration, crafting, weapons and magic.
		</p>
	</header>
	<section aria-labelledby="how-made">
		<h2 id="how-made">How This Reference Is Made</h2>
		<p>
			Item names, descriptions, stats, icons and the ways to get each item are read from the game's
			files. Where the game's code decides a value, such as a price, a drop chance or the armor a
			piece adds, item pages show the value the game applies, and runs of the game confirm it.
		</p>
		<p>
			Where the game's own tooltip disagrees with what the game does, item pages show what the game
			applies beside what the tooltip says. <a href={resolve('known-issues/')}>Known Issues</a> lists
			these defects with steps to see them yourself.
		</p>
	</section>
	<section aria-labelledby="limits">
		<h2 id="limits">What It Cannot Tell You</h2>
		<ul>
			<li>
				Some items are given by quests and events this reference does not read yet. Their pages say
				that no way to get them is known, which does not mean they cannot be obtained.
			</li>
			<li>
				A place on an item page is where the game puts a merchant, container or enemy. Quests and
				your progress decide when you can reach it.
			</li>
			<li>
				Prices change with your respect in the merchant's city. Pages show the price at neutral
				respect.
			</li>
			<li>
				A drop chance appears only where the game's code that chooses the item is read. Elsewhere
				the chance reads Unknown with the reason.
			</li>
		</ul>
	</section>
	<section aria-labelledby="game-release">
		<h2 id="game-release">Game Release and Data</h2>
		<ReleaseInfo release={data.release} />
		<p>
			The reference describes this game release. A later game update may change an item's properties
			or sources. Read the linked patch notes for the game's changes.
		</p>
	</section>
	<section aria-labelledby="support">
		<h2 id="support">Support</h2>
		<p>
			If this reference helps you, you can <a href="https://ko-fi.com/wowmuch"
				>support it on Ko-fi</a
			>.
		</p>
	</section>
	<section aria-labelledby="disclaimer">
		<h2 id="disclaimer">Disclaimer</h2>
		<p>
			The Bloodline Compendium is an unofficial fan project. It is not affiliated with, endorsed by
			or sponsored by Shieldbearer Studios, 505 Pulse or 505 Games. The Bloodline and its names,
			artwork, trademarks and game assets belong to their respective rights holders.
		</p>
	</section>
</div>
