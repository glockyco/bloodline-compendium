import { describe, expect, it } from 'vitest';
import { balancedColumns } from './CardGrid.svelte';

describe('balancedColumns', () => {
	it('splits a small set evenly', () => {
		expect(balancedColumns(4)).toBe(2);
		expect(balancedColumns(6)).toBe(3);
		expect(balancedColumns(1)).toBe(1);
	});

	it('avoids a lone card on the last line', () => {
		expect(balancedColumns(7, 4)).toBe(4);
		expect(balancedColumns(5)).toBe(3);
	});

	it('keeps a long list dense', () => {
		expect(balancedColumns(28, 4)).toBe(4);
		expect(balancedColumns(26, 4)).toBe(3);
		expect(balancedColumns(30, 4)).toBe(3);
	});
});
