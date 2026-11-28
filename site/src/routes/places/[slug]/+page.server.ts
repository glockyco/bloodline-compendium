import { readIndex, readPlace, readPlaces } from '#lib/server/data.js';
import { entityRoute } from '#lib/server/entity-route.js';

export const { entries, load } = entityRoute({
	name: 'Place',
	index: readPlaces,
	slugs: (index) => index.places.map((place) => place.slug),
	// The item categories name the breakdown's kinds in the plural the item list uses, such as Weapons.
	load: async (slug) => ({
		place: await readPlace(slug),
		categories: (await readIndex()).categories.map(({ slug, plural }) => ({ slug, plural }))
	})
});
