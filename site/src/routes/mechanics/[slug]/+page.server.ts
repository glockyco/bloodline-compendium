import { readMechanic, readMechanics } from '#lib/server/data.js';
import { entityRoute } from '#lib/server/entity-route.js';

export const { entries, load } = entityRoute({
	name: 'Mechanic',
	index: readMechanics,
	slugs: (index) => index.mechanics.map((mechanic) => mechanic.slug),
	load: async (slug, { mechanics }) => ({
		mechanic: await readMechanic(slug),
		others: mechanics.filter((other) => other.slug !== slug)
	})
});
