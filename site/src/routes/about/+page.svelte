<script lang="ts">
	import { resolve } from '$app/paths';
	import SeoHead from '#lib/SeoHead.svelte';
	import ReleaseInfo from '#lib/ReleaseInfo.svelte';
	let { data } = $props();
</script>

<SeoHead
	title="About | The Bloodline Compendium"
	description="How this unofficial The Bloodline item reference uses game data, which game release it describes and where to find coverage limits and patch notes."
/>
<h1>About</h1>
<p class="intro">
	The Bloodline Compendium is an unofficial item reference for players of <a
		href="https://store.steampowered.com/app/1159290/The_Bloodline/">The Bloodline</a
	>, a single-player sandbox adventure RPG with exploration, crafting, weapons and magic.
</p>
<section aria-labelledby="how-made">
	<h2 id="how-made">How this reference is made</h2>
	<p>
		Item names, descriptions, stats, icons and known sources are read from the game's files. Item
		pages connect shops, recipes, drops, gathering and other ways to obtain an item. Their Used for
		tables show the recipes and purchases that require it.
	</p>
	<p>
		Unknown information stays unknown. A missing source does not prove an item is unobtainable, and
		a known source does not guarantee that its location is reachable. <a href={resolve('coverage/')}
			>Coverage</a
		> lists the gaps and explains the limits.
	</p>
	<p>
		Where the game's own tooltip or data disagrees with what the game does, item pages show what the
		game applies. <a href={resolve('known-issues/')}>Known Issues</a> lists the defects in the game that
		we reproduced, with steps to see them yourself.
	</p>
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
		If this reference helps you, you can <a href="https://ko-fi.com/wowmuch">support it on Ko-fi</a
		>.
	</p>
</section>
<section aria-labelledby="disclaimer">
	<h2 id="disclaimer">Disclaimer</h2>
	<p>
		The Bloodline Compendium is an unofficial fan project. It is not affiliated with, endorsed by or
		sponsored by Shieldbearer Studios, 505 Pulse or 505 Games. The Bloodline and its names, artwork,
		trademarks and game assets belong to their respective rights holders.
	</p>
</section>
