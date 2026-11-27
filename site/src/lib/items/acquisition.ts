import type { Amount, Odds, OddsEvent, Price, Source, SourceKind } from '../site-data.js';
import { formatChanceRange, formatNumber } from '../format.js';
import { buyPrice, neutralMultiplier } from '../rules/price.js';

/**
 * What the player pays. A trading price is the base value times the multiplier for the player's respect in the
 * merchant's city, which defaults to neutral respect.
 */
export function payPrice(price: Price, multiplier: number = neutralMultiplier): number {
	if (price.rule !== 'trading') return price.amount;
	const result = buyPrice.evaluate({ value: price.amount, multiplier });
	return result.known ? result.value : price.amount;
}

export function formatAmount(amount: Amount): string {
	return amount.min === amount.max
		? formatNumber(amount.min)
		: `${formatNumber(amount.min)} to ${formatNumber(amount.max)}`;
}

/**
 * A price in gold, or a count of the item a merchant takes as currency, such as 3 × Cinderpear. The sign keeps the
 * item's name as the game spells it, since the game gives no plural.
 */
export function pricePhrase(amount: string, currency: Price['currency']): string {
	return currency ? `${amount} × ${currency.name}` : `${amount} gold`;
}

const oddsLabels: Record<OddsEvent, string> = {
	container: 'per container',
	lootSpot: 'per spawn point',
	kill: 'per kill',
	delivery: 'per delivery',
	use: 'per use',
	catch: 'per catch',
	pick: 'per item gathered'
};

/** A chance's event as a column heading writes it, such as per Container: every word capitalized but per. */
export function headingCase(label: string): string {
	return label.replace(/\b(?!per\b)[a-z]/g, (letter) => letter.toUpperCase());
}

/** What a chance applies to, such as per container. A pick is one item that a gather or harvest yields. */
export function oddsLabel(odds: Odds, kind: SourceKind): string {
	if (odds.event === 'pick') return kind === 'farming' ? 'per item harvested' : 'per item gathered';
	return oddsLabels[odds.event];
}

/** Whether a source gives the item every time, which reads Always, as on the OSRS Wiki. A placed item does. */
export function alwaysThere(odds: Odds, kind: SourceKind): boolean {
	return kind === 'pickup' && odds.chance === 1 && (odds.highestChance ?? 1) === 1;
}

export function sourceContext(source: Source): string | null {
	if (source.price) {
		const amount = payPrice(source.price);
		return amount === 0 ? 'Free' : pricePhrase(formatNumber(amount), source.price.currency);
	}
	if (source.odds?.chance != null) {
		return alwaysThere(source.odds, source.kind)
			? 'Always'
			: `${formatChanceRange(source.odds.chance, source.odds.highestChance ?? source.odds.chance)} ${oddsLabel(source.odds, source.kind)}`;
	}
	return null;
}

export function sortSources(sources: Source[]): Source[] {
	const firstPrice = sources[0]?.price;
	const comparablePrices =
		firstPrice &&
		sources.every(
			(source) =>
				source.price &&
				source.price.currency?.slug === firstPrice.currency?.slug &&
				source.price.currency?.name === firstPrice.currency?.name
		);
	// The cheapest price leads when prices compare, and otherwise the best chance, so the likeliest source is first.
	return [...sources].sort((a, b) => {
		if (comparablePrices && a.price && b.price) {
			const difference = payPrice(a.price) - payPrice(b.price);
			if (difference) return difference;
		}
		const chance =
			(b.odds?.highestChance ?? b.odds?.chance ?? -1) -
			(a.odds?.highestChance ?? a.odds?.chance ?? -1);
		if (chance) return chance;
		return a.name.localeCompare(b.name, 'en');
	});
}

export const useHeadings: Partial<Record<SourceKind, string>> = {
	crafting: 'Crafting',
	smithing: 'Smithing',
	cooking: 'Cooking',
	farming: 'Farming'
};

