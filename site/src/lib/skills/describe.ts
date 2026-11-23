import { plural } from '../format.js';
import { proficiencyPointsPerLevel } from '../rules/progression.js';
import type { DisciplinePage, DisciplineSummary, ItemStatus, SkillPage } from '../site-data.js';

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

const points = (count: number) =>
	`${count} [${count === 1 ? 'proficiency point' : 'proficiency points'}](mechanic:proficiency-points)`;

/**
 * The sentences that open a discipline page, with typed links: how it levels, what its level unlocks and the
 * proficiency points each level up gives, from the proficiency rule. Blood Magic names its points defect in place of a
 * number, and a tree without a level of its own names the discipline its skills check.
 */
export function disciplineIntro(
	discipline: Pick<DisciplinePage, 'name' | 'levelName' | 'skillRows' | 'passiveRows'>
): string {
	const slots = discipline.skillRows.flatMap((row) => row.slots);
	const passives = discipline.passiveRows.some((row) => row.passives.length > 0);
	if (discipline.levelName === null) {
		const checked = [...new Map(slots.map((slot) => [slot.levelDiscipline.slug, slot])).values()];
		return `${discipline.name} has no level of its own. Its skills unlock at ${andList.format(
			checked.map(
				(slot) =>
					`[${slot.levelDiscipline.name}](discipline:${slot.levelDiscipline.slug}) level ${slot.requiredLevel}`
			)
		)}, and every discipline starts at level 1.`;
	}
	const unlocks = slots.length > 0 ? 'the skills below' : passives ? 'the passives below' : null;
	const opening =
		`${discipline.name} [levels up as you use it](mechanic:discipline-levels).` +
		(unlocks ? ` Its level unlocks ${unlocks}` : '');
	const first = proficiencyPointsPerLevel(discipline.levelName, 1);
	const later = proficiencyPointsPerLevel(discipline.levelName, 2);
	if (first.setByBloodLevels)
		return `${opening}, and each level up sets your ${discipline.name} proficiency points by your [Blood Levels](mechanic:blood-levels) setting instead of adding to them, a bug listed below.`;
	if (!first.known || !later.known) return `${opening}.`;
	if (first.value === later.value) {
		if (first.value === 0)
			return `${opening}. Its level ups give no [proficiency points](mechanic:proficiency-points)${passives ? '' : ', and it has no passives'}.`;
		return `${opening}, and each level up gives you ${points(first.value)}${passives ? ` to spend on ${unlocks === 'the passives below' ? 'them' : 'its passives'}` : ''}.`;
	}
	return `${opening}. Its first level up gives you ${points(first.value)} and later ones give ${later.value === 0 ? 'none' : later.value}${passives ? '' : first.value === 1 ? ', and it has no passives to spend it on' : ', and it has no passives to spend them on'}.`;
}
