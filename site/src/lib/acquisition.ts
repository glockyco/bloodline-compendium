import type { Amount, Odds, OddsEvent, Price, Source, SourceKind } from './site-data.js';
import { formatChanceRange, formatNumber } from './format.js';
import { buyPrice, neutralMultiplier } from './rules/price.js';

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
	kill: 'per kill',
	delivery: 'per delivery',
	use: 'per use',
	catch: 'per catch',
	pick: 'per item gathered'
};

/** What a chance applies to, such as per container. A pick is one item that a gather or harvest yields. */
export function oddsLabel(odds: Odds, kind: SourceKind): string {
	if (odds.event === 'pick') return kind === 'farming' ? 'per item harvested' : 'per item gathered';
	return oddsLabels[odds.event];
}

export function sourceContext(source: Source): string | null {
	if (source.price) {
		const amount = payPrice(source.price);
		return amount === 0 ? 'Free' : pricePhrase(formatNumber(amount), source.price.currency);
	}
	if (source.odds?.chance != null) {
		return `${formatChanceRange(source.odds.chance, source.odds.highestChance ?? source.odds.chance)} ${oddsLabel(source.odds, source.kind)}`;
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
 * The words a first-screen summary leads with for each way of getting an item. They follow the headings of the
 * sections the summary links to, so "Dropped by" names a creature and "Found in" a container.
 */
export const sourceVerbs: Record<SourceKind, string> = {
	shop: 'Sold by',
	currencyShop: 'Sold by',
	stable: 'Sold by',
	smithing: 'Smithed',
	crafting: 'Crafted',
	cooking: 'Cooked',
	containerLoot: 'Found in',
	randomLoot: 'Found in',
	enemyDrop: 'Dropped by',
	pickup: 'Found on the ground in',
	dialogue: 'Given by',
	fishing: 'Caught by',
	gathering: 'Gathered from',
	farming: 'Grown from',
	bountyNote: 'Received when accepting the quest',
	settlementDelivery: 'Delivered by',
	contract: 'Given by',
	seasonalGift: 'Given by',
	archaeology: 'Dug up from',
	reward: 'Given by'
};

/** The ways of getting an item that a recipe line shows in full instead of a source table. */
export const recipeKinds: ReadonlySet<SourceKind> = new Set(['smithing', 'crafting', 'cooking']);

/** A name in a summary line with the place it stands in, or with no place when the next entry shares it. */
export interface SummaryEntry {
	name: string;
	place: string | null;
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
 * one condition with the range of levels, such as weaponsmithing level 10 to 40.
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

function placeName(source: Source): string | null {
	return source.places[0]?.name ?? (source.settlement ? 'your settlement' : null);
}

/**
 * The first two names, each with the place it stands in. Names that share one place name it once, and a name whose
 * place is unknown beside a located one says so. One name in several places names two of them.
 */
function pairedEntries(sources: Source[]): {
	entries: SummaryEntry[];
	joiner: ', ' | ' and ';
	more: number;
} {
	const names = [...new Set(sources.map((source) => source.name))];
	if (names.length === 1) {
		const places = [
			...new Set(sources.map(placeName).filter((place): place is string => place !== null))
		];
		return {
			entries: [{ name: names[0], place: places.length ? places.slice(0, 2).join(' and ') : null }],
			joiner: ', ',
			more: 0
		};
	}
	const shown = names.slice(0, 2).map((name) => ({
		name,
		place: placeName(sources.find((source) => source.name === name)!)
	}));
	const more = names.length - shown.length;
	if (shown.every((entry) => entry.place === shown[0].place)) {
		return {
			entries: shown.map((entry, index) => ({
				name: entry.name,
				place: index === shown.length - 1 ? entry.place : null
			})),
			joiner: ' and ',
			more
		};
	}
	return {
		entries: shown.map((entry) => ({ name: entry.name, place: entry.place ?? 'an unknown place' })),
		joiner: ', ',
		more
	};
}

export function summarize(kind: SourceKind, sources: Source[]): SourceSummary {
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
	if (kind === 'pickup') {
		// An item lying in a place no source records still says that the place is unknown.
		const places = [
			...new Set(sources.flatMap((source) => (source.places.length ? [source.places[0].name] : [])))
		].slice(0, 2);
		return {
			...base,
			verb: sourceVerbs[kind],
			entries: [],
			joiner: ', ',
			more: 0,
			places: places.length ? places : ['an unknown place']
		};
	}
	return { ...base, verb: sourceVerbs[kind], ...pairedEntries(sources), places: [] };
}

/** A summary line as plain text, for previews and search descriptions. */
export function summaryText(summary: SourceSummary): string {
	const entries = summary.entries.map((entry) =>
		entry.place ? `${entry.name} in ${entry.place}` : entry.name
	);
	const line = [
		summary.verb,
		entries.join(summary.joiner) +
			(summary.more
				? `${summary.joiner === ' and ' ? ',' : ''} and ${formatNumber(summary.more)} more`
				: ''),
		summary.places.join(' and ')
	]
		.filter(Boolean)
		.join(' ');
	return [line, summary.context].filter(Boolean).join(', ');
}

/** The first way to get an item as one sentence, for previews, or null when no way is known. */
export function firstRoute(sources: Source[], order: SourceKind[]): string | null {
	for (const kind of order) {
		const group = sortSources(sources.filter((source) => source.kind === kind));
		if (group.length) return summaryText(summarize(kind, group));
	}
	return null;
}
