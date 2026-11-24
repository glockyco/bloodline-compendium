<script lang="ts">
	/**
	 * A value that is not known, which reads Unknown and shows why on hover, on keyboard focus and on tap, since
	 * a tap focuses the element. It needs no JavaScript. A value that differs from case to case reads Varies.
	 */
	let { reason, id, label = 'Unknown' }: { reason: string; id: string; label?: string } = $props();
	// Escape hides the reason until the pointer or focus leaves, as WCAG asks of content shown on hover or focus.
	let dismissed = $state(false);
</script>

<span
	class="unknown-value"
	class:dismissed
	tabindex="0"
	role="button"
	aria-describedby={id}
	onkeydown={(event) => {
		if (event.key === 'Escape' && !dismissed) {
			dismissed = true;
			event.preventDefault();
		}
	}}
	onblur={() => (dismissed = false)}
	onmouseleave={() => (dismissed = false)}
	>{label}<span class="reason" role="tooltip" {id}>{reason}</span></span
>

<style>
	.unknown-value {
		position: relative;
		color: var(--text-soft);
		text-decoration: underline dotted;
		text-underline-offset: var(--underline-offset);
		cursor: help;
	}
	.reason {
		position: absolute;
		z-index: var(--layer-tooltip);
		inset-block-start: var(--position-below);
		margin-block-start: var(--gap-line);
		inset-inline-end: 0;
		display: none;
		width: max-content;
		max-width: var(--size-tooltip);
		padding: var(--inset-cell) var(--inset);
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-1);
		background: var(--surface-raised);
		box-shadow: var(--shadow-raised);
		color: var(--text);
		font-size: var(--type-1);
		font-weight: var(--weight-regular);
		text-align: start;
		white-space: normal;
	}
	.unknown-value:is(:hover, :focus):not(.dismissed) .reason {
		display: block;
	}
</style>
