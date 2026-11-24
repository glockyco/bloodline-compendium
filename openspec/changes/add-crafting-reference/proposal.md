# Proposal

## Why

41 of the listed weapons have a recipe, players assemble further weapons from parts, and the reference shows only which recipe makes an item. It cannot yet say what a weapon part adds to a finished weapon, what a quality grade does, what armorsmithing quality or a rune changes, or how a recipe is learned. A crafted weapon's damage, speed and value also differ from copy to copy, because the anvil scales them by the quality the player reaches when making it. The planner can therefore hold crafted gear only once these rules are verified. This change publishes them first, as `add-skill-reference` published skills before the planner used them, and `add-equipment-planner` builds the planner's Equipment on top of it.

## What Changes

- Settle the crafting rules in the lab: the quality the anvil gives and what it does to an equipped weapon, weapon assembly from parts, armorsmithing qualities, rune attunement, enchanting, how recipes are learned, and what ingredient treatments do in meals and potions.
- Publish weapon assembly from parts, quality grades, armorsmithing qualities with rune slots and material costs, rune attunement, enchanting, and how each recipe is learned.
- Correct the treatment effects on the 27 ingredient pages if the lab shows the game applies something else.
- Add a weapon builder and armor arithmetic on armor pages, both from shared rules that the planner reuses.

## Capabilities

### New Capabilities

- `catalog/crafting-models`: weapon assembly, quality grades, armorsmithing, rune attunement, enchanting, recipe learning, hand-mapped recipes and ingredient treatments.
- `site/player-tools`: the weapon builder and armor arithmetic.

### Modified Capabilities

None. The new capabilities consume `catalog/entity-graph`, `site/game-rules` and `site/presentation` without replacing their contracts.

## Impact

- Catalog: producers on the kind structure of `add-skill-reference`, beside `CraftingMechanism`, `CookingMechanism` and `FarmingMechanism`, and a filled `CraftingData` in `SiteDataModel.cs` with regenerated types.
- Site: rules in `site/src/lib/rules/` for weapon assembly, quality and armor, a weapon builder route, armor arithmetic on item pages, and crafting facts on item and profession pages.
- Lab: checks under `tools/runtime/checks/` for crafted weapon quality, weapon assembly, armorsmithing, attunement, enchanting, recipe learning and ingredient treatments.
- `add-equipment-planner` depends on this change.
