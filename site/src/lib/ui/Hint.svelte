<script lang="ts">
	/**
	 * A value with more to say about it, which shows on hover, on keyboard focus and on tap, since a tap focuses the
	 * element. It needs no JavaScript.
	 */
	let {
		text,
		hint,
		id,
		soft = false
	}: {
		text: string;
		hint: string;
		id: string;
		/** Whether the value reads softer than its neighbours, as a value that is not known does. */
		soft?: boolean;
	} = $props();
	// Escape hides the hint until the pointer or focus leaves, as WCAG asks of content shown on hover or focus.
	let dismissed = $state(false);
</script>

<span
	class="hinted"
	class:soft
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
	>{text}<span class="hint" role="tooltip" {id}>{hint}</span></span
>

<style>
	.hinted {
		position: relative;
		text-decoration: underline dotted;
		text-underline-offset: var(--underline-offset);
		cursor: help;
	}
	.soft {
		color: var(--text-soft);
	}
	.hint {
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
	.hinted:is(:hover, :focus):not(.dismissed) .hint {
		display: block;
	}
</style>
