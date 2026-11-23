/**
 * Starts a page's side column level with the first card of its main column: the body of the main column's first
 * section, below that section's heading and any intro line, or the main column's first block when it opens without a
 * heading, such as an item's model. The two columns then read as one row however long the heading and intro are. The
 * side column is the layout's `[data-side]` child and the main column its `[data-main]` child. Below the width where
 * the columns stack, the side column keeps no offset. Without JavaScript, the layout's stylesheet offset applies.
 */
export function alignSide(layout: HTMLElement) {
	const update = () => {
		const side = layout.querySelector<HTMLElement>(':scope > [data-side]');
		// The main column is the first marked block with content, as an item's answer column can be empty.
		const main = [...layout.querySelectorAll<HTMLElement>(':scope > [data-main]')].find(
			(block) => block.firstElementChild
		);
		if (!side) return;
		// Columns stack when the side column starts at the main column's left edge.
		if (!main || side.offsetLeft <= main.offsetLeft) {
			side.style.marginTop = '0px';
			return;
		}
		const first = main.firstElementChild as HTMLElement;
		const card = first.matches('.section')
			? (first.querySelector<HTMLElement>(':scope > .body') ?? first)
			: first;
		// The side column's area starts at the layout's top, so the offset is measured from there.
		const offset = card.getBoundingClientRect().top - layout.getBoundingClientRect().top;
		side.style.marginTop = `${Math.max(0, offset)}px`;
	};
	const observer = new ResizeObserver(update);
	observer.observe(layout);
	update();
	return { destroy: () => observer.disconnect() };
}
