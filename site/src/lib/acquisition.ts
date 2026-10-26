import type { Amount, Odds, OddsEvent, Price, Source, SourceKind } from './site-data.js';
import { formatChance, formatNumber } from './format.js';
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
		return amount === 0
			? 'Free'
			: `${formatNumber(amount)} ${source.price.currency?.name ?? 'gold'}`;
	}
	if (source.odds?.chance != null) {
		return `${formatChance(source.odds.chance)} ${oddsLabel(source.odds, source.kind)}`;
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
	return [...sources].sort((a, b) => {
		if (comparablePrices && a.price && b.price) {
			const difference = payPrice(a.price) - payPrice(b.price);
			if (difference) return difference;
		}
		return a.name.localeCompare(b.name, 'en');
	});
}

export const useHeadings: Partial<Record<SourceKind, string>> = {
	crafting: 'Crafting',
	smithing: 'Smithing',
	cooking: 'Cooking',
	farming: 'Farming',
	currencyShop: 'Currency payments'
};

/** The verb a first-screen summary leads with for each way of getting an item. */
export const sourceVerbs: Record<SourceKind, string> = {
	shop: 'Buy from',
	currencyShop: 'Buy from',
	stable: 'Buy from',
	smithing: 'Smith',
	crafting: 'Craft',
	cooking: 'Cook',
	containerLoot: 'Loot from',
	randomLoot: 'Find in',
	enemyDrop: 'Loot from',
	pickup: 'Find',
	dialogue: 'Receive from',
	fishing: 'Catch by',
	gathering: 'Gather from',
	farming: 'Grow',
	bountyNote: 'Receive from',
	settlementDelivery: 'Receive from',
	contract: 'Receive from',
	seasonalGift: 'Receive from',
	archaeology: 'Dig up from',
	reward: 'Receive from'
};

/** The ways of getting an item that a recipe line shows in full instead of a source table. */
export const recipeKinds: ReadonlySet<SourceKind> = new Set(['smithing', 'crafting', 'cooking']);

/** One line of the first-screen summary for the sources of one kind. */
export interface SourceSummary {
	verb: string;
	names: string;
	more: number;
	places: string[];
	context: string | null;
	conditions: string[];
}

export function summarize(kind: SourceKind, sources: Source[]): SourceSummary {
	const first = sources[0];
	const recipe = recipeKinds.has(kind) ? first.recipe : null;
	// A pickup is named by where it lies, and random loot by the containers it can fill.
	const unnamed = kind === 'pickup' || kind === 'randomLoot';
	const names = [...new Set(sources.map((source) => source.name))];
	const shown = recipe || unnamed ? [] : names.slice(0, 2);
	const placed = recipe || kind === 'randomLoot' ? [] : unnamed ? sources : sources.slice(0, 2);
	const places = [
		...new Set(placed.flatMap((source) => source.places.slice(0, 1).map((place) => place.name)))
	].slice(0, 2);
	return {
		verb: recipe?.station
			? `${sourceVerbs[kind]} at the`
			: kind === 'pickup'
				? 'Find lying'
				: sourceVerbs[kind],
		names: recipe
			? (recipe.station?.name ?? first.name)
			: kind === 'randomLoot'
				? 'random container loot'
				: shown.join(', '),
		more: recipe ? sources.length - 1 : unnamed ? 0 : Math.max(0, names.length - shown.length),
		places,
		context: recipe ? null : sourceContext(first),
		conditions: [...new Set(first.conditions.map((condition) => condition.label))]
	};
}

/** The first way to get an item as one sentence, for previews, or null when no way is known. */
export function firstRoute(sources: Source[], order: SourceKind[]): string | null {
	for (const kind of order) {
		const group = sortSources(sources.filter((source) => source.kind === kind));
		if (!group.length) continue;
		const summary = summarize(kind, group);
		return [
			[
				summary.verb,
				summary.names,
				summary.more ? `and ${formatNumber(summary.more)} more` : '',
				summary.places.length ? `in ${summary.places.join(', ')}` : ''
			]
				.filter(Boolean)
				.join(' '),
			summary.context
		]
			.filter(Boolean)
			.join(', ');
	}
	return null;
}
