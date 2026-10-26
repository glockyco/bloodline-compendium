import type { Amount, Source, SourceKind } from './site-data.js';
import { formatChance, formatNumber } from './format.js';

export function formatAmount(amount: Amount): string {
	return amount.min === amount.max
		? formatNumber(amount.min)
		: `${formatNumber(amount.min)} to ${formatNumber(amount.max)}`;
}

export function chanceLabel(source: Source): string {
	return source.chanceBasis ?? 'Chance basis unknown';
}

export function sourceContext(source: Source): string | null {
	if (source.price) {
		return `${formatNumber(source.price.amount)} ${source.price.currency?.name ?? 'gold'}`;
	}
	if (source.chance !== null && source.chanceBasis) {
		return `${formatChance(source.chance)} ${source.chanceBasis.toLowerCase()}`;
	}
	if (source.weight) {
		return `${formatNumber(source.weight.value)} of ${formatNumber(source.weight.total)} relative chance`;
	}
	if (source.amount && (source.amount.min !== 1 || source.amount.max !== 1)) {
		return `${formatAmount(source.amount)} items`;
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
			const difference = a.price.amount - b.price.amount;
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
