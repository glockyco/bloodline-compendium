import type { DisciplineIndex, SkillIndex } from '../site-data.js';

/** A skill window category on the home page: its disciplines, its skills with a known way to learn them, and icons. */
export interface HomeSkillGroup {
	slug: string;
	name: string;
	count: number;
	disciplines: string[];
	icons: string[];
}

export interface HomeSkills {
	/** Skills with a known way to learn them. */
	known: number;
	groups: HomeSkillGroup[];
}

/**
 * The home page's skill groups, one per category of the skill window in its order, each with the first five skill
 * icons of its list, as the item groups show theirs.
 */
export function homeSkills(disciplines: DisciplineIndex, skills: SkillIndex): HomeSkills {
	const categories = [
		...new Map(
			disciplines.disciplines.map((entry) => [entry.category.slug, entry.category])
		).values()
	];
	return {
		known: skills.skills.filter((skill) => skill.status === 'obtainable').length,
		groups: categories.map((category) => ({
			slug: category.slug,
			name: category.name,
			count: skills.skills.filter(
				(skill) => skill.category === category.slug && skill.status === 'obtainable'
			).length,
			disciplines: disciplines.disciplines
				.filter((entry) => entry.category.slug === category.slug)
				.map((entry) => entry.name),
			icons: skills.skills
				.filter((skill) => skill.category === category.slug && skill.icon)
				.slice(0, 5)
				.map((skill) => skill.icon!)
		}))
	};
}
