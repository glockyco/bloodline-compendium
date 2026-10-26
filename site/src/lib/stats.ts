import type { Fact } from './site-data.js';
import { formatNumber } from './format.js';

/**
 * A fact's value as readers see it, or null when the game files do not determine it. Labels, tooltip labels
 * and units come from the catalog's stat vocabulary, and every number passes through formatNumber, so a
 * stat reads the same in tooltips, tables and descriptions.
 */
export function factValue(fact: Fact): string | null {
	if (fact.number !== null) return formatNumber(fact.number) + (fact.unit ? ' ' + fact.unit : '');
	if (fact.text !== null) return fact.text;
	if (fact.effect)
		return (
			fact.effect.name +
			' ' +
			formatNumber(fact.effect.power) +
			(fact.effect.seconds ? ' for ' + formatNumber(fact.effect.seconds) + ' s' : '')
		);
	return null;
}
