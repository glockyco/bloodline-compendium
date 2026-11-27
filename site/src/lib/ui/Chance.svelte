<script lang="ts">
	import { formatOneIn, formatPercentRange } from '../format.js';
	import Hint from './Hint.svelte';
	/**
	 * A chance in a column of chances. Every row reads as a percent, so the rows compare at a glance, and the hint
	 * reads the same chance as about 1 in N.
	 */
	let { min, max = min, id }: { min: number; max?: number; id: string } = $props();
	const text = $derived(formatPercentRange(min, max));
	const hint = $derived(formatOneIn(min, max));
</script>

{#if hint}<Hint {text} {hint} {id} />{:else}{text}{/if}
