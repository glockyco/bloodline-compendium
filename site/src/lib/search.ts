/** A page the site's search finds: its address, name, kind and context, such as "Item · Weapon", and icon. */
export interface SearchResult {
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

let pagefind: Promise<Pagefind> | null = null;

// The index loads on first use, so pages without a search pay nothing for it.
export function preloadSearch(): Promise<unknown> {
	return load();
}

function load(): Promise<Pagefind> {
	// A URL built at run time keeps the bundler from resolving the index, which only the build writes.
	const path = new URL('/pagefind/pagefind.js', location.origin).href;
	pagefind ??= import(/* @vite-ignore */ path) as Promise<Pagefind>;
	return pagefind;
}

/**
 * The best matches for a query from the Pagefind index the build writes, or null when a newer query replaced it
 * while it ran.
 */
export async function searchPages(query: string, limit = 8): Promise<SearchResult[] | null> {
	const found = await (await load()).debouncedSearch(query, {}, 150);
	if (found === null) return null;
	const shown = await Promise.all(found.results.slice(0, limit).map((result) => result.data()));
	return shown.map((entry) => ({
		url: entry.url.replace(/index\.html$/, ''),
		title: entry.meta.title ?? entry.url,
		kind: entry.meta.kind ?? 'Page',
		detail: entry.meta.detail ?? null,
		icon: entry.meta.icon ?? null
	}));
}
