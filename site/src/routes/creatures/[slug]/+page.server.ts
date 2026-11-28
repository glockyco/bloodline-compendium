import { readCreature, readCreatures } from '#lib/server/data.js';
import { entityRoute } from '#lib/server/entity-route.js';

export const { entries, load } = entityRoute({
	name: 'Creature',
	index: readCreatures,
	slugs: (index) => index.creatures.map((creature) => creature.slug),
	load: async (slug) => ({ creature: await readCreature(slug) })
});
