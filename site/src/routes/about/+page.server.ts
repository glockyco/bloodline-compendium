import { readDisciplines, readSkills } from '#lib/server/data.js';
import { homeSkills } from '#lib/skills/home.js';

export async function load() {
	const [disciplines, skills] = await Promise.all([readDisciplines(), readSkills()]);
	// About promises the skills the site shows a way to get, as the skill list counts them.
	return { skillCount: homeSkills(disciplines, skills).known };
}
