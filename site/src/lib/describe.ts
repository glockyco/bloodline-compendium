import { sortSources, summarize, summaryContext } from './acquisition.js';
import { formatNumber } from './format.js';
import { sourceHeadings } from './labels.js';
import type { ItemPage, ItemStatus, Source, SourceKind } from './site-data.js';

/** What a page says about an item without a known way to get it, on the page and in its description. */
export const statusSentence: Record<ItemStatus, string> = {
	obtainable: '',
	noKnownSource: 'No known source. It may come from a quest or event.',
	notInNormalPlay: 'This item cannot be obtained in normal play. It only appears in test areas.'
};

// Words ending in s that name one thing, such as Gladius, Lens and the title A Walk in the Woods.
const singular = new Set(['Lens', 'Molasses', 'Woods']);

/** Whether a name takes "are", as Leather Pants and Bandit's Daggers do: its head noun is a plural. */
export function isPlural(name: string): boolean {
	const head = name.split(' of ')[0].trim().split(' ').at(-1)!.replace(/[()]/g, '');
	return /[^s]s$/.test(head) && !/(us|is)$/.test(head) && !singular.has(head);
}

const armorSlots: Record<string, string> = {
	Head: 'head armor',
	Torso: 'torso armor',
	Arms: 'arm armor',
	Legs: 'leg armor',
	Feet: 'foot armor',
	Shoulders: 'shoulder armor',
	Knees: 'knee armor'
};
const weaponWords: Record<string, string> = {
	'One Handed Weapon': 'one-handed weapon',
	'Two Handed Weapon': 'two-handed weapon'
};
const categoryPhrases: Record<string, string> = {
	Material: 'a material',
	Food: 'a food item',
	Potion: 'a potion',
	'Quest Item': 'a quest item',
	Note: 'a note',
	'Trade Goods': 'a trade good',
	Currency: 'a currency'
};

function article(phrase: string): string {
	return /^[aeiou]/i.test(phrase) && !/^one\b/i.test(phrase) ? `an ${phrase}` : `a ${phrase}`;
}

/** What kind of item it is, as the noun phrase after "is" or "are". */
export function typePhrase(item: Pick<ItemPage, 'name' | 'type' | 'category' | 'facts'>): string {
	const category = item.category.name;
	if (category === 'Armor') {
		if (item.type === 'Amulet') return 'jewelry';
		if (item.type === 'Support') return 'a support item';
		const slot = armorSlots[item.type] ?? `${item.type.toLowerCase()} armor`;
		const armorClass = item.facts.find((fact) => fact.stat === 'armorType')?.text;
		return armorClass && ['Light', 'Medium', 'Heavy'].includes(armorClass)
			? `${armorClass.toLowerCase()} ${slot}`
			: slot;
	}
	if (category === 'Weapon') {
		const word = weaponWords[item.type] ?? item.type.toLowerCase();
		// A plural name is several weapons of the type: Bandit's Daggers are daggers.
		return isPlural(item.name) ? `${word}s` : article(word);
	}
	if (category === 'Weapon Part') return `${article(item.type.toLowerCase())} for smithing weapons`;
	return categoryPhrases[category] ?? 'an item';
}

function statSentence(
	item: Pick<ItemPage, 'name' | 'category' | 'facts' | 'projectileGroups'>
): string | null {
	const number = (stat: string) => item.facts.find((fact) => fact.stat === stat)?.number;
	const plural = isPlural(item.name);
	const body = number('bodyHitDamage');
	const groups = item.projectileGroups;
	if (item.category.name === 'Weapon' && (body != null || (groups && groups.length > 1))) {
		const sentences: string[] = [];
		if (groups && groups.length > 1) {
			const hits = groups.map((group) => ({
				condition: group.condition,
				text: `${formatNumber(group.count)} ${group.count === 1 ? 'projectile' : 'projectiles'} dealing ${group.bodyHitDamage == null ? 'unknown' : formatNumber(group.bodyHitDamage)} body hit damage and ${group.headHitDamage == null ? 'unknown' : formatNumber(group.headHitDamage)} head hit damage per projectile`
			}));
			const unconditional = hits.filter((hit) => !hit.condition).map((hit) => hit.text);
			if (unconditional.length)
				sentences.push(
					`Per shot, ${plural ? 'they fire' : 'it fires'} ${unconditional.join(' and ')}.`
				);
			for (const hit of hits) if (hit.condition) sentences.push(`${hit.condition}: ${hit.text}.`);
		} else if (body != null) {
			const hits = [`${formatNumber(body)} body hit damage`];
			const head = number('headHitDamage');
			if (head != null) hits.push(`${formatNumber(head)} head hit damage`);
			sentences.push(`${plural ? 'They deal' : 'It deals'} ${hits.join(' and ')} per projectile.`);
			const projectiles = number('projectilesPerShot');
			if (projectiles != null)
				sentences.push(
					`${plural ? 'They fire' : 'It fires'} ${formatNumber(projectiles)} ${projectiles === 1 ? 'projectile' : 'projectiles'} per shot.`
				);
		}
		const charged = number('chargedBodyHitDamage');
		if (charged != null)
			sentences.push(`Fully charged body hit damage is ${formatNumber(charged)} per projectile.`);
		return sentences.join(' ');
	}
	const damage = number('damage');
	if (item.category.name === 'Weapon' && damage)
		return `${plural ? 'They deal' : 'It deals'} ${formatNumber(damage)} damage.`;
	const armor = number('armorRating');
	if (item.category.name === 'Armor' && armor)
		return `${plural ? 'They add' : 'It adds'} ${formatNumber(armor)} armor.`;
	return null;
}

/** The first way the page lists, as its summary line names it, with the first seller's price. */
function firstWay(sources: Source[]): string | null {
	for (const kind of Object.keys(sourceHeadings) as SourceKind[]) {
		const group = sortSources(sources.filter((source) => source.kind === kind));
		if (!group.length) continue;
		const summary = summarize(kind, group);
		const [entry] = summary.entries;
		// A name without a place shares the place of the name after it.
		const place = entry?.place ?? summary.entries.find((other) => other.place)?.place;
		const where = entry
			? place
				? `${entry.name} in ${place}`
				: entry.name
			: summary.places.join(' and ');
		const price = group[0].price ? summaryContext([group[0]]) : null;
		return `${summary.verb} ${where}${price ? ` for ${price === 'Free' ? 'free' : price}` : ''}.`;
	}
	return null;
}

/**
 * The description search engines and chat apps show for an item: what it is, its damage or armor, and the first way
 * to get it, as wiki descriptions open. A qualifier names the item as its title does, so same-named items differ.
 */
export function describeItem(item: ItemPage, confirmedSources: Source[]): string {
	const name = item.name + (item.qualifier ? ` (${item.qualifier})` : '');
	return [
		`${name} ${isPlural(item.name) ? 'are' : 'is'} ${typePhrase(item)} in The Bloodline.`,
		statSentence(item),
		firstWay(confirmedSources) ?? statusSentence[item.status]
	]
		.filter(Boolean)
		.join(' ');
}