/**
 * The words a sentence leads with for each way of getting an item: the section heading's words in sentence case,
 * so "Made by smithing" stands for the heading "Made by Smithing" and "Dropped by" names a creature.
 */
export const sourceVerbs: Record<SourceKind, string> = {
	shop: 'Sold by',
	currencyShop: 'Sold by',
	stable: 'Sold by',
	smithing: 'Made by smithing',
	crafting: 'Crafted',
	cooking: 'Cooked',
	containerLoot: 'Found in',
	randomLoot: 'Found in',
	enemyDrop: 'Dropped by',
	pickup: 'Picked up in',
	dialogue: 'Given in dialogue by',
	fishing: 'Fished in',
	gathering: 'Gathered from',
	farming: 'Grown from',
	bountyNote: 'Obtained when you accept the quest',
	settlementDelivery: 'Delivered by',
	contract: 'Given by',
	seasonalGift: 'Given by',
	archaeology: 'Dug up from',
	reward: 'Given by',
	skillTree: 'Unlocked in the tree of',
	skillBook: 'Learned from a skill book in',
	noteSheet: 'Learned from a note sheet in',
	event: 'Unlocked by'
};

/** The ways of getting an item that a recipe line shows in full instead of a source table. */
export const recipeKinds: ReadonlySet<SourceKind> = new Set(['smithing', 'crafting', 'cooking']);

/**
 * A name in a summary line with the place it stands in. A null place shares the next entry's place, or none when
 * `unknown` says that no place is known.
 */
export interface SummaryEntry {
	name: string;
	place: string | null;
	unknown?: boolean;
}

const irregularPlurals: Record<string, string> = {
	Deer: 'Deer',
	Sheep: 'Sheep',
	Fish: 'Fish',
	Elf: 'Elves',
	Wolf: 'Wolves',
	Dwarf: 'Dwarves',
	Mouse: 'Mice',
	Goose: 'Geese'
};

/** The plural of a creature's name, made on its head noun, such as Goblin Brutes and Spirits of the Forest. */
export function pluralName(name: string): string {
	const [head, ...rest] = name.split(' of ');
	const words = head.split(' ');
	const last = words.pop()!;
	const plural =
		irregularPlurals[last] ??
		(/man$/.test(last)
			? last.replace(/man$/, 'men')
			: /[^aeiou]y$/.test(last)
				? last.slice(0, -1) + 'ies'
				: /(s|x|z|ch|sh)$/.test(last)
					? last + 'es'
					: last + 's');
	return [[...words, plural].join(' '), ...rest].join(' of ');
}

/**
 * How a sentence names what gives the item. A person's name stands as it is. A container takes an article and a
 * creature the plural, as wikis write "dropped by goblins", unless its name is one creature's, such as The Copper
 * King. A settlement building is yours, and a merchant whose name several merchants share, such as Shopkeeper, is a
 * role and takes "the".
 */
export function giverPhrase(source: Source, shared: ReadonlySet<string> = new Set()): string {
	if (source.kind === 'containerLoot')
		return (/^[aeiou]/i.test(source.name) ? 'an ' : 'a ') + source.name;
	if (source.kind === 'enemyDrop')
		return source.name.startsWith('The ') ? source.name : pluralName(source.name);
	if (source.settlement) return `your settlement's ${source.name}`;
	if (shared.has(source.name)) return `the ${source.name}`;
	return source.name;
}

/** One line of the first-screen summary for the sources of one kind. */
export interface SourceSummary {
	verb: string;
	/** The named sources, each with its own place, joined by `joiner`. */
	entries: SummaryEntry[];
	joiner: ', ' | ' and ';
	more: number;
	/** Places named without a source, for items lying on the ground. */
	places: string[];
	context: string | null;
	conditions: string[];
}

