import type { ItemStatus, SkillPage } from '../site-data.js';

/** What a skill page says when no confirmed way to learn the skill is known. */
export const skillStatusSentence: Record<ItemStatus, string> = {
	obtainable: '',
	noKnownSource: 'No known way to learn this skill.',
	notInNormalPlay:
		'This skill cannot be learned in normal play. It only sits in skill trees the game no longer opens.'
};

/** The status sentence a skill page opens with, or null when its sources answer how to learn it. */
export function skillStatus(skill: SkillPage): string | null {
	return skill.sources.some((source) => !source.unconfirmed)
		? null
		: skillStatusSentence[skill.status];
}
