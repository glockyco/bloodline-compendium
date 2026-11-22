import { readProfessions } from '#lib/server/data.js';
import { entityRoute } from '#lib/server/entity-route.js';
import { professionPage } from '#lib/server/professions.js';

export const { entries, load } = entityRoute({
	name: 'Profession',
	index: readProfessions,
	slugs: (index) => index.professions.map((profession) => profession.slug),
	load: (slug) => professionPage(slug, null)
});
