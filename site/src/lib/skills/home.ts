import type { DisciplineIndex, DisciplineSummary, SkillIndex } from '../site-data.js';

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
