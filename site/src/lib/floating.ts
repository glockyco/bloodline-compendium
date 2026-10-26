import {
	autoUpdate,
	computePosition,
	flip,
	offset,
	shift,
	size,
	type ReferenceElement
} from '@floating-ui/dom';

/**
 * Places `panel` right of `anchor`, or left where the right lacks room, so it never covers the rows above or below
 * the anchor, and shifts it vertically to stay in the viewport. It follows scrolling, resizing and content changes
 * until the returned function stops it. Narrow screens dock the panel at the bottom instead, through its CSS.
 */
export function placeBeside(anchor: ReferenceElement, panel: HTMLElement): () => void {
	let active = true;
	const stop = autoUpdate(anchor, panel, () => {
		// Clearing the height that `size` wrote lets `flip` measure the panel's natural height.
		panel.style.maxHeight = '';
		void computePosition(anchor, panel, {
			placement: 'right-start',
			strategy: 'fixed',
			middleware: [
				offset(10),
				flip({ padding: 12, crossAxis: false, fallbackPlacements: ['left-start'] }),
				shift({ padding: 12, mainAxis: true, crossAxis: false }),
				size({
					padding: 12,
					apply({ availableHeight, elements }) {
						elements.floating.style.maxHeight = `${Math.max(0, availableHeight)}px`;
					}
				})
			]
		}).then(({ x, y }) => {
			if (!active) return;
			panel.style.left = `${x}px`;
			panel.style.top = `${y}px`;
			panel.style.visibility = 'visible';
		});
	});
	return () => {
		active = false;
		stop();
	};
}

/**
 * The visible part of a link: the line under the pointer when its name wraps, or its first line, so the panel goes
 * beside the words the reader points at instead of beside the whole paragraph the link spans.
 */
export function linkLine(
	link: HTMLElement,
	pointer: () => { x: number; y: number } | null
): ReferenceElement {
	return {
		contextElement: link,
		getBoundingClientRect() {
			const rects = [...link.getClientRects()].filter((rect) => rect.width > 0);
			if (!rects.length) return link.getBoundingClientRect();
			const at = pointer();
			const line =
				(at && rects.find((rect) => at.y >= rect.top && at.y <= rect.bottom)) ?? rects[0];
			return line;
		}
	};
}
