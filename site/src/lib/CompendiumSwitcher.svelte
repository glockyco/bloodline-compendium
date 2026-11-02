<script lang="ts">
	import { resolve } from '$app/paths';
	import { afterNavigate } from '$app/navigation';
	import { compendiums, iconUrl, thisCompendium } from './compendiums.js';

	// The logo stays the link home, so switching to another compendium has a control of its own.
	let open = $state(false);
	let root: HTMLElement;
	let toggle: HTMLButtonElement;
	afterNavigate(() => {
		open = false;
	});
</script>

<svelte:window
	onclick={(event) => {
		if (open && !root.contains(event.target as Node)) open = false;
	}}
	onkeydown={(event) => {
		if (open && event.key === 'Escape') {
			open = false;
			toggle.focus();
		}
	}}
/>

<div class="switcher" bind:this={root}>
	<button
		bind:this={toggle}
		type="button"
		class="toggle"
		aria-label="Switch compendium"
		title="Switch compendium"
		aria-expanded={open}
		aria-controls="compendium-switcher"
		onclick={() => (open = !open)}
	>
		<svg viewBox="0 0 24 24" aria-hidden="true">
			<rect x="3" y="3" width="7" height="7" rx="1" />
			<rect x="14" y="3" width="7" height="7" rx="1" />
			<rect x="3" y="14" width="7" height="7" rx="1" />
			<rect x="14" y="14" width="7" height="7" rx="1" />
		</svg>
	</button>
	<div id="compendium-switcher" class="panel" hidden={!open}>
		<p class="title">Game Compendiums</p>
		<ul>
			{#each compendiums as compendium (compendium.icon)}
				<li>
					{#if compendium.icon === thisCompendium}
						<a href={resolve('/')} aria-current="true">
							<img src={iconUrl(compendium)} alt="" width="128" height="128" />
							{compendium.name}
							<svg class="check" viewBox="0 0 24 24" aria-hidden="true"
								><path d="m5 12 5 5 9-10" /></svg
							>
						</a>
					{:else}
						<a href={compendium.url} rel="external">
							<img src={iconUrl(compendium)} alt="" width="128" height="128" />
							{compendium.name}
						</a>
					{/if}
				</li>
			{/each}
		</ul>
		<a class="about" href={resolve('about/')}>About this Compendium</a>
	</div>
</div>

<style>
	.switcher {
		position: relative;
		display: flex;
	}
	.toggle {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: var(--size-control-compact);
		height: var(--size-control-compact);
		padding: 0;
		color: var(--text);
		background: var(--surface-veil);
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-1);
		cursor: pointer;
		transition: background-color var(--duration-hover);
	}
	.toggle:hover,
	.toggle[aria-expanded='true'] {
		background: var(--surface-raised);
	}
	.toggle svg {
		width: var(--size-icon-control);
		height: var(--size-icon-control);
		fill: none;
		stroke: currentColor;
		stroke-width: 1.7;
	}
	/* The toggle sits at the end of the bar, so the panel opens towards the left. */
	.panel {
		position: absolute;
		top: var(--position-below);
		right: 0;
		z-index: var(--layer-tooltip);
		width: var(--size-switcher);
		margin-top: var(--gap-inline);
		padding: var(--inset-cell);
		background: var(--surface-panel);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		box-shadow: var(--shadow-raised);
	}
	.panel[hidden] {
		display: none;
	}
	.title {
		margin: 0;
		padding: var(--inset-line) var(--inset-cell);
		color: var(--text-muted);
		font-size: var(--type-meta);
	}
	ul {
		margin: 0;
		padding: 0;
		list-style: none;
	}
	a {
		display: flex;
		align-items: center;
		gap: var(--gap-block);
		min-height: var(--size-control);
		padding: 0 var(--inset-cell);
		border-radius: var(--radius-1);
		color: var(--text);
		text-decoration: none;
		white-space: nowrap;
	}
	a:hover {
		background: var(--surface-hover);
		color: var(--text);
	}
	a[aria-current='true'] {
		background: var(--surface-raised);
	}
	img {
		flex: none;
		width: var(--size-icon-row);
		height: var(--size-icon-row);
	}
	.check {
		width: var(--size-chevron);
		height: var(--size-chevron);
		margin-left: auto;
		fill: none;
		stroke: var(--gold);
		stroke-width: 2.5;
	}
	.about {
		margin-top: var(--gap-inline);
		border-top: var(--border-width) solid var(--border);
		border-radius: 0;
	}
</style>
