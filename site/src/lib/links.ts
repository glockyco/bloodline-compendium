import type { EntityReference } from './site-data.js';

/** A Markdown link to a typed target, as the catalog's LinkedText reads it. */
const link = /\[([^\]]+)\]\(([a-zA-Z]+:[a-z0-9-]+)\)/g;

/** A run of text, with the entity it links to when it is a link. */
export interface LinkPart {
	text: string;
	entity: EntityReference | null;
}

/** The text as runs, each link resolved through the catalog's link targets. An unknown target throws. */
export function linkParts(text: string, links: Record<string, EntityReference>): LinkPart[] {
	const parts: LinkPart[] = [];
	let last = 0;
	for (const match of text.matchAll(link)) {
		if (match.index > last) parts.push({ text: text.slice(last, match.index), entity: null });
		const entity = links[match[2]];
		// The catalog lists every target text may use, so a missing one is a typo the build must not hide.
		if (!entity)
			throw new Error(`Text links to ${match[2]}, which the catalog does not list: ${text}`);
		parts.push({ text: match[1], entity });
		last = match.index + match[0].length;
	}
	if (last < text.length) parts.push({ text: text.slice(last), entity: null });
	return parts;
}

/** The text without its link marks, for places that show plain text, such as a meta description. */
export function plainText(text: string): string {
	return text.replace(link, '$1');
}
