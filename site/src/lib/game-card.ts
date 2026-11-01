import type { Fact, ItemPage } from './site-data.js';
import { titleCase } from './labels.js';
import { factValue } from './stats.js';

export interface CardLine {
	fact: Fact;
	text: string;
}

/** The card pairs weapon speed with applied damage, or armor class with armor. */
export function cardLines(facts: Fact[]): { first: CardLine | null; second: CardLine | null } {
	const find = (stat: string) => facts.find((fact) => fact.stat === stat);
	const speed = find('speed');
	const bodyHitDamage = find('bodyHitDamage');
	const damage = bodyHitDamage ?? find('damage');
	const type = find('armorType');
	const armor = find('armorRating');
	if (speed || damage)
		return {
			first: speed ? { fact: speed, text: speed.text ?? 'Unknown' } : null,
			second: damage
				? {
						fact: damage,
						text: `Damage: ${factValue(damage) ?? 'Unknown'}`
					}
				: null
		};
	if (type || armor)
		return {
			first: type && type.text !== 'Unarmored' ? { fact: type, text: `Type: ${type.text}` } : null,
			second: armor ? { fact: armor, text: `ARMOR: ${factValue(armor) ?? 'Unknown'}` } : null
		};
	return { first: null, second: null };
}

export interface DetailRow {
	key: string;
	label: string;
	values: string[];
	prose: boolean;
}

const effectStats = new Set(['effect', 'effects']);

/**
 * Ranged projectile groups supply their damage table instead of repeating the same facts in rows.
 * A food's or potion's effects share one Effects row where the first of them stands, because the numbers of the game's effect slots mean nothing to a
 * player. Its restoration amount, which the item list's Heals column reads, is already the amount of a Restore
 * Health or Restore Mana effect.
 */
export function detailRows(
	facts: Fact[],
	projectileGroups?: ItemPage['projectileGroups']
): DetailRow[] {
	const { first, second } = cardLines(facts);
	const shown = facts.filter(
		(fact) =>
			fact !== first?.fact &&
			(fact !== second?.fact || fact.stat === 'bodyHitDamage') &&
			fact.stat !== 'restorationAmount' &&
			(!projectileGroups?.length ||
				!['bodyHitDamage', 'headHitDamage', 'projectilesPerShot', 'chargedBodyHitDamage'].includes(
					fact.stat
				))
	);
	const effects = shown.filter((fact) => effectStats.has(fact.stat));
	const values = effects.map((fact) => factValue(fact) ?? 'Unknown');
	return shown.flatMap((fact) =>
		!effectStats.has(fact.stat)
			? [
					{
						key: fact.stat + fact.label,
						label: titleCase(fact.label),
						values: [
							fact.stat === 'blockStaminaCost' && fact.number === 50
								? 'cost half their damage in stamina'
								: (factValue(fact) ?? 'Unknown')
						],
						prose: fact.number == null || fact.stat === 'blockStaminaCost'
					}
				]
			: fact === effects[0]
				? [{ key: 'effects', label: 'Effects', values, prose: true }]
				: []
	);
}
