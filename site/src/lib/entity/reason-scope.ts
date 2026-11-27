/**
 * Places each reason once at the scope it covers. A reason that every row of several carries goes in the line above
 * the table. Rows that then share the same reasons form a band under those reasons, after the rows that stand alone,
 * so a band runs to the next band or to the table's end. A row that stands alone keeps its reasons beside it.
 */
export function scopeReasons<T>(rows: T[], reasonsOf: (row: T) => readonly string[]) {
	const all = rows.map((row) => [...new Set(reasonsOf(row))]);
	const lines =
		rows.length > 1 ? all[0].filter((reason) => all.every((own) => own.includes(reason))) : [];
	const groups = new Map<string, { reasons: string[]; rows: T[] }>();
	rows.forEach((row, index) => {
		const reasons = all[index].filter((reason) => !lines.includes(reason));
		const key = JSON.stringify(reasons);
		const group = groups.get(key);
		if (group) group.rows.push(row);
		else groups.set(key, { reasons, rows: [row] });
	});
	const banded = (group: { reasons: string[]; rows: T[] }) =>
		group.reasons.length > 0 && group.rows.length > 1;
	return {
		lines,
		rows: [
			...[...groups.values()]
				.filter((group) => !banded(group))
				.flatMap((group) => group.rows.map((row) => ({ row, reasons: group.reasons, band: null }))),
			...[...groups.values()].filter(banded).flatMap((group) =>
				group.rows.map((row, index) => ({
					row,
					reasons: [] as string[],
					/** The reasons that head the band, on its first row. */
					band: index === 0 ? group.reasons : null
				}))
			)
		]
	};
}
