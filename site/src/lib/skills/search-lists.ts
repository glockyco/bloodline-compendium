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
	for (const category of index.categories) {
		const count = browsed.filter((skill) => skill.category === category.slug).length;
		if (!count || category.slug === 'other') continue;
		records.push({
			url: url({ category: [category.slug] }),
			title: `${category.name} Skills`,
			words: [],
			summary: `All ${count} ${category.name.toLocaleLowerCase('en')} skills in the skill list.`
		});
	}
	const discipline = index.facets.find((facet) => facet.id === 'discipline');
	for (const value of discipline?.values ?? []) {
		const count = browsed.filter((skill) => skill.facets.discipline?.includes(value)).length;
		if (!count) continue;
		records.push({
			url: url({ discipline: [value] }),
			title: `${value} Skills`,
			words: [],
			summary: `All ${count} ${value} skills in the skill list.`
		});
	}
	return records;
}
