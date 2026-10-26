const numberFormat = new Intl.NumberFormat('en', { maximumFractionDigits: 3 });

export function formatNumber(value: number): string {
	return numberFormat.format(value);
}

const percentFormat = new Intl.NumberFormat('en', { maximumFractionDigits: 1 });
const rarityFormat = new Intl.NumberFormat('en', { maximumSignificantDigits: 2 });

/**
 * A chance as a percent with at most one decimal, or as about 1 in N below 1 percent, where a percent would
 * hide the difference between rare and very rare. A chance short of certain never reads as 100%.
 */
export function formatChance(value: number): string {
	if (value > 0 && value < 0.01) return `about 1 in ${rarityFormat.format(1 / value)}`;
	if (value < 1 && value > 0.999) return 'over 99.9%';
	return percentFormat.format(value * 100) + '%';
}
