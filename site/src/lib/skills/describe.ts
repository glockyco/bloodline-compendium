import { plural } from '../format.js';
import type { DisciplineSummary, ItemStatus, SkillPage } from '../site-data.js';

const andList = new Intl.ListFormat('en', { type: 'conjunction' });

/** What a discipline teaches, counted, such as "31 skills and 13 passives", or null when it teaches nothing. */
export function disciplineContents(
	discipline: Pick<DisciplineSummary, 'skills' | 'passives' | 'spellsongs'>
): string | null {
	const parts = [
		discipline.skills ? plural(discipline.skills, 'skill', 'skills') : null,
		discipline.passives ? plural(discipline.passives, 'passive', 'passives') : null,
		discipline.spellsongs ? plural(discipline.spellsongs, 'spellsong', 'spellsongs') : null
	].filter((part) => part !== null);
	return parts.length ? andList.format(parts) : null;
}

/** What a skill page says when no confirmed way to learn the skill is known. */
export const skillStatusSentence: Record<ItemStatus, string> = {
	obtainable: '',
	noKnownSource: 'No known way to learn this skill.',
	notInNormalPlay: 'This skill is unused. The game never offers it to you.'
};

/** The status sentence a skill page opens with, or null when its sources answer how to learn it. */
export function skillStatus(skill: SkillPage): string | null {
	return skill.sources.some((source) => !source.unconfirmed)
		? null
		: skillStatusSentence[skill.status];
}
