import { formatNumber } from '../format.js';
import { missingInputs, type Rule } from './rule.js';

const f = Math.fround;

/**
 * The respect bands of Window_Trading_Shop:Calculate Discount Based On Respect, each with the multiplier it sets.
 * Respect outside -9999 to 9999 matches no band.
 */
export const respectBands = [
	{ min: -9999, max: -500, multiplier: 1.5 },
	{ min: -499, max: -250, multiplier: 1.44 },
	{ min: -249, max: -1, multiplier: 1.42 },
	{ min: 0, max: 249, multiplier: 1.4 },
	{ min: 250, max: 499, multiplier: 1.3 },
	{ min: 500, max: 749, multiplier: 1.2 },
	{ min: 750, max: 9999, multiplier: 1.0 }
] as const;

/** The multiplier that merchants use outside the cities that keep respect. */
export const neutralMultiplier = 1.4;

export function respectMultiplier(respect: number): number | undefined {
	return respectBands.find((band) => respect >= band.min && respect <= band.max)?.multiplier;
}

/** The multiplier a city's respect sets, as the game stores it in a 32-bit float. */
export const respectDiscount: Rule<{ respect: number }, number> = {
	id: 'respect-discount',
	cites: 'Window_Trading_Shop:Calculate Discount Based On Respect',
	check: {
		class: 'Window_Trading_Shop_C',
		function: 'Calculate Discount Based On Respect',
		inputs: { respect: 'Respect' },
		output: 'BuyMultiplier'
	},
	evaluate(inputs) {
		const missing = missingInputs(inputs, ['respect']);
		if (missing.length) return { known: false, missing };
		const respect = inputs.respect as number;
		const band = respectBands.find((entry) => respect >= entry.min && respect <= entry.max);
		if (!band) return { known: false, missing: ['respect between -9999 and 9999'] };
		return {
			known: true,
			value: f(band.multiplier),
			steps: [
				`Respect ${formatNumber(respect)} lies between ${formatNumber(band.min)} and ${formatNumber(band.max)}, so the multiplier is ${formatNumber(band.multiplier)}`
			]
		};
	}
};

/** What a merchant charges: the base value times the multiplier, in 32-bit floats, truncated. */
export const buyPrice: Rule<{ value: number; multiplier: number }, number> = {
	id: 'buy-price',
	cites: 'Window_Trading_Shop:GetBuyValueWithMultiplier',
	check: {
		class: 'Window_Trading_Shop_C',
		function: 'GetBuyValueWithMultiplier',
		inputs: { value: 'Value', multiplier: 'BuyMultiplier' },
		output: 'BuyValue'
	},
	evaluate(inputs) {
		const missing = missingInputs(inputs, ['value', 'multiplier']);
		if (missing.length) return { known: false, missing };
		const value = inputs.value as number;
		const product = f(value * f(inputs.multiplier as number));
		const price = Math.trunc(product);
		return {
			known: true,
			value: price,
			steps: [
				`${formatNumber(value)} × ${formatNumber(inputs.multiplier as number)} = ${formatNumber(product)}`,
				`without its fraction, ${formatNumber(price)} gold`
			]
		};
	}
};

/** What a merchant pays: the base value times 1.5 minus the multiplier, truncated, and at least 1. */
export const sellPrice: Rule<{ value: number; multiplier: number }, number> = {
	id: 'sell-price',
	cites: 'Window_Trading_Shop:GetSellValueWithMultiplier',
	check: {
		class: 'Window_Trading_Shop_C',
		function: 'GetSellValueWithMultiplier',
		inputs: { value: 'Value', multiplier: 'BuyMultiplier' },
		output: 'SellValue'
	},
	evaluate(inputs) {
		const missing = missingInputs(inputs, ['value', 'multiplier']);
		if (missing.length) return { known: false, missing };
		const value = inputs.value as number;
		const factor = f(1.5 - f(inputs.multiplier as number));
		const product = f(value * factor);
		const price = Math.max(Math.trunc(product), 1);
		return {
			known: true,
			value: price,
			steps: [
				`1.5 − ${formatNumber(inputs.multiplier as number)} = ${formatNumber(factor)}`,
				`${formatNumber(value)} × ${formatNumber(factor)} = ${formatNumber(product)}`,
				`without its fraction and at least 1, ${formatNumber(price)} gold`
			]
		};
	}
};
