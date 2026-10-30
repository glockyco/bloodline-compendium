/**
 * The Worker on `bloodline.compendiums.org`, the shorter address, which answers every request with a permanent
 * redirect to the same path and query on the site's address. It has no assets and no other code, so it stays far
 * inside the Workers free tier. `wrangler.redirect.jsonc` deploys it.
 */
import { SITE_ORIGIN } from './lib/seo.js';

export default {
	fetch(request: Request): Response {
		const source = new URL(request.url);
		const target = new URL(source.pathname + source.search, SITE_ORIGIN).toString();
		return new Response(null, {
			status: 301,
			headers: {
				Location: target,
				Link: `<${target}>; rel="canonical"`,
				'Cache-Control': 'public, max-age=3600'
			}
		});
	}
};
