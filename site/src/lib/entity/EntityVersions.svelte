<script
	lang="ts"
	generics="T extends { key: string; versionId?: string | null; versionLabel?: string | null; versionName?: string | null; icon: string | null }"
>
	import type { Snippet } from 'svelte';
	import { onMount } from 'svelte';
	import { SvelteURL } from 'svelte/reactivity';
	import { afterNavigate, replaceState } from '$app/navigation';
	import { page } from '$app/state';
	/**
	 * An entity page's heading and its versions: entities of one name that show different things share a page, one
	 * tab each, and the address fragment names the version shown.
	 */
	let {
		versions,
		name,
		selected = $bindable(0),
		heading,
		panel
	}: {
		versions: T[];
		/** The shared name, which labels the tab list. */
		name: string;
		selected?: number;
		heading: Snippet;
		panel: Snippet<[T, string]>;
	} = $props();

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

<div class="item-heading">
	{@render heading()}
	{#if versions.length > 1}
		<div class="version-tabs" role="tablist" aria-label={name}>
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
			{@render panel(version, `${version.versionId}-`)}
		</div>
	{:else}
		{@render panel(version, '')}
	{/if}
{/each}

<style>
	/* Slots shrink rather than wrap, so three versions stay on one row on a phone. */
	.version-tabs {
		display: flex;
		gap: var(--gap-block);
		margin-top: var(--gap-groups);
	}
	.version-tabs button {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: var(--gap-inline);
		flex: 0 1 var(--size-version-slot);
		min-width: 0;
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
