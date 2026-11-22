<script lang="ts">
	import { formatNumber } from '../format.js';
	import { attributes, measuredLevels, type Attribute } from '../rules/attributes.js';
	import type { Evaluation } from './evaluate.js';
	/**
	 * The attributes as the game's level up window lists them, each with its points and a button to remove or add one,
	 * and what they give, grouped as the window's side panel groups it.
	 */
	let {
		levels,
		evaluation,
		change
	}: {
		levels: Record<Attribute, number>;
		evaluation: Evaluation;
		change: (attribute: Attribute, level: number) => void;
	} = $props();
	const effects = $derived(evaluation.effects);
	const percent = (value: number) => `${formatNumber(value)}%`;
	const groups = $derived(
		effects
			? [
					{
						title: 'Resources',
						rows: [
							['Health', formatNumber(effects.health)],
							['Mana', formatNumber(effects.mana)],
							['Stamina', formatNumber(effects.stamina)]
						]
					},
					{
						title: 'Regeneration',
						rows: [
							['Health Regen', effects.healthRegen.toFixed(2)],
							['Mana Regen', effects.manaRegen.toFixed(2)],
							['Stamina Regen', effects.staminaRegen.toFixed(2)]
						]
					},
					{
						title: 'Utility',
						rows: [
							['Armor Weight Reduction', percent(effects.armorWeightReduction)],
							['Disease Resistance', percent(effects.diseaseResistance)],
							['Pickup Strength', formatNumber(effects.pickupStrength)]
						]
					},
					{
						title: 'Learning',
						rows: [
							['Strength EXP Bonus', '+' + percent(effects.strengthExpBonus)],
							['Intelligence EXP Bonus', '+' + percent(effects.intelligenceExpBonus)],
							['Dexterity EXP Bonus', '+' + percent(effects.dexterityExpBonus)]
						]
					}
				]
			: []
	);
</script>

<div class="attributes">
	<ul class="levels">
		{#each attributes as attribute (attribute)}<li>
				<span class="name" data-game>{attribute}</span>
				<button
					type="button"
					aria-label="Remove a {attribute} point"
					disabled={levels[attribute] <= measuredLevels.min}
					onclick={() => change(attribute, levels[attribute] - 1)}>−</button
				>
				<output aria-label="{attribute} points">{levels[attribute]}</output>
				<button
					type="button"
					aria-label="Add a {attribute} point"
					disabled={levels[attribute] >= measuredLevels.max ||
						evaluation.levelUpPoints.remaining < 1}
					onclick={() => change(attribute, levels[attribute] + 1)}>+</button
				>
			</li>{/each}
	</ul>
	<dl class="effects">
		{#each groups as group (group.title)}<div class="group">
				<dt class="field-label">{group.title}</dt>
				{#each group.rows as [label, value] (label)}<dd>
						<span>{label}</span><span class="value">{value}</span>
					</dd>{/each}
			</div>{/each}
	</dl>
</div>

<style>
	.attributes {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(var(--size-card-narrow), 1fr));
		gap: var(--gap-block) var(--gap-groups);
	}
	.levels {
		display: grid;
		gap: var(--gap-inline);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.levels li {
		display: grid;
		grid-template-columns: minmax(0, 1fr) var(--size-control) var(--size-control) var(
				--size-control
			);
		align-items: center;
		gap: var(--gap-inline);
	}
	.levels button {
		padding: 0;
	}
	output {
		text-align: center;
		font-variant-numeric: tabular-nums;
	}
	.effects {
		display: grid;
		gap: var(--gap-block);
		margin: 0;
	}
	.group {
		display: grid;
		gap: var(--gap-line);
	}
	.effects dd {
		display: flex;
		justify-content: space-between;
		gap: var(--gap-inline);
		margin: 0;
	}
	.value {
		font-variant-numeric: tabular-nums;
	}
</style>
