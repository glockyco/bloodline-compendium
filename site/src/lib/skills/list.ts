import type { SkillIndex, SkillSummary } from '../site-data.js';
import type { ListModel } from '../list/state.js';

/** The skill list's model: its filters and columns, with no fixed columns besides the name. */
export function skillListModel(index: SkillIndex): ListModel<SkillSummary> {
	return { facets: index.facets, columns: index.columns, fixed: [] };
}
