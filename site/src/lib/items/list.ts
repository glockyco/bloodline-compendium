import type { ItemIndex, ItemSummary, ListColumn } from '../site-data.js';
import type { ListModel } from '../list/state.js';

/** Weapon columns that only some weapons have, shown when a shown weapon has them. */
const specializedWeaponColumns: Record<string, true> = {
	bodyHitDamage: true,
	headHitDamage: true,
	projectilesPerShot: true,
	blockStaminaCost: true,
	extraFullBlocks: true,
	shieldChargeDamage: true
};

/**
 * Leaves out specialized weapon columns that no shown item has, and melee damage when every shown weapon is ranged
 * or a shield, whose damage the other columns state.
 */
function prune(shown: ItemSummary[], columns: ListColumn[]): ListColumn[] {
	const weapons = shown.filter((item) => item.category === 'weapon');
	const noMeleeDamage =
		weapons.length > 0 &&
		weapons.every(
			(item) => !item.cells.damage && (item.cells.bodyHitDamage || item.cells.blockStaminaCost)
		);
	return columns.filter(
		(column) =>
			(!specializedWeaponColumns[column.id] || shown.some((item) => item.cells[column.id])) &&
			(column.id !== 'damage' || !noMeleeDamage)
	);
}

/** The item list's model: its filters and columns, with type and sell value as fixed columns. */
export function itemListModel(index: ItemIndex): ListModel<ItemSummary> {
	return {
		facets: index.facets,
		columns: index.columns,
		fixed: [
			{ id: 'type', numeric: false },
			{ id: 'value', numeric: true }
		],
		prune
	};
}
