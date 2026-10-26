import { defineConfig, type Plugin } from 'vitest/config';
import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import { createReadStream, statSync } from 'node:fs';
import { extname, join, normalize, sep } from 'node:path';
import { assetDirectory } from '../scripts/asset-directory.mjs';

const imageTypes: Record<string, string> = {
	'.webp': 'image/webp',
	'.png': 'image/png',
	'.jpg': 'image/jpeg'
};

// Generated images are copied into the build after prerendering, and the publication gate checks every link
// to them, so prerendering may treat them as missing.
const generatedImage =
	/^\/(icons|art|brand)\/|^\/(favicon-\d+x\d+|apple-touch-icon|og-default)\.png$/;

/**
 * Serves the images the build generates for the development server's data source. They live outside static/,
 * so a fixture build cannot replace the game images a running development server shows.
 */
function generatedImages(): Plugin {
	return {
		name: 'bloodline-generated-images',
		configureServer(server) {
			const root = assetDirectory(import.meta.dirname, process.env.BLOODLINE_SITE_DATA);
			server.middlewares.use((request, response, next) => {
				const path = normalize(decodeURIComponent((request.url ?? '').split('?')[0]));
				const file = join(root, path);
				const type = imageTypes[extname(file)];
				if (!type || !file.startsWith(root + sep)) return next();
				if (!statSync(file, { throwIfNoEntry: false })?.isFile()) return next();
				response.setHeader('content-type', type);
				response.setHeader('cache-control', 'no-cache');
				createReadStream(file).pipe(response);
			});
		}
	};
}

export default defineConfig({
	plugins: [
		generatedImages(),
		sveltekit({
			compilerOptions: {
				// Force runes mode for the project, except for libraries. Can be removed in svelte 6.
				runes: ({ filename }) =>
					filename.split(/[/\\]/).includes('node_modules') ? undefined : true
			},
			adapter: adapter(),
			prerender: {
				handleHttpError: ({ path, message }) => {
					if (generatedImage.test(path)) return;
					throw new Error(message);
				}
			}
		})
	],
	test: {
		expect: { requireAssertions: true },
		projects: [
			{
				extends: './vite.config.ts',
				test: {
					name: 'server',
					environment: 'node',
					include: ['src/**/*.{test,spec}.{js,ts}', 'tests/**/*.test.js'],
					exclude: ['src/**/*.svelte.{test,spec}.{js,ts}']
				}
			}
		]
	}
});
