import { attributes } from '../rules/attributes.js';
import type { PlannerData } from '../site-data.js';
import type { PlannerState } from './state.js';

/**
 * A build as players post it: the game build, the character level, the attribute split, the disciplines with their
 * levels, the skill bar's skills in order with their pages, and the build link. Attributes and disciplines still at 1
 * are left out, because every character starts there.
 */
export function buildText(
	state: PlannerState,
	data: PlannerData,
	link: string,
	/** A skill page's whole address, which the route builds from the kind registry. */
	skillUrl: (slug: string) => string
): string {
	const skills = new Map(data.skills.map((skill) => [skill.key, skill]));
	const raised = attributes.filter((attribute) => state.attributes.points[attribute] > 1);
	const disciplines = data.disciplines
		.map((discipline) => [discipline.name, state.skills.disciplines[discipline.key] ?? 1] as const)
		.filter(([, level]) => level > 1)
		.sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0], 'en'));
	const bar = state.skillBars.main.flatMap((key, slot) => {
		if (key === null) return [];
		const skill = skills.get(key);
		return [
			skill
				? `${slot + 1}. ${skill.name}: ${skillUrl(skill.slug)}`
				: `${slot + 1}. A skill this game version lacks`
		];
	});
	return [
		`The Bloodline build, game build ${state.build}`,
		`Level ${state.attributes.level}`,
		raised.length
			? 'Attributes: ' +
				raised.map((attribute) => `${attribute} ${state.attributes.points[attribute]}`).join(', ')
			: 'Attributes: all at 1',
		disciplines.length
			? 'Disciplines: ' + disciplines.map(([name, level]) => `${name} ${level}`).join(', ')
			: 'Disciplines: all at 1',
		...(bar.length ? ['Skill bar:', ...bar] : []),
		`Build: ${link}`
	].join('\n');
}