/** A range of numbers as one value when they agree and as "from to" otherwise. */
function span(values: number[], format: (value: number) => string): string {
	const min = Math.min(...values);
	const max = Math.max(...values);
	return min === max ? format(min) : `${format(min)} to ${format(max)}`;
}

/**
 * The price or chance that holds for every summarized source: one value when they agree, a range in one currency or
 * per one event otherwise, and nothing when the sources cannot be compared.
 */
export function summaryContext(sources: Source[]): string | null {
	const priced = sources.filter((source) => source.price);
	if (priced.length === sources.length) {
		const currency = priced[0].price!.currency;
		if (!priced.every((source) => source.price!.currency?.key === currency?.key)) return null;
		const amounts = priced.map((source) => payPrice(source.price!));
		if (amounts.every((amount) => amount === 0)) return 'Free';
		return pricePhrase(span(amounts, formatNumber), currency);
	}
	const chances = sources.filter((source) => source.odds?.chance != null);
	if (chances.length !== sources.length) return null;
	const label = oddsLabel(chances[0].odds!, chances[0].kind);
	if (!chances.every((source) => oddsLabel(source.odds!, source.kind) === label)) return null;
	const minimum = Math.min(...chances.map((source) => source.odds!.chance!));
	const maximum = Math.max(
		...chances.map((source) => source.odds!.highestChance ?? source.odds!.chance!)
	);
	return `${formatChanceRange(minimum, maximum)} ${label}`;
}

/**
 * The conditions that hold for every summarized source. A level that every source requires of the same skill is
 * one condition with the range of levels, such as Weaponsmithing level 10 to 40.
 */
export function summaryConditions(sources: Source[]): string[] {
	const [first, ...rest] = sources;
	const shared = first.conditions
		.filter((condition) => condition.kind !== 'proficiencyLevel')
		.map((condition) => condition.label)
		.filter((label) => rest.every((source) => source.conditions.some((c) => c.label === label)));
	const levels = sources.map((source) =>
		source.conditions.filter((condition) => condition.kind === 'proficiencyLevel')
	);
	// The label names the skill around its level, so labels that differ only in the number require one skill.
	const template = (label: string) => label.replace(/\d+/, '#');
	for (const condition of levels[0]) {
		const matching = levels.map((own) =>
			own.find(
				(other) => template(other.label) === template(condition.label) && other.value != null
			)
		);
		if (condition.value == null || matching.some((match) => !match)) continue;
		const values = matching.map((match) => match!.value!);
		shared.push(template(condition.label).replace('#', span(values, String)));
	}
	return [...new Set(shared)];
}

/** Where a source stands. A settlement building's phrase already says it is yours, so it names no place. */
function placeName(source: Source): string | null {
	return source.settlement ? null : (source.places[0]?.name ?? null);
}

/** The ways that stand somewhere in the world, so a missing place is worth saying, unlike a recipe or a seed. */
export const placedKinds: ReadonlySet<SourceKind> = new Set([
	'shop',
	'currencyShop',
	'stable',
	'containerLoot',
	'enemyDrop',
	'dialogue',
	'gathering'
]);

/**
 * The first two givers, each with the place it stands in. Givers that share one place name it once, and a giver
 * whose place is unknown says so. One giver in several places names two of them.
 */
