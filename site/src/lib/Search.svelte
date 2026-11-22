<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { browseKinds } from './entities.js';
	import IconSlot from './ui/IconSlot.svelte';
	/**
	 * Search over every page, read from the Pagefind index the build writes. Each result shows its icon, its name and a
	 * line with its kind and context, such as "Item · Weapon", so names that several kinds share stay apart.
	 */
	let {
		variant = 'header',
		placeholder = 'Search the compendium'
	}: { variant?: 'header' | 'hero'; placeholder?: string } = $props();
	const browse = $derived(browseKinds(page.data.kinds));
	const id = $props.id();

	interface Result {
		url: string;
		title: string;
		kind: string;
		detail: string | null;
		icon: string | null;
	}
	interface Pagefind {
		debouncedSearch(
			query: string,
			options?: object,
			timeout?: number
		): Promise<{
			results: { data(): Promise<{ url: string; meta: Record<string, string> }> }[];
		} | null>;
	}

	let query = $state('');
	let results = $state<Result[]>([]);
	let status = $state<'idle' | 'loading' | 'done' | 'failed'>('idle');
	let open = $state(false);
	let active = $state(-1);
	let input: HTMLInputElement | undefined = $state();
	let pagefind: Promise<Pagefind> | null = null;

	// The index loads on first use, so pages without a search pay nothing for it.
	function load(): Promise<Pagefind> {
		// A URL built at run time keeps the bundler from resolving the index, which only the build writes.
		const path = new URL('/pagefind/pagefind.js', location.origin).href;
		pagefind ??= import(/* @vite-ignore */ path) as Promise<Pagefind>;
		return pagefind;
	}

	async function search(value: string) {
		if (!value.trim()) {
			results = [];
			status = 'idle';
			return;
		}
		status = 'loading';
		try {
			const found = await (await load()).debouncedSearch(value, {}, 150);
			if (found === null) return;
			const shown = await Promise.all(found.results.slice(0, 8).map((result) => result.data()));
			if (value !== query) return;
			results = shown.map((entry) => ({
				url: entry.url.replace(/index\.html$/, ''),
				title: entry.meta.title ?? entry.url,
				kind: entry.meta.kind ?? 'Page',
				detail: entry.meta.detail ?? null,
				icon: entry.meta.icon ?? null
			}));
			active = results.length ? 0 : -1;
			status = 'done';
		} catch {
			status = 'failed';
		}
	}

	function keydown(event: KeyboardEvent) {
		if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
			if (!results.length) return;
			event.preventDefault();
			open = true;
			const step = event.key === 'ArrowDown' ? 1 : -1;
			active = (active + step + results.length) % results.length;
		} else if (event.key === 'Enter' && open && results[active]) {
			event.preventDefault();
			choose(results[active]);
		} else if (event.key === 'Escape') {
			open = false;
		}
	}

	function choose(result: Result) {
		open = false;
		query = '';
		results = [];
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- the index stores the site's own resolved paths.
		void goto(result.url);
	}

	// "/" focuses the search from anywhere outside a field, as the site's earlier search did.
	function shortcut(event: KeyboardEvent) {
		const target = event.target as HTMLElement | null;
		if (event.key !== '/' || target?.closest('input, textarea, select, [contenteditable]')) return;
		event.preventDefault();
		input?.focus();
	}
</script>

<svelte:window onkeydown={shortcut} />

<div
	class="search {variant}"
	role="search"
	onfocusout={(event) => {
		if (!event.currentTarget.contains(event.relatedTarget as Node | null)) open = false;
	}}
