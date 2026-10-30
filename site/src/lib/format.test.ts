import { expect, it } from 'vitest';
import { formatNumber, formatChance, formatChanceRange } from './format.js';

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
