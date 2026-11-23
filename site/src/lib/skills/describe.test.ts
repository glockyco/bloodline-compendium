import { describe, expect, it } from 'vitest';
import { disciplineIntro } from './describe.js';
import type { DisciplinePage, EntityReference } from '../site-data.js';

const reference = (name: string, slug: string): EntityReference => ({
	kind: 'discipline',
	key: `/Game/Invented/${slug}`,
	name,
	slug,
	icon: null,
	fragment: null
});

function discipline(
	name: string,
	levelName: string | null,
	{ skills = true, passives = true, checks = reference(name, name.toLowerCase()) } = {}
): Pick<DisciplinePage, 'name' | 'levelName' | 'skillRows' | 'passiveRows'> {
	return {
		name,
		levelName,
		skillRows: skills
			? [
					{
						level: 1,
						slots: [{ skill: reference('Bolt', 'bolt'), requiredLevel: 1, levelDiscipline: checks }]
					}
				]
			: [],
		passiveRows: passives
			? [
					{
						level: 3,
						passives: [
							{
								key: 'passive',
								anchor: 'passive-calm',
								name: 'Calm',
								description: null,
								requiredLevel: 3,
								levelDiscipline: checks,
								pointCost: 1,
								costIncrease: 1,
								ranks: 1,
								rankName: null,
								icon: null
							}
						]
					}
				]
			: []
	};
}

describe('discipline intro', () => {
	it('states the points each level up gives', () => {
		expect(disciplineIntro(discipline('Necromancy', 'Necromancy'))).toContain(
			'each level up gives you 2 [proficiency points](mechanic:proficiency-points) to spend on its passives'
		);
		expect(disciplineIntro(discipline('Daggers', 'Daggers'))).toContain(
			'gives you 1 [proficiency point](mechanic:proficiency-points)'
		);
	});

	it('names the Blood Levels defect in place of a number for Blood Magic', () => {
		const intro = disciplineIntro(discipline('Blood Magic', 'Blood Magic'));
		expect(intro).toContain(
			'[Blood Levels](mechanic:blood-levels) setting instead of adding to them'
		);
		expect(intro).not.toMatch(/gives you \d/);
	});

	it('tells a first level up apart from later ones, and says when no passive can use the points', () => {
		expect(disciplineIntro(discipline('Focuses', 'Staff', { passives: false }))).toContain(
			'Its first level up gives you 1 [proficiency point](mechanic:proficiency-points) and later ones give none, and it has no passives to spend it on.'
		);
		expect(disciplineIntro(discipline('Vampirism', 'Vampirism', { passives: false }))).toContain(
			'give no [proficiency points](mechanic:proficiency-points), and it has no passives.'
		);
	});

	it('says a tree without skills unlocks its passives', () => {
		expect(disciplineIntro(discipline('Lute', 'Lute', { skills: false }))).toContain(
			'Its level unlocks the passives below, and each level up gives you 1 [proficiency point](mechanic:proficiency-points) to spend on them.'
		);
	});

	it('names the discipline whose level a tree without its own level checks', () => {
		expect(
			disciplineIntro(
				discipline('Utility Gadgets', null, {
					passives: false,
					checks: reference('Spears', 'spears')
				})
			)
		).toBe(
			'Utility Gadgets has no level of its own. Its skills unlock at [Spears](discipline:spears) level 1, and every discipline starts at level 1.'
		);
	});
});
