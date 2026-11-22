import { attributes, measuredLevels, type Attribute } from '../rules/attributes.js';
import { rankCosts } from '../rules/progression.js';
import type { PlannerData } from '../site-data.js';
import { evaluate, levelOf } from './evaluate.js';

/** The skill bar's slots: two rows of five, as the game lays them out. */
export const hotbarSlots = 10;

/**
 * One planned character, each choice keyed by catalog key so a link survives renames. Discipline levels default to 1
 * and passive ranks to 0, so the state stores only what differs.
 */
export interface PlannerState {
	/** The game build the inputs came from when the build was planned. */
	build: number;
	level: number;
	disciplines: Record<string, number>;
	attributes: Record<Attribute, number>;
	/** Skills unlocked in the trees, which cost a skill point each. */
	skills: string[];
	/** Skills a book, conversation or event taught, which cost no skill point. */
	learned: string[];
	passives: Record<string, number>;
	/** The Blood Magic proficiency points the character has, which its level does not set. */
	bloodMagicPoints: number | null;
	hotbar: (string | null)[];
}

/** A new character: level 1, every attribute and discipline at 1, nothing bought. */
export function newCharacter(data: PlannerData): PlannerState {
	return {
		build: data.build,
		level: 1,
		disciplines: {},
		attributes: Object.fromEntries(attributes.map((attribute) => [attribute, 1])) as Record<
			Attribute,
			number
		>,
		skills: [],
		learned: [],
		passives: {},
		bloodMagicPoints: null,
		hotbar: Array(hotbarSlots).fill(null)
	};
}

/** The outcome of an edit: the new state, or the unchanged state and why the game would refuse it. */
export type Edit =
	{ state: PlannerState; refused?: undefined } | { state: PlannerState; refused: string };

const refuse = (state: PlannerState, refused: string): Edit => ({ state, refused });

/** Sets a whole number at least 1, as the game's levels are. Lowering keeps choices that no longer fit. */
function positive(value: number): number {
	return Math.max(1, Math.floor(value));
}

export function setLevel(state: PlannerState, level: number): Edit {
	return { state: { ...state, level: positive(level) } };
}

export function setDisciplineLevel(state: PlannerState, discipline: string, level: number): Edit {
	return {
		state: { ...state, disciplines: { ...state.disciplines, [discipline]: positive(level) } }
	};
}

/**
 * Sets an attribute's points. The game starts every attribute at 1 and refuses to go lower, and its effects are known
 * only up to the measured level.
 */
export function setAttribute(state: PlannerState, attribute: Attribute, value: number): Edit {
	const level = Math.floor(value);
	if (level < measuredLevels.min)
		return refuse(state, `${attribute} starts at 1 and cannot go lower.`);
	if (level > measuredLevels.max)
		return refuse(state, `${attribute} effects are known up to ${measuredLevels.max}.`);
	return { state: { ...state, attributes: { ...state.attributes, [attribute]: level } } };
}

/**
 * Unlocks or refunds a tree skill. The game unlocks a skill only once its discipline reaches the slot's level and a
 * skill point is left, and refunds the point when the skill is clicked again.
 */
export function toggleSkill(state: PlannerState, data: PlannerData, skill: string): Edit {
	if (state.skills.includes(skill))
		return {
			state: {
				...state,
				skills: state.skills.filter((key) => key !== skill),
				hotbar: state.learned.includes(skill)
					? state.hotbar
					: state.hotbar.map((slot) => (slot === skill ? null : slot))
			}
		};
	const slot = data.disciplines
		.flatMap((discipline) => discipline.slots)
		.find((entry) => entry.skill === skill);
	if (!slot) return refuse(state, 'No skill tree holds this skill.');
	const checked = data.disciplines.find((discipline) => discipline.key === slot.levelDiscipline);
	if (levelOf(state, slot.levelDiscipline) < slot.requiredLevel)
		return refuse(state, `Needs ${checked?.name ?? 'its discipline'} level ${slot.requiredLevel}.`);
	if (evaluate(state, data).skillPoints.remaining < 1)
		return refuse(state, 'No skill point is left.');
	return { state: { ...state, skills: [...state.skills, skill] } };
}

/** Marks a skill a book, conversation or event teaches as known, or unmarks it. */
export function toggleLearned(state: PlannerState, data: PlannerData, skill: string): Edit {
	if (state.learned.includes(skill))
		return {
			state: {
				...state,
				learned: state.learned.filter((key) => key !== skill),
				hotbar: state.skills.includes(skill)
					? state.hotbar
					: state.hotbar.map((slot) => (slot === skill ? null : slot))
			}
		};
	if (!data.skills.some((entry) => entry.key === skill && entry.learned))
		return refuse(state, 'No book, conversation or event teaches this skill.');
	return { state: { ...state, learned: [...state.learned, skill] } };
}

/**
 * Buys the next rank of a passive. The game sells a rank only once the passive's discipline reaches its level, while
 * ranks remain and its discipline's points cover the rank's cost.
 */
export function buyRank(state: PlannerState, data: PlannerData, passive: string): Edit {
	const owner = data.disciplines.find((discipline) =>
		discipline.passives.some((entry) => entry.key === passive)
	);
	const entry = owner?.passives.find((candidate) => candidate.key === passive);
	if (!owner || !entry) return refuse(state, 'No skill tree holds this passive.');
	const ranks = state.passives[passive] ?? 0;
	const costs = rankCosts(entry);
	if (ranks >= costs.length)
		return refuse(
			state,
			entry.ranks === null ? 'Only its first rank is known.' : 'Every rank is bought.'
		);
	const checked = data.disciplines.find((discipline) => discipline.key === entry.levelDiscipline);
	if (levelOf(state, entry.levelDiscipline) < entry.requiredLevel)
		return refuse(
			state,
			`Needs ${checked?.name ?? 'its discipline'} level ${entry.requiredLevel}.`
		);
	const balance = evaluate(state, data).proficiency[owner.key];
	if (balance?.remaining !== null && balance !== undefined && balance.remaining < costs[ranks])
		return refuse(state, `Needs ${costs[ranks]} ${owner.name} points.`);
	return { state: { ...state, passives: { ...state.passives, [passive]: ranks + 1 } } };
}

/** Removes the last bought rank, a planning edit the game itself does not offer. */
export function removeRank(state: PlannerState, passive: string): Edit {
	const ranks = state.passives[passive] ?? 0;
	if (ranks < 1) return refuse(state, 'No rank is bought.');
	const passives = { ...state.passives, [passive]: ranks - 1 };
	if (passives[passive] === 0) delete passives[passive];
	return { state: { ...state, passives } };
}

export function setBloodMagicPoints(state: PlannerState, points: number | null): Edit {
	return {
		state: { ...state, bloodMagicPoints: points === null ? null : Math.max(0, Math.floor(points)) }
	};
}

/** Puts a known skill on a hotbar slot, or empties the slot with null. */
export function placeOnHotbar(state: PlannerState, slot: number, skill: string | null): Edit {
	if (!Number.isInteger(slot) || slot < 0 || slot >= hotbarSlots)
		return refuse(state, 'The skill bar has two rows of five slots.');
	if (skill !== null && !state.skills.includes(skill) && !state.learned.includes(skill))
		return refuse(state, 'Only an unlocked or learned skill goes on the skill bar.');
	const hotbar = state.hotbar.map((entry, index) =>
		index === slot ? skill : entry === skill && skill !== null ? null : entry
	);
	return { state: { ...state, hotbar } };
}
