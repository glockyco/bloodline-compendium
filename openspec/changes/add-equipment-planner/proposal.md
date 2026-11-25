# Proposal

## Why

Players who share builds compare gear as well as levels and skills, and the Character Planner has only Attributes, Skills and Skill Bars. Comparison 59 approved Equipment and Item Bar tabs for it. An assembled weapon's numbers depend on its parts and the grade of each copy, so Equipment builds on the crafting rules that `add-crafting-reference` verifies.

## What Changes

- An Equipment tab with the character sheet's 13 worn and held slots, holding found and bought items or crafted pieces with their parts, quality, runes and enchantment.
- Light, Medium and Heavy Armor levels in the build, so walking speed follows the armor levels the build's author planned.
- Armor and walking speed in the summary, from the shared armor rule, with known issues named on the build.
- An Item Bar tab with its eight slots, using Equipment's item choice.
- Equipment, armor levels and the Item Bar in build links, saved characters and the text copy.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `site/skill-planner`: builds gain Equipment, armor levels and the Item Bar, with their totals, links and text.

## Impact

- `site/src/lib/planner/`, `site/src/lib/rules/` and the planner route's payload budget in `scripts/payload-gate.mjs`.
- The catalog writes, as planner inputs, the items each slot and the Item Bar take with the facts the rules need.
- A lab check under `tools/runtime/checks/` for which items each slot takes.
- Depends on `add-crafting-reference`. Companion and mount choices arrive with the plan's mounts and companions step.
