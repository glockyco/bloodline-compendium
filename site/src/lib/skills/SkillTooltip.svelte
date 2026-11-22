<script lang="ts">
	import type { Fact } from '../site-data.js';
	import GameCard from '../ui/GameCard.svelte';
	import { formatNumber } from '../format.js';
	/**
	 * A skill as its in-game tooltip shows it: the name, the base damage when it has one, the cooldown and the cost
	 * in mana or stamina, and the description, in the tooltip's own words, such as "Cooldown: 2 seconds".
	 */
	let {
		skill,
		heading = 'h2'
	}: {
		skill: { name: string; icon: string | null; description: string | null; facts: Fact[] };
		heading?: 'h2' | 'p';
	} = $props();
	const lines = $derived(
		skill.facts.map((fact) => ({
			text:
				fact.number === null
					? `${fact.tooltipLabel ?? fact.label}: Unknown`
					: `${fact.tooltipLabel ?? fact.label}: ${formatNumber(fact.number)}${fact.unit ? ' ' + fact.unit : ''}`,
			size: 'small' as const
		}))
	);
</script>

<div data-ui="tooltip">
	<GameCard name={skill.name} icon={skill.icon} description={skill.description} {heading} {lines} />
</div>
