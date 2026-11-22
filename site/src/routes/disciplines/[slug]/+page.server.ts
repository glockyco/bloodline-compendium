import { readDiscipline, readDisciplines, readSkill } from '#lib/server/data.js';
import { entityRoute } from '#lib/server/entity-route.js';
import type { Fact } from '#lib/site-data.js';

export const { entries, load } = entityRoute({
	name: 'Discipline',
	index: readDisciplines,
	slugs: (index) => index.disciplines.map((discipline) => discipline.slug),
	load: async (slug) => {
		// The page marks every relation on the entries that show it, so the publication gate finds each one.
		const discipline = await readDiscipline(slug);
		// The skill table shows each skill's cooldown and cost, which its own page holds.
		const skillFacts: Record<string, Fact[]> = {};
		for (const row of discipline.skillRows)
			for (const slot of row.slots)
				if (slot.skill.key && slot.skill.slug)
					skillFacts[slot.skill.key] = (await readSkill(slot.skill.slug)).facts;
		return { discipline, skillFacts };
	}
});
