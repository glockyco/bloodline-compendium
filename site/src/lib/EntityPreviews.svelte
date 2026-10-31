<script lang="ts">
	import { onMount } from 'svelte';
	import { beforeNavigate } from '$app/navigation';
	import TooltipPresenter from './TooltipPresenter.svelte';
	import { loadPreview, type Preview } from './previews.js';
	import { linkLine, placeBeside } from './floating.js';

	/**
	 * Shows the tooltip of every link marked data-preview on hover after a short intent, on keyboard focus and on a
	 * first tap, and a second tap follows the link. One listener serves the page, so a list of a thousand links does
	 * no work until a reader points at one. Without JavaScript the links lead straight to the entity's page. The
	 * attribute holds the address of the preview's data, and `TooltipPresenter` shows it in its kind's tooltip.
	 */
	let preview = $state<Preview | null>(null);
	let failed = $state(false);
	let panel = $state<HTMLElement | undefined>();
	// A phone docks the preview at the edge away from the link, so it never covers what the reader tapped.
	let dockTop = $state(false);
	let anchor: HTMLAnchorElement | null = null;
	let tapped: HTMLAnchorElement | null = null;
	let touch = false;
	let hovering = false;
	let pointer: { x: number; y: number } | null = null;
	let intent: ReturnType<typeof setTimeout> | undefined;
	let stopPlacing: (() => void) | undefined;
	const id = 'entity-preview';

	function linkOf(target: EventTarget | null): HTMLAnchorElement | null {
		return target instanceof Element ? target.closest<HTMLAnchorElement>('a[data-preview]') : null;
	}
	async function show(link: HTMLAnchorElement) {
		if (anchor !== link) close();
		anchor = link;
		dockTop = link.getBoundingClientRect().top > window.innerHeight / 2;
		link.setAttribute('aria-describedby', id);
		const loaded = await loadPreview(link.dataset.preview!);
		// A navigation can remove the link while its details load.
		if (anchor !== link || !link.isConnected) return;
		failed = !loaded;
		preview = loaded;
	}
	function close() {
		clearTimeout(intent);
		stopPlacing?.();
		stopPlacing = undefined;
		anchor?.removeAttribute('aria-describedby');
		anchor = null;
		tapped = null;
		preview = null;
		failed = false;
	}
	// The panel appears once it has content, and is placed beside the line of the link the reader points at.
	$effect(() => {
		if (!panel || !anchor || (!preview && !failed)) return;
		const link = anchor;
		stopPlacing?.();
		stopPlacing = placeBeside(
			linkLine(link, () => (hovering ? pointer : null)),
			panel
		);
	});
	// The page that held the link goes away, so its tooltip goes with it.
	beforeNavigate(() => {
		hovering = false;
		close();
	});
	onMount(() => {
		const over = (event: PointerEvent) => {
			const link = linkOf(event.target);
			if (!link || event.pointerType === 'touch') return;
			hovering = true;
			pointer = { x: event.clientX, y: event.clientY };
			if (link === anchor) return;
			clearTimeout(intent);
			intent = setTimeout(() => void show(link), 160);
		};
		const move = (event: PointerEvent) => {
			if (event.pointerType !== 'touch') pointer = { x: event.clientX, y: event.clientY };
		};
		const out = (event: PointerEvent) => {
			const link = linkOf(event.target);
			if (!link || event.pointerType === 'touch' || link.contains(event.relatedTarget as Node))
				return;
			hovering = false;
			clearTimeout(intent);
			// A keyboard focus keeps its tooltip after the pointer leaves.
			if (link === anchor && !link.matches(':focus-visible')) close();
		};
		// A click focuses its link too, but only keyboard focus asks for the tooltip.
		const focus = (event: FocusEvent) => {
			const link = linkOf(event.target);
			if (link && !touch && link.matches(':focus-visible')) void show(link);
		};
		const blur = (event: FocusEvent) => {
			if (linkOf(event.target) && !hovering) close();
		};
		const down = (event: PointerEvent) => {
			touch = event.pointerType === 'touch';
			if (touch && !linkOf(event.target)) close();
		};
		const click = (event: MouseEvent) => {
			const link = linkOf(event.target);
			if (!link || !touch || tapped === link) return;
			event.preventDefault();
			// Opening a preview closes the previous one, which forgets its tap, so the tap is remembered after.
			void show(link);
			tapped = link;
		};
		// A tooltip taller than the window scrolls by the arrow and page keys while its link keeps focus, as zoomed
		// keyboard readers cannot move into it without closing it.
		const steps: Record<string, number> = { ArrowDown: 1, ArrowUp: -1, PageDown: 8, PageUp: -8 };
		const key = (event: KeyboardEvent) => {
			if (!anchor) return;
			if (event.key === 'Escape') {
				event.preventDefault();
				close();
				return;
			}
			const step = steps[event.key];
			if (
				step &&
				panel &&
				document.activeElement === anchor &&
				panel.scrollHeight > panel.clientHeight
			) {
				event.preventDefault();
				panel.scrollBy({ top: step * 40 });
			}
		};
		document.addEventListener('pointerover', over);
		document.addEventListener('pointermove', move, { passive: true });
		document.addEventListener('pointerout', out);
		document.addEventListener('focusin', focus);
		document.addEventListener('focusout', blur);
		document.addEventListener('pointerdown', down);
		// The router follows links from its own click listener, so a first tap is caught in the capture phase.
		document.addEventListener('click', click, true);
		document.addEventListener('keydown', key);
		return () => {
			close();
			document.removeEventListener('pointerover', over);
			document.removeEventListener('pointermove', move);
			document.removeEventListener('pointerout', out);
			document.removeEventListener('focusin', focus);
			document.removeEventListener('focusout', blur);
			document.removeEventListener('pointerdown', down);
			document.removeEventListener('click', click, true);
			document.removeEventListener('keydown', key);
		};
	});
</script>

{#if preview || failed}<div
		class="entity-preview"
		class:dock-top={dockTop}
		{id}
		role="tooltip"
		bind:this={panel}
	>
		{#if preview}<TooltipPresenter {preview} />{:else}<p>The details could not be loaded.</p>{/if}
	</div>{/if}

<style>
	/* Hidden until placed, so it never flashes at the corner of the page. */
	.entity-preview {
		position: fixed;
		z-index: var(--layer-tooltip);
		top: 0;
		left: 0;
		visibility: hidden;
		width: var(--size-tooltip);
		padding: 0;
		border-radius: var(--radius-1);
		background: var(--surface-panel);
		box-shadow: var(--shadow-raised);
		overflow: auto;
		pointer-events: none;
	}
	/* The game card is the popover's top edge, with no second frame around it, and the site's facts below it share
	   one inset. */
	.entity-preview :global([data-ui='tooltip'] > :not(.card)) {
		padding-inline: var(--inset);
	}
	.entity-preview :global(:is([data-ui='tooltip'], .route):last-child) {
		padding-bottom: var(--inset);
	}
	/* Phones dock the tooltip at the bottom, where it covers no link the reader is reading. */
	@media (width < 40rem) {
		.entity-preview {
			inset: auto var(--gap-block) var(--gap-block) !important;
			width: auto;
			max-height: var(--size-preview-phone) !important;
			visibility: visible;
		}
		.entity-preview.dock-top {
			inset: var(--gap-block) var(--gap-block) auto !important;
		}
	}
</style>
