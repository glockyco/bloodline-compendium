const numberFormat = new Intl.NumberFormat('en', { maximumFractionDigits: 3 });

export function formatNumber(value: number): string {
	return numberFormat.format(value);
}

/** A count with its noun in the right number, such as "1 skill" or "12 skills". */
/**
 * A unit as it follows a value, as the game writes "4 seconds": after a space, in the singular for exactly 1, and
 * straight after the number for a unit that starts with a percent sign, as in "50% of incoming damage in stamina".
 */
export function unitAfter(value: number | null | undefined, unit: string): string {
	const word = value === 1 ? unit.replace(/s$/, '') : unit;
	return unit.startsWith('%') ? word : ' ' + word;
}

export function plural(count: number, one: string, many: string): string {
	return `${formatNumber(count)} ${count === 1 ? one : many}`;
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

const smallPercentFormat = new Intl.NumberFormat('en', { maximumSignificantDigits: 2 });

/**
 * A range of chances with both ends in one style: about 1 in N when both are below 1 percent, and percents
 * otherwise, where a low end below 1 percent keeps two significant digits.
 */
export function formatChanceRange(min: number, max: number): string {
	if (min === max) return formatChance(min);
	if (max < 0.01 && min > 0)
		return `about 1 in ${rarityFormat.format(1 / min)} to 1 in ${rarityFormat.format(1 / max)}`;
	const low =
		min > 0 && min < 0.01 ? smallPercentFormat.format(min * 100) + '%' : formatChance(min);
	return `${low} to ${formatChance(max)}`;
}
