import { error, json } from '@sveltejs/kit';
import { previewSources } from '#lib/server/previews.js';

// Entity links load these on demand to show an entity's tooltip before the reader follows the link.
export const prerender = true;
export async function entries() {
	const all = [];
	for (const [kind, source] of Object.entries(previewSources))
		for (const slug of await source.slugs()) all.push({ kind, slug });
	return all;
}
export async function GET({ params }: { params: { kind: string; slug: string } }) {
	const source = previewSources[params.kind as keyof typeof previewSources];
	if (!source || !(await source.slugs()).includes(params.slug)) error(404, 'Preview not found');
	return json(await source.build(params.slug));
}
