import { error } from '@sveltejs/kit';
import { readIndex, readKinds, readProfession, readProfessions } from '#lib/server/data.js';
import { kindPath } from '#lib/entities.js';
import { listUrl } from '#lib/items/search-lists.js';

/** The items a level shows before its link to the rest in the item list. */
const shownPerLevel = 12;

export async function entries() {
	return (await readProfessions()).professions.map((profession) => ({ slug: profession.slug }));
}

export async function load({ params }: { params: { slug: string } }) {
	if (!(await readProfessions()).professions.some((profession) => profession.slug === params.slug))
		error(404, 'Profession not found');
	const [{ rows, ...profession }, index, kinds] = await Promise.all([
		readProfession(params.slug),
		readIndex(),
		readKinds()
	]);
	const listPath = kindPath('item', kinds)!;
	const facets = { profession: [profession.name] };
	const sort = { column: 'professionLevel', direction: 'asc' as const };
	// Rows group by the level they need, each with its items as chips, and each level leads to the item list
	// filtered to it. The page carries only what a chip shows, so a profession with hundreds of recipes stays light.
	const levels = [...new Set(rows.map((row) => row.level))].sort(
		(a, b) => (a ?? Infinity) - (b ?? Infinity)
	);
	const groups = levels.map((level) => {
		const items = new Map(
			rows
				.filter((row) => row.level === level)
				.map((row) => [
					row.product.key + (row.product.fragment ?? ''),
					{
						key: row.product.key,
						name: row.product.name,
						slug: row.product.slug,
						icon: row.product.icon,
						fragment: row.product.fragment ?? null
					}
				])
		);
		const all = [...items.values()].sort((a, b) => a.name.localeCompare(b.name, 'en'));
		return {
			level,
			// A level shows its first items, and its link leads to the rest, so a level of forty stays one glance.
			// Items without a level have no list filter to lead to, so they all show.
			items: level == null ? all : all.slice(0, shownPerLevel),
			count: all.length,
			url:
				level == null
					? null
					: listUrl(index, listPath, facets, {
							stats: [{ column: 'professionLevel', min: String(level), max: String(level) }],
							sort
						})
		};
	});
	return {
		profession: { ...profession, farming: rows.every((row) => row.kind === 'farming') },
		groups,
		// An item made at two levels shows in both, but counts once, as the profession list counts it.
		total: new Set(rows.map((row) => row.product.key + (row.product.fragment ?? ''))).size,
		all: listUrl(index, listPath, facets, { sort })
	};
}
