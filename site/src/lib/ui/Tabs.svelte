<script lang="ts" generics="Id extends string">
	import type { Snippet } from 'svelte';
	import { onMount, tick } from 'svelte';
	import { SvelteURL } from 'svelte/reactivity';
	import { replaceState } from '$app/navigation';
	import { page } from '$app/state';
	import Count from './Count.svelte';
	/**
	 * A page's parts as tabs, each with its own address fragment so a link can open one, and the arrow keys, Home and
	 * End move between them. Tabs stay in one row, so where they do not fit, such as five groups on a phone, a
	 * dropdown of the same parts replaces them rather than a row that scrolls or wraps. Until the page has its script,
	 * every panel shows with its own heading, so a reader without JavaScript and search still see all of them.
	 */
	let {
		tabs,
		label,
		initial,
		panel
	}: {
		tabs: readonly { id: Id; label: string; count?: number }[];
		/** What the tab list is, for screen readers, such as "Character sections". */
		label: string;
		/** The tab shown when the address names none. */
		initial: Id;
		/** A panel's content. The flag says whether the tabs show, so a panel can leave out a heading its tab repeats. */
		panel: Snippet<[Id, boolean]>;
	} = $props();
	let selected: Id | null = $state(null);
	// Whether the tabs overflow their row, measured on a hidden copy so the choice never flickers.
	let overflow = $state(false);
	let measure: HTMLDivElement | undefined = $state();
	let container: HTMLDivElement | undefined = $state();
	const select_id = $props.id();
	function selectFragment() {
		const fragment = window.location.hash.slice(1);
		selected = tabs.find((entry) => entry.id === fragment)?.id ?? initial;
	}
	function select(id: Id, focus = false) {
		selected = id;
		const url = new SvelteURL(page.url.href);
		url.hash = id;
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- only the current page's fragment changes.
		replaceState(url, page.state);
		if (focus) document.getElementById(`tab-${id}`)?.focus();
	}
	function navigate(event: KeyboardEvent, index: number) {
		const targets: Record<string, number> = {
			ArrowRight: (index + 1) % tabs.length,
			ArrowLeft: (index + tabs.length - 1) % tabs.length,
			Home: 0,
			End: tabs.length - 1
		};
		const target = targets[event.key];
		if (target === undefined) return;
		event.preventDefault();
		select(tabs[target].id, true);
	}
	onMount(() => {
		selectFragment();
		window.addEventListener('hashchange', selectFragment);
		const observer = new ResizeObserver(() => {
			if (!measure || !container) return;
			const next = measure.scrollWidth > container.clientWidth;
			if (next === overflow) return;
			// The tab row and the select replace each other, so focus on one moves to the other instead of the page.
			const focused =
				container.contains(document.activeElement) && !measure.contains(document.activeElement);
			overflow = next;
			if (focused)
				void tick().then(() =>
					(next
						? document.getElementById(select_id)
						: document.getElementById(`tab-${selected}`)
					)?.focus()
				);
		});
		if (container) observer.observe(container);
		return () => {
			window.removeEventListener('hashchange', selectFragment);
			observer.disconnect();
		};
	});
</script>

<div class="tabbed" bind:this={container}>
	{#if selected !== null}<div class="measure" aria-hidden="true" bind:this={measure}>
			{#each tabs as entry (entry.id)}<span class="tab-copy"
					>{entry.label}{#if entry.count !== undefined}&nbsp;<Count
							value={entry.count}
						/>{/if}</span
				>{/each}
		</div>{/if}
	{#if selected !== null && overflow}<label class="choose" for={select_id}
			><span class="field-label">{label}</span><select
				id={select_id}
				value={selected}
				onchange={(event) => select(event.currentTarget.value as Id)}
				>{#each tabs as entry (entry.id)}<option value={entry.id}
						>{entry.label}{entry.count !== undefined ? ` (${entry.count})` : ''}</option
					>{/each}</select
			></label
		>{:else if selected !== null}<div class="tabs" role="tablist" aria-label={label}>
			{#each tabs as entry, index (entry.id)}<button
					type="button"
					role="tab"
					id="tab-{entry.id}"
					aria-controls="panel-{entry.id}"
					aria-selected={selected === entry.id}
					tabindex={selected === entry.id ? 0 : -1}
					onclick={() => select(entry.id)}
					onkeydown={(event) => navigate(event, index)}
					>{entry.label}{#if entry.count !== undefined}&nbsp;<Count
							value={entry.count}
						/>{/if}</button
				>{/each}
		</div>{/if}
	<!-- A tab panel takes focus, as WAI-ARIA's tabs pattern asks, so Tab reaches it from its tab. -->
	{#each tabs as entry (entry.id)}<!-- svelte-ignore a11y_no_noninteractive_tabindex -->
		<div
			id="panel-{entry.id}"
			role={selected === null ? undefined : 'tabpanel'}
			aria-labelledby={selected === null ? undefined : `tab-${entry.id}`}
			hidden={selected !== null && selected !== entry.id}
			tabindex={selected !== null && !overflow ? 0 : undefined}
			aria-label={selected !== null && overflow ? entry.label : undefined}
			class="panel-body"
		>
			{@render panel(entry.id, selected !== null)}
		</div>{/each}
</div>

<style>
	.tabbed {
		position: relative;
		display: grid;
		gap: var(--gap-section);
		min-width: 0;
	}
	.tabs,
	.measure {
		display: flex;
		gap: var(--gap-inline);
		border-bottom: var(--border-width) solid var(--border-control);
	}
	/* A copy of the tab row that takes no space, measured to tell whether the real row fits. It is as wide as the
	   real row and hides what overflows, so its scroll width tells the row's need without widening the page. */
	.measure {
		position: absolute;
		inset-inline: 0;
		overflow: hidden;
		visibility: hidden;
		pointer-events: none;
	}
	.tab-copy {
		flex: 0 0 auto;
		padding: var(--inset-cell) var(--inset);
		white-space: nowrap;
	}
	.choose {
		display: grid;
		gap: var(--gap-line);
	}
	.choose select {
		width: 100%;
	}
	.tabs button {
		flex: 0 0 auto;
		display: inline-flex;
		align-items: center;
		padding: var(--inset-cell) var(--inset);
		border: 0;
		border-bottom: var(--marker-width) solid transparent;
		border-radius: 0;
		background: none;
		color: var(--text-muted);
	}
	.tabs button[aria-selected='true'] {
		border-bottom-color: var(--accent);
		color: var(--text);
	}
	.panel-body {
		display: grid;
		gap: var(--gap-section);
	}
	.panel-body[hidden] {
		display: none;
	}
</style>
