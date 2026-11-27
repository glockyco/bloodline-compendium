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

const smallPercentFormat = new Intl.NumberFormat('en', { maximumSignificantDigits: 2 });

/**
 * A chance as a percent with at most one decimal, and with two significant digits below 1 percent, so that rare
 * and very rare still differ. A chance short of certain never reads as 100%.
 */
export function formatPercent(value: number): string {
	if (value > 0 && value < 0.01) return smallPercentFormat.format(value * 100) + '%';
	if (value < 1 && value > 0.999) return 'over 99.9%';
	return percentFormat.format(value * 100) + '%';
}

/** A range of chances as percents, which a column of chances shows. */
export function formatPercentRange(min: number, max: number): string {
	return min === max ? formatPercent(min) : `${formatPercent(min)} to ${formatPercent(max)}`;
}

const oneIn = (value: number) => (value >= 1 ? 'always' : `1 in ${rarityFormat.format(1 / value)}`);

/**
 * A range of chances as about 1 in N, which a column of percents shows as a hint. A chance of nothing or of
 * everything has no such reading.
 */
export function formatOneIn(min: number, max: number): string | null {
	if (min <= 0 || min >= 1) return null;
	return min === max ? `about ${oneIn(min)}` : `about ${oneIn(min)} to ${oneIn(max)}`;
}

/**
 * A chance in a sentence: a percent, or about 1 in N below 1 percent, where a percent is hard to picture.
 */
export function formatChance(value: number): string {
	return value > 0 && value < 0.01 ? `about ${oneIn(value)}` : formatPercent(value);
}

/**
 * A range of chances in a sentence, with both ends in one style: about 1 in N when both are below 1 percent, and
 * percents otherwise.
 */
export function formatChanceRange(min: number, max: number): string {
	if (min === max) return formatChance(min);
	if (max < 0.01 && min > 0) return `about ${oneIn(min)} to ${oneIn(max)}`;
	return formatPercentRange(min, max);
}
