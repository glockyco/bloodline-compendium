import { hiddenAvailability, writeState, type ListState } from '../list/state.js';
import type { ListRecord } from '../items/search-lists.js';
import type { SkillIndex } from '../site-data.js';
import { skillListModel } from './list.js';

/**
 * Search records for every skill window category and every discipline, linking to the skill list with that filter,
 * so that a search for pyromancy skills finds the list next to single skills.
 */
export function skillListRecords(index: SkillIndex, listPath: string): ListRecord[] {
	const browsed = index.skills.filter((skill) => !hiddenAvailability.includes(skill.status));
	const url = (facets: Record<string, string[]>) => {
		const state: ListState = {
			name: '',
			facets,
			stats: [],
			match: 'all',
			reveal: false,
			sort: { column: 'name', direction: 'asc' }
		};
		return `${listPath}?${writeState(state, skillListModel(index))}`;
	};
	const records: ListRecord[] = [];
	const entry = (members: SkillIndex['skills']) => ({
		words: [],
		detail: `${members.length} ${members.length === 1 ? 'skill' : 'skills'}`,
		icon: members.find((skill) => skill.icon)?.icon ?? null
	});
	for (const category of index.categories) {
		const members = browsed.filter((skill) => skill.category === category.slug);
		if (!members.length || category.slug === 'other') continue;
		records.push({
			url: url({ category: [category.slug] }),
			title: `${category.name} Skills`,
			summary: `All ${members.length} ${category.name.toLocaleLowerCase('en')} skills in the skill list.`,
			...entry(members)
		});
	}
	const discipline = index.facets.find((facet) => facet.id === 'discipline');
	for (const value of discipline?.values ?? []) {
		const members = browsed.filter((skill) => skill.facets.discipline?.includes(value));
		if (!members.length) continue;
		records.push({
			url: url({ discipline: [value] }),
			title: `${value} Skills`,
			summary: `All ${members.length} ${value} skills in the skill list.`,
			...entry(members)
		});
	}
	return records;
}
