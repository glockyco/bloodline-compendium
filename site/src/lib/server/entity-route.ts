import { error } from '@sveltejs/kit';

/**
 * The loader of a kind's page route: it prerenders one page per slug its index lists and answers any other slug with
 * a 404, so every kind's route agrees on which pages exist. Versions that share a slug share a page.
 */
export function entityRoute<Index, Data>(route: {
	/** The kind's name in a 404 message, such as "Skill". */
	name: string;
	index: () => Promise<Index>;
	slugs: (index: Index) => string[];
	load: (slug: string, index: Index) => Promise<Data>;
}) {
	return {
		entries: async () => [...new Set(route.slugs(await route.index()))].map((slug) => ({ slug })),
		load: async ({ params }: { params: { slug: string } }) => {
			const index = await route.index();
			if (!route.slugs(index).includes(params.slug)) error(404, `${route.name} not found`);
			return route.load(params.slug, index);
		}
	};
}
