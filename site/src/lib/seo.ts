/**
 * The production address. Canonical links, link previews and the sitemap use it on every host, so review hosts
 * never become duplicate indexed pages. `wrangler.jsonc`, `static/robots.txt` and the deploy scripts name it too.
 */
export const SITE_ORIGIN = 'https://the-bloodline.compendiums.org';

/** The wrong-fact form, whose Page field takes the page's address from the query, so a report names its page. */
export function wrongFactHref(pathname: string): string {
	return (
		'https://github.com/glockyco/bloodline-compendium/issues/new?template=wrong-fact.yml&page=' +
		encodeURIComponent(SITE_ORIGIN + pathname)
	);
}
