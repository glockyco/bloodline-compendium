import { formatNumber, plural } from '../format.js';
import type { ResourceSummary } from '../site-data.js';

/**
 * What a resource needs and gives, as its list card and its preview say it. Its loot counts different items, and a
 * harvest gives some of them, so the line says it can give them.
 */
export function resourceLine(resource: ResourceSummary): string {
	const gives = plural(resource.loot, 'different item', 'different items');
	return resource.level
		? `Needs ${resource.profession} level ${formatNumber(resource.level)}, can give ${gives}`
		: `Can give ${gives}`;
}