>
	<label class="field">
		<span class="sr-only">Search the compendium</span>
		<svg class="glass" viewBox="0 0 16 16" aria-hidden="true"
			><path
				d="M7 2a5 5 0 1 0 3.1 8.9l3.5 3.5 1-1-3.5-3.5A5 5 0 0 0 7 2Zm0 1.5a3.5 3.5 0 1 1 0 7 3.5 3.5 0 0 1 0-7Z"
			/></svg
		>
		<input
			bind:this={input}
			bind:value={query}
			type="search"
			role="combobox"
			autocomplete="off"
			spellcheck="false"
			aria-expanded={open && query.trim() !== ''}
			aria-controls={id + '-results'}
			aria-autocomplete="list"
			aria-activedescendant={open && active >= 0 ? `${id}-option-${active}` : undefined}
			{placeholder}
			onfocus={() => {
				open = true;
				void load();
			}}
			oninput={() => {
				open = true;
				void search(query);
			}}
			onkeydown={keydown}
		/>
	</label>
	{#if open && query.trim()}
		<div class="panel" id={id + '-results'}>
			{#if status === 'failed'}<p class="status">Search could not load. Try again later.</p>
			{:else if status === 'loading' && !results.length}<p class="status">Searching</p>
			{:else if status === 'done' && !results.length}<p class="status">
					Nothing found for "{query}".
				</p>
			{:else}
				<ul role="listbox" aria-label="Search results">
					{#each results as result, index (result.url)}
						<li
							id={`${id}-option-${index}`}
							role="option"
							aria-selected={index === active}
							class:active={index === active}
						>
							<!-- eslint-disable svelte/no-navigation-without-resolve -- the index stores the site's own resolved paths. -->
							<a
								href={result.url}
								tabindex="-1"
								onmouseenter={() => (active = index)}
								onclick={(event) => {
									event.preventDefault();
									choose(result);
								}}
							>
								<IconSlot icon={result.icon} size="row" />
								<span class="text">
									<span class="name" data-game>{result.title}</span>
									<span class="detail"
										>{[result.kind, result.detail].filter(Boolean).join(' · ')}</span
									>
								</span>
							</a>
							<!-- eslint-enable svelte/no-navigation-without-resolve -->
						</li>
					{/each}
				</ul>
			{/if}
		</div>
	{/if}
	<noscript
		>Search needs JavaScript. Browse the lists of {#each browse as entry, index (entry.path)}{index
				? index === browse.length - 1
					? ' and '
					: ', '
				: ''}<a href={entry.path}>{entry.label}</a>{/each} instead.</noscript
	>
</div>

<style>
	.search {
		position: relative;
		width: 100%;
	}
	/* The field is the visible box, so the glass sits beside the input in its flow. */
	.field {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
		min-height: var(--size-control);
		padding: 0 var(--inset);
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-1);
		background: var(--surface-raised);
	}
	.field:focus-within {
		outline: var(--focus-width) solid var(--focus);
		outline-offset: var(--focus-offset);
		border-color: var(--accent);
	}
	.glass {
		flex: none;
		width: var(--size-icon-inline);
		height: var(--size-icon-inline);
		fill: var(--text-muted);
	}
	/* The field draws the box, so the input inside it drops the site's text field style. */
	.field input {
		flex: 1;
		min-width: 0;
		align-self: stretch;
		min-height: 0;
		padding: 0;
		border: 0;
		background: none;
		color: var(--text);
		appearance: none;
	}
	.field input:focus {
		outline: none;
	}
	.field input::-webkit-search-cancel-button {
		appearance: none;
	}
	.hero .field {
		min-height: var(--size-control-large);
		border-radius: var(--radius-2);
		box-shadow: var(--shadow-raised);
	}
	.panel {
		position: absolute;
		z-index: var(--layer-search);
		top: var(--position-below);
		left: 0;
		right: 0;
		margin-top: var(--gap-line);
		max-height: var(--size-filter-sheet);
		overflow-y: auto;
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-2);
		background: var(--surface-raised);
		box-shadow: var(--shadow-raised);
	}
	ul {
		margin: 0;
		padding: var(--gap-line) 0;
		list-style: none;
	}
	[role='option'] a {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr);
		align-items: center;
		gap: var(--gap-block);
		padding: var(--gap-inline) var(--inset);
		color: var(--text);
		text-decoration: none;
	}
	.active a {
		background: var(--surface-hover);
	}
	.text {
		display: grid;
		min-width: 0;
	}
	.name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.detail {
		color: var(--text-muted);
		font-size: var(--type-meta);
		line-height: var(--leading-meta);
	}
	.status {
		margin: 0;
		padding: var(--inset);
		color: var(--text-muted);
	}
</style>
