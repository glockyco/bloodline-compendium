import { expect, it } from 'vitest';
import { formatNumber, formatChance } from './format.js';

it('removes floating-point noise from pool weights and totals', () => {
	expect(formatNumber(18.020000000000028)).toBe('18.02');
	expect(formatNumber(1)).toBe('1');
	expect(formatNumber(1234.56789)).toBe('1,234.568');
});

it('formats probabilities as percentages with at most three decimals', () => {
	expect(formatChance(0.123456789)).toBe('12.346%');
	expect(formatChance(0.25)).toBe('25%');
	expect(formatChance(0)).toBe('0%');
	expect(formatChance(1)).toBe('100%');
});
