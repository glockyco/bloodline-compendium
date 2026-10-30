# Proposal

## Why

Crafting is how players make most of their gear, and the reference shows only which recipe makes an item. It cannot yet say what a weapon part contributes to a finished weapon, what a quality grade does, what armorsmithing quality or a rune changes, or how a recipe is learned. The skill planner's first part plans levels and skills, and players discussing builds also compare gear, which needs equipment with its totals. These systems were first planned in `curate-item-reference` and moved here, so that the item reference ships first with what is verified.

## What Changes

- Settle the crafting rules in the lab: weapon quality on equipped weapons, armorsmithing qualities, rune attunement and enchanting, and treated ingredients in potions.
- Publish weapon assembly from parts, quality grades, armorsmithing qualities with rune slots and material costs, rune attunement, enchanting, and how each recipe is learned.
- Publish the recipes that the crafting code maps by hand, which the recipe table alone misses.
- Add a weapon builder and armor arithmetic on armor pages, both from shared rules.
- Add the planner's second part: equipment, crafted weapons, runes and enchantments with armor and mobility totals, in builds that keep sharing as the skill planner shares them.

## Capabilities

### New Capabilities

- `catalog/crafting-models`: weapon assembly, quality grades, armorsmithing, rune attunement, enchanting, recipe learning and hand-mapped recipes.
- `site/player-tools`: the weapon builder and armor arithmetic.
- `site/equipment-planner`: equipment in planned builds, with armor and mobility totals and crafted weapons.

### Modified Capabilities

None. The new capabilities consume `catalog/entity-graph`, `site/game-rules`, `site/presentation` and the skill planner's build links and characters without replacing their contracts.

## Impact

- Catalog: producers beside `CraftingMechanism`, `CookingMechanism` and `FarmingMechanism`, and a filled `CraftingData` in `SiteDataModel.cs` with regenerated types.
- Site: rules in `site/src/lib/rules/` for weapon assembly, quality and equipment totals, a weapon builder route, armor arithmetic on item pages, and the planner's equipment part.
- Lab: checks under `tools/runtime/checks/` for crafted weapon quality, armorsmithing, attunement, enchanting and treated ingredients.
- Depends on `curate-item-reference`, `publish-item-reference` and, for the planner part, `add-skills-and-planner`.
