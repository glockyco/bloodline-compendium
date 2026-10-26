const numberFormat = new Intl.NumberFormat('en', { maximumFractionDigits: 3 });

export function formatNumber(value: number): string {
	return numberFormat.format(value);
}

export function formatChance(value: number): string {
	return formatNumber(value * 100) + '%';
}
