import { error } from '@sveltejs/kit';
import { readIndex, readKinds, readProfession, readProfessions } from './data.js';
import { kindPath } from '../entities.js';
import { listUrl } from '../items/search-lists.js';
import type { EntityReference, ProfessionRow } from '../site-data.js';

/**
 * The most recipes one page of a profession shows, so each page stays within its payload budget. A longer profession
 * splits its levels over pages.
 */
const rowsPerPage = 200;

/** A range of levels that a profession's page shows, with its path segment, or null for the first page. */
export interface LevelBand {
	min: number;
	max: number;
	segment: string | null;
}

/**
 * The bands of whole levels a long profession splits into, each as many levels as fit one page, such as levels 1 to
 * 8. A short profession is one band.
 */
export function levelBands(rows: ProfessionRow[]): LevelBand[] {
	const levels = rows.map((row) => row.level).filter((level) => level != null);
	if (rows.length <= rowsPerPage || !levels.length) return [];
	const found: { min: number; max: number; count: number }[] = [];
	for (const level of [...new Set(levels)].sort((a, b) => a - b)) {
		const count = levels.filter((value) => value === level).length;
		const last = found.at(-1);
		if (last && last.count + count <= rowsPerPage) {
			last.max = level;
			last.count += count;
		} else found.push({ min: level, max: level, count });
	}
	return found.map((band, index) => ({
		min: band.min,
		max: band.max,
		segment: index === 0 ? null : `levels-${band.min}-${band.max}`
	}));
}

/** The pages each profession has beyond its first, for prerendering. */
export async function professionBandEntries() {
	const { professions } = await readProfessions();
	const entries = await Promise.all(
		professions.map(async ({ slug }) =>
			levelBands((await readProfession(slug)).rows)
				.filter((band) => band.segment != null)
				.map((band) => ({ slug, band: band.segment! }))
		)
	);
	return entries.flat();
}

/** The part of a reference a link with its icon reads, so a profession with hundreds of recipes stays light. */
const link = ({ key, name, slug, icon, fragment }: EntityReference) => ({
	key,
	name,
	slug,
	icon,
	fragment: fragment ?? null
});

/** What a profession's page shows: its facts and the rows of its band of levels, or every row without bands. */
export async function professionPage(slug: string, segment: string | null) {
	if (!(await readProfessions()).professions.some((profession) => profession.slug === slug))
		error(404, 'Profession not found');
	const [{ rows, ...profession }, index, kinds] = await Promise.all([
		readProfession(slug),
		readIndex(),
		readKinds()
	]);
	const all = levelBands(rows);
	const band = all.find((entry) => entry.segment === segment) ?? (segment ? null : all[0]);
	if (segment && !band) error(404, 'Levels not found');
	const types = new Map(index.items.map((item) => [item.key, item.type]));
	const shown = band
		? rows.filter((row) => row.level != null && row.level >= band.min && row.level <= band.max)
		: rows;
	const levels = rows.map((row) => row.level).filter((level) => level != null);
	return {
		profession: {
			...profession,
			farming: rows.every((row) => row.kind === 'farming'),
			minLevel: levels.length ? Math.min(...levels) : null,
			maxLevel: levels.length ? Math.max(...levels) : null,
			icon: rows.find((row) => row.product.icon)?.product.icon ?? null
		},
		bands: all,
		band: band ?? null,
		// Rows come in the producer's order, by level and then name, with what a reader decides on: what each
		// needs, what it gives and the type that tells a blade from a guard.
		rows: shown.map((row) => ({
			level: row.level,
			type: types.get(row.product.key ?? '') ?? null,
			product: link(row.product),
			output: row.output,
			experience: row.experience,
			inputs: row.inputs.map((input) => ({ item: link(input.item), quantity: input.quantity }))
		})),
		// An item made at two levels shows in both, but counts once, as the profession list counts it.
		total: new Set(rows.map((row) => row.product.key + (row.product.fragment ?? ''))).size,
		all: listUrl(
			index,
			kindPath('item', kinds)!,
			{ profession: [profession.name] },
			{
				sort: { column: 'professionLevel', direction: 'asc' }
			}
		)
	};
}
