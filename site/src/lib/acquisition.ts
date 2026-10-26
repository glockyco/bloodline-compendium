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
