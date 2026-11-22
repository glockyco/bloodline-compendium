import { formatNumber } from '../format.js';
import type { RuleResult } from './rule.js';

/** The nine attributes in the order the level up window lists them. */
export const attributes = [
	'Strength',
	'Intelligence',
	'Dexterity',
	'Endurance',
	'Fortitude',
	'Constitution',
	'Vitality',
	'Wisdom',
	'Willpower'
] as const;

export type Attribute = (typeof attributes)[number];

/**
 * The attribute levels lab runs measured. Every attribute starts at 1, a decrease at 1 does nothing, and runs
 * attributes2 and attributes4 raised each one to 21, so effects above 21 are unknown.
 */
export const measuredLevels = { min: 1, max: 21 };

/** What the level up window and the player show for a set of attribute levels. */
export interface AttributeEffects {
	health: number;
	stamina: number;
	mana: number;
	/** The regeneration rates the level up window's side panel shows. */
	healthRegen: number;
	staminaRegen: number;
	manaRegen: number;
	pickupStrength: number;
	/** Percentages, as the side panel shows them. */
	diseaseResistance: number;
	armorWeightReduction: number;
	strengthExpBonus: number;
	intelligenceExpBonus: number;
	dexterityExpBonus: number;
}

/**
 * Health, stamina and mana each point adds, from a new character's 150 health, 200 stamina and 100 mana. A point
 * adds the same at every level that runs passives1, attributes2 and attributes4 measured.
 */
export const pointGains: Record<Attribute, { health: number; stamina: number; mana: number }> = {
	Strength: { health: 0, stamina: 10, mana: 0 },
	Intelligence: { health: 0, stamina: 0, mana: 25 },
	Dexterity: { health: 0, stamina: 15, mana: 0 },
	Endurance: { health: 0, stamina: 30, mana: 0 },
	Fortitude: { health: 25, stamina: 0, mana: 0 },
	Constitution: { health: 15, stamina: 15, mana: 0 },
	Vitality: { health: 10, stamina: 0, mana: 0 },
	Wisdom: { health: 0, stamina: 0, mana: 10 },
	Willpower: { health: 0, stamina: 0, mana: 5 }
};

/**
 * The side panel's Armor Weight Reduction for Endurance levels 1 to 21, and the EXP bonus that Strength,
 * Intelligence and Dexterity each give their own, as runs attributes2 and attributes4 read them. The game computes both in the
 * window's own functions, which the code index does not reduce to a formula, so the planner shows them as measured.
 */
const armorWeightReduction = [
	1.96, 3.84, 5.65, 7.39, 9.06, 10.67, 12.21, 13.69, 15.12, 16.48, 17.8, 19.06, 20.27, 21.44, 22.56,
	23.64, 24.67, 25.66, 26.62, 27.53, 28.41
];
const expBonus = [
	2.4, 4.8, 7, 9.1, 11.1, 13, 14.8, 16.5, 18.1, 19.7, 21.2, 22.6, 23.9, 25.2, 26.4, 27.5, 28.6,
	29.7, 30.7, 31.6, 32.5
];

/** A regeneration modifier, which grows with the square root of the points above the first. */
const regen = (level: number, step: number) => step * Math.sqrt(level - 1);

/**
 * The effects of a set of attribute levels, as PlayerPassiveLevelStatComponent:Initialize Regeneration Stats and the
 * level up window's Get All Stats show them. A level outside the measured range makes the result unknown and names
 * the attribute.
 */
export function attributeEffects(
	levels: Partial<Record<Attribute, number>>
): RuleResult<AttributeEffects> {
	const missing = attributes.filter((attribute) => {
		const level = levels[attribute];
		return (
			level === undefined ||
			!Number.isInteger(level) ||
			level < measuredLevels.min ||
			level > measuredLevels.max
		);
	});
	if (missing.length) return { known: false, missing };
	const level = (attribute: Attribute) => levels[attribute]!;
	const gained = (resource: 'health' | 'stamina' | 'mana') =>
		attributes.reduce(
			(sum, attribute) => sum + pointGains[attribute][resource] * (level(attribute) - 1),
			0
		);
	const health = 150 + gained('health');
	const stamina = 200 + gained('stamina');
	const mana = 100 + gained('mana');
	const healthModifier = regen(level('Constitution'), 0.025) + regen(level('Vitality'), 0.045);
	const manaModifier = regen(level('Willpower'), 0.045) + regen(level('Wisdom'), 0.025);
	const value: AttributeEffects = {
		health,
		stamina,
		mana,
		healthRegen: 1 + healthModifier,
		staminaRegen: 10 + healthModifier,
		manaRegen: 1 + manaModifier,
		pickupStrength: 200 + 15 * level('Strength'),
		diseaseResistance: 1.5 * level('Fortitude'),
		armorWeightReduction: armorWeightReduction[level('Endurance') - 1],
		strengthExpBonus: expBonus[level('Strength') - 1],
		intelligenceExpBonus: expBonus[level('Intelligence') - 1],
		dexterityExpBonus: expBonus[level('Dexterity') - 1]
	};
	return {
		known: true,
		value,
		steps: [
			`Health: 150 plus each point's health, ${formatNumber(health)}`,
			`Stamina: 200 plus each point's stamina, ${formatNumber(stamina)}`,
			`Mana: 100 plus each point's mana, ${formatNumber(mana)}`,
			`Health regeneration: 1 plus 0.025 × √(Constitution − 1) plus 0.045 × √(Vitality − 1), ${value.healthRegen.toFixed(2)}`,
			`Mana regeneration: 1 plus 0.045 × √(Willpower − 1) plus 0.025 × √(Wisdom − 1), ${value.manaRegen.toFixed(2)}`,
			`Pickup Strength: 200 plus 15 per Strength level, ${formatNumber(value.pickupStrength)}`,
			`Disease Resistance: 1.5% per Fortitude level, ${formatNumber(value.diseaseResistance)}%`
		]
	};
}
