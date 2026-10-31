import type { Page } from '@playwright/test';

/**
 * Layout defects a reader can see, measured in the rendered page:
 * - the page scrolls sideways,
 * - content leaves a card or tooltip,
 * - the rows of cards that share a line do not start at the same height,
 * - visible text is smaller than step 0 of the type scale,
 * - a surface or table cell is padded off the shared inset,
 * - a numeric cell or heading in a column of a table is not right-aligned,
 * - a comma in site text has no space after it.
 */
export async function layoutProblems(page: Page): Promise<string[]> {
	// Search and other components render after load, and their text counts too.
	await page.waitForLoadState('networkidle');
	await page.evaluate(() => document.fonts.ready);
	return page.evaluate(() => {
		const problems: string[] = [];
		const tolerance = 1;
		const describe = (element: Element) =>
			element.tagName.toLowerCase() +
			(element.getAttribute('data-ui') ? `[data-ui=${element.getAttribute('data-ui')}]` : '') +
			(element.className && typeof element.className === 'string'
				? '.' + element.className.trim().split(/\s+/)[0]
				: '') +
			` "${(element.textContent ?? '').trim().slice(0, 40)}"`;
		const visible = (element: Element) => {
			const style = getComputedStyle(element);
			const box = element.getBoundingClientRect();
			return (
				style.visibility !== 'hidden' && style.display !== 'none' && box.width > 0 && box.height > 0
			);
		};

		if (document.documentElement.scrollWidth > window.innerWidth + tolerance)
			problems.push(
				`page scrolls sideways: ${document.documentElement.scrollWidth}px wide at ${window.innerWidth}px`
			);

		for (const container of document.querySelectorAll('[data-ui=card] > *, [data-ui=tooltip]')) {
			const style = getComputedStyle(container);
			const box = container.getBoundingClientRect();
			const left = box.left + parseFloat(style.paddingLeft) + parseFloat(style.borderLeftWidth);
			const right = box.right - parseFloat(style.paddingRight) - parseFloat(style.borderRightWidth);
			for (const child of container.querySelectorAll('*')) {
				if (!visible(child) || child.closest('.sr-only')) continue;
				const inner = child.getBoundingClientRect();
				if (inner.left < left - tolerance || inner.right > right + tolerance)
					problems.push(`${describe(child)} leaves ${describe(container)}`);
			}
		}

		// Cards on one line of a grid must start each shared row at the same height.
		for (const grid of new Set(
			[...document.querySelectorAll('[data-ui=card]')].map((card) => card.parentElement)
		)) {
			if (!grid) continue;
			const lines = new Map<number, Element[]>();
			for (const card of grid.querySelectorAll(':scope > [data-ui=card]')) {
				const top = Math.round(card.getBoundingClientRect().top);
				lines.set(top, [...(lines.get(top) ?? []), card]);
			}
			for (const cards of lines.values()) {
				if (cards.length < 2) continue;
				const rows = cards.map((card) => [...(card.firstElementChild?.children ?? [])]);
				const count = Math.max(...rows.map((row) => row.length));
				for (let index = 0; index < count; index++) {
					const tops = rows.map((row) =>
						row[index] ? Math.round(row[index].getBoundingClientRect().top) : null
					);
					const known = tops.filter((top): top is number => top !== null);
					if (Math.max(...known) - Math.min(...known) > tolerance)
						problems.push(
							`row ${index + 1} of cards on one line starts at ${known.join(', ')}px: ${cards.map(describe).join(' | ')}`
						);
				}
			}
		}

		// Every surface and every cell starts its content at the shared inset, so stacked sections share one edge.
		const root = getComputedStyle(document.documentElement);
		const inset = parseFloat(root.getPropertyValue('--inset')) * parseFloat(root.fontSize);
		for (const element of document.querySelectorAll(
			// A list surface passes its inset to its entries, so its dividers span the surface.
			'.surface:not(.acquisition-routes), .relation-panel td, .relation-panel th, .acquisition-routes a'
		)) {
			if (!visible(element)) continue;
			const padding = parseFloat(getComputedStyle(element).paddingLeft);
			if (Math.abs(padding - inset) > tolerance)
				problems.push(`${describe(element)} is inset ${padding}px instead of ${inset}px`);
		}
		// A phone shows some tables as rows of labelled values, where a number follows its label instead of a column.
		for (const cell of document.querySelectorAll('td.numeric, th.numeric')) {
			const row = cell.parentElement;
			if (!row || getComputedStyle(row).display !== 'table-row') continue;
			if (visible(cell) && getComputedStyle(cell).textAlign !== 'right')
				problems.push(`${describe(cell)} is a number that is not right-aligned`);
		}

		const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
		// Labels, counts and secondary lines use step 0, .875rem, as in Afallon, and nothing is smaller.
		const minimum = parseFloat(root.fontSize) * 0.875;
		for (let node = walker.nextNode(); node; node = walker.nextNode()) {
			const parent = node.parentElement;
			if (
				!parent ||
				!node.textContent?.trim() ||
				!visible(parent) ||
				parent.closest('.sr-only, script, style, noscript')
			)
				continue;
			const size = parseFloat(getComputedStyle(parent).fontSize);
			if (size < minimum - 0.01)
				problems.push(`${describe(parent)} is ${size}px, below ${minimum}px`);
			// Game text keeps its own punctuation.
			if (!parent.closest('[data-game]') && /,[^\s\d]/.test(parent.innerText ?? ''))
				problems.push(`${describe(parent)} has a comma without a space after it`);
		}
		return problems;
	});
}
