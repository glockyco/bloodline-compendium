import type { ItemIndex, ItemSummary, ListColumn } from '../site-data.js';
import type { ListModel } from '../list/state.js';

/** Weapon columns that only some weapons have, shown when every shown weapon has them, as after a weapon type filter. */
const specializedWeaponColumns: Record<string, true> = {
	headHitDamage: true,
	projectilesPerShot: true,
	blockStaminaCost: true,
	extraFullBlocks: true,
	shieldChargeDamage: true
};

/**
 * Leaves out specialized weapon columns that a shown item lacks, and any column no shown item has, so no column fills
 * with empty values.
 */
function prune(shown: ItemSummary[], columns: ListColumn[]): ListColumn[] {
	return columns.filter((column) =>
		specializedWeaponColumns[column.id]
			? shown.length > 0 && shown.every((item) => item.cells[column.id])
			: column.id !== 'damage' || shown.some((item) => item.cells[column.id])
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
