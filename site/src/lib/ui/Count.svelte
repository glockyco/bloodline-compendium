<script lang="ts">
	import { formatNumber } from '../format.js';
	let { value, label }: { value: number; label?: string } = $props();
</script>

<span class="count" aria-label={label}>{formatNumber(value)}</span>

<style>
	.count {
		display: inline-block;
		padding: 0 var(--space-2);
		border-radius: var(--radius-round);
		background: var(--surface-raised);
		color: var(--text-muted);
		font-family: var(--font-body);
		font-size: var(--type-1);
		font-weight: var(--weight-regular);
		line-height: var(--leading-snug);
		font-variant-numeric: tabular-nums;
		white-space: nowrap;
		vertical-align: middle;
	}
</style>
