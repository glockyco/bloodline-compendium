import { expect, it } from 'vitest';
import {
	formatNumber,
	formatChance,
	formatChanceRange,
	formatOneIn,
	formatPercentRange,
	unitAfter
} from './format.js';

it('removes floating-point noise from pool weights and totals', () => {
	expect(formatNumber(18.020000000000028)).toBe('18.02');
	expect(formatNumber(1)).toBe('1');
	expect(formatNumber(1234.56789)).toBe('1,234.568');
});

it('formats probabilities as percentages with at most three decimals', () => {
	expect(formatChance(0.123456789)).toBe('12.3%');
	expect(formatChance(0.25)).toBe('25%');
	expect(formatChance(0)).toBe('0%');
	expect(formatChance(1)).toBe('100%');
	expect(formatChance(0.9996)).toBe('over 99.9%');
	expect(formatChance(0.01)).toBe('1%');
	expect(formatChance(0.007934)).toBe('about 1 in 130');
	expect(formatChance(0.0004)).toBe('about 1 in 2,500');
});

it('writes both ends of a chance range in one style', () => {
	expect(formatChanceRange(0.0055, 0.385)).toBe('0.55% to 38.5%');
	expect(formatChanceRange(0.002, 0.005)).toBe('about 1 in 500 to 1 in 200');
	expect(formatChanceRange(0.25, 0.25)).toBe('25%');
});

it('writes every chance of a column as a percent and reads it as about 1 in N', () => {
	expect(formatPercentRange(0.0036, 0.0059)).toBe('0.36% to 0.59%');
	expect(formatPercentRange(0.0072, 0.1)).toBe('0.72% to 10%');
	expect(formatPercentRange(0.011, 0.011)).toBe('1.1%');
	expect(formatOneIn(0.0036, 0.0059)).toBe('about 1 in 280 to 1 in 170');
	expect(formatOneIn(0.091, 0.091)).toBe('about 1 in 11');
	expect(formatOneIn(0.25, 1)).toBe('about 1 in 4 to always');
	expect(formatOneIn(1, 1)).toBeNull();
	expect(formatOneIn(0, 0)).toBeNull();
});

it('writes a unit after its value as the game does', () => {
	expect(5 + unitAfter(5, 'seconds')).toBe('5 seconds');
	expect(1 + unitAfter(1, 'seconds')).toBe('1 second');
	expect(50 + unitAfter(50, '% of incoming damage in stamina')).toBe(
		'50% of incoming damage in stamina'
	);
	expect(20 + unitAfter(20, 'per contact')).toBe('20 per contact');
});
