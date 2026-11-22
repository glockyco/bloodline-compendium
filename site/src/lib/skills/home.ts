import { entityPath, kindPath } from '../entities.js';
import type { HomeSectionBody } from '../entity/home.js';
import { formatNumber, plural } from '../format.js';
import type {
	DisciplineIndex,
	DisciplineSummary,
	EntityKindDefinition,
	SkillIndex
} from '../site-data.js';

export interface HomeSkills {
	/** Skills with a known way to learn them, as the skill list shows them first. */
	known: number;
	/** Every discipline in the skill window's order, which the home page groups by category. */
	disciplines: DisciplineSummary[];
}

/** The home page's skill count and disciplines. */
export function homeSkills(disciplines: DisciplineIndex, skills: SkillIndex): HomeSkills {
	return {
		known: skills.skills.filter((skill) => skill.status === 'obtainable').length,
		disciplines: disciplines.disciplines
	};
}

/** The home page's discipline section: a tile per discipline, with a link to every skill. */
export function disciplineHomeSection(
	disciplines: DisciplineIndex,
	skills: SkillIndex,
	kinds: EntityKindDefinition[]
): HomeSectionBody {
	const data = homeSkills(disciplines, skills);
	return {
		all: { href: kindPath('skill', kinds)!, text: `All ${formatNumber(data.known)} skills` },
		tiles: data.disciplines.map((discipline) => ({
			href: entityPath(
				{
					kind: 'discipline',
					key: discipline.key,
					name: discipline.name,
					slug: discipline.slug,
					icon: null
				},
				kinds
			)!,
			icon: discipline.icon ?? null,
			name: discipline.name,
			meta: `${discipline.category.name} ·\u00a0${(discipline.skills ? plural(discipline.skills, 'skill', 'skills') : plural(discipline.spellsongs, 'spellsong', 'spellsongs')).replace(' ', '\u00a0')}`,
			game: true
		}))
	};
}