function pairedEntries(
	kind: SourceKind,
	sources: Source[],
	shared: ReadonlySet<string>
): {
	entries: SummaryEntry[];
	joiner: ', ' | ' and ';
	more: number;
} {
	// A settlement building and a merchant of the same name elsewhere are different givers, so phrases group them.
	const phrases = sources.map((source) => giverPhrase(source, shared));
	const names = [...new Set(phrases)];
	const of = (name: string) => sources.filter((_, index) => phrases[index] === name);
	const unknown = (group: Source[]) =>
		placedKinds.has(kind) && group.every((source) => !source.settlement && !source.places.length);
	if (names.length === 1) {
		const places = [
			...new Set(sources.map(placeName).filter((place): place is string => place !== null))
		];
		return {
			entries: [
				{
					name: names[0],
					place: places.length ? places.slice(0, 2).join(' and ') : null,
					unknown: unknown(sources)
				}
			],
			joiner: ', ',
			more: 0
		};
	}
	const shown = names.slice(0, 2).map((name) => {
		const group = of(name);
		return { name, place: placeName(group[0]), unknown: unknown(group) };
	});
	const more = names.length - shown.length;
	if (
		shown.every((entry) => entry.place === shown[0].place && entry.unknown === shown[0].unknown)
	) {
		return {
			entries: shown.map((entry, index) => ({
				name: entry.name,
				place: index === shown.length - 1 ? entry.place : null,
				unknown: index === shown.length - 1 && entry.unknown
			})),
			joiner: ' and ',
			more
		};
	}
	return { entries: shown, joiner: ', ', more };
}

/** The places of sources a summary names by place alone, such as pickups and fishing spots. */
function placesOf(sources: Source[]): string[] {
	return [
		...new Set(sources.flatMap((source) => (source.places.length ? [source.places[0].name] : [])))
	].slice(0, 2);
}

export function summarize(
	kind: SourceKind,
	sources: Source[],
	shared: ReadonlySet<string> = new Set()
): SourceSummary {
	const first = sources[0];
	const recipe = recipeKinds.has(kind) ? first.recipe : null;
	const base = {
		context: recipe ? null : summaryContext(sources),
		conditions: summaryConditions(sources)
	};
	if (recipe) {
		return {
			...base,
			verb: recipe.station ? `${sourceVerbs[kind]} at the` : sourceVerbs[kind],
			entries: [{ name: recipe.station?.name ?? first.name, place: null }],
			joiner: ', ',
			more: sources.length - 1,
			places: []
		};
	}
	if (kind === 'randomLoot') {
		return {
			...base,
			verb: sourceVerbs[kind],
			entries: [{ name: 'random container loot', place: null }],
			joiner: ', ',
			more: 0,
			places: []
		};
	}
	if (kind === 'pickup' || kind === 'fishing') {
		// A pickup or a fishing spot is its place, so the line names places alone, and says when none is known.
		const places = placesOf(sources);
		return {
			...base,
			verb: places.length ? sourceVerbs[kind] : sourceVerbs[kind].replace(/ in$/, ''),
			entries: places.length ? [] : [{ name: '', place: null, unknown: true }],
			joiner: ', ',
			more: 0,
			places
		};
	}
	return { ...base, verb: sourceVerbs[kind], ...pairedEntries(kind, sources, shared), places: [] };
}

/** An entry as a sentence writes it, with its place or that the place is unknown. */
export function entryText(entry: SummaryEntry): string {
	if (entry.place) return `${entry.name} in ${entry.place}`;
	return entry.unknown ? `${entry.name}, location unknown` : entry.name;
}

/** A summary line as plain text, for previews and search descriptions. */
export function summaryText(summary: SourceSummary): string {
	const entries = summary.entries.map(entryText);
	const line = [
		summary.verb,
		entries.join(summary.joiner) +
			(summary.more
				? `${summary.joiner === ' and ' ? ',' : ''} and ${formatNumber(summary.more)} more`
				: ''),
		summary.places.join(' and ')
	]
		.filter(Boolean)
		.join(' ')
		.replace(' ,', ',');
	return [line, summary.context].filter(Boolean).join(', ');
}

/** The first way to get an item as one sentence, for previews, or null when no way is known. */
export function firstRoute(
	sources: Source[],
	order: SourceKind[],
	shared: ReadonlySet<string> = new Set()
): string | null {
	for (const kind of order) {
		const group = sortSources(
			sources.filter((source) => !source.unconfirmed && source.kind === kind)
		);
		if (group.length) return summaryText(summarize(kind, group, shared));
	}
	return null;
}
