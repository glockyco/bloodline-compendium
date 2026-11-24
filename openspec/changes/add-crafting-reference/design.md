## Context

Findings come from Steam build 24464547. `curate-item-reference` first planned this work and moved it here, so that the item reference ships with what is verified.

What the game holds:
- `WeaponComponentTable` has 791 parts with their class, part kind, the weapon types they fit (`TypeFilters`), damage, speed, value and `WeaponsmithingLevelRequired`. Only 11 rows name the weapon types they fit, so an empty `TypeFilters` does not say which weapons a part fits.
- `ItemRecipes` has 1,124 rows with up to four ingredients, their quantities, `CraftAmount` and `CraftingType`. `AllFoodRecipes` has 100 rows and `SeedDataTable` 18. 41 of the 369 listed weapons and 2 of the 402 listed armor pieces have a recipe. Weapons assembled from parts are not listed items, because each is its own combination.
- The anvil scales a crafted weapon when it is made. `Crafting:Begin Crafting Minigame` opens `BlacksmithingMinigame` for an anvil recipe or a weapon from parts. `BlacksmithingMinigame:Scale Weapon Based On Quality` writes the scaled damage, speed and value into the new item's `ItemInfo`, and `Crafting:Add Custom Weapon to Inventory` multiplies a weapon from parts by the minigame's quality. With Auto Craft, `BlacksmithingMinigame:Auto Craft` sets the quality from `RandomIntegerInRange(1, 5)` and the Weaponsmithing level instead of the minigame. Copies of the same crafted weapon therefore differ, and an equipped crafted weapon carries values that no class default holds.
- `Crafting` and `CustomWeapon_Base` each convert a quality to a stat multiplier in their own `Convert Quality to Stat Multiplier`, which `curate-item-reference` read as D 0.75 and F 0.5 against 0.9 and 0.8. The equipment slot, inventory and storage widgets have their own copies. Which one the equipped weapon uses is open.
- `Armorsmithing` holds the armorsmithing qualities and `Check Armor Quality is Unlocked`. The player also has rune tiers, an attunement table and enchantments with melee trigger types, as `establish-reference-architecture`'s design records, and the code index locates `Begin Attunement` and `Add Enchanted Item to Inventory`.
- `AllIngredientEffects` has 27 rows of ingredient treatments, which `Potioncrafting_Ingredient_Slot:Retrieve Prep Effects` reads. 27 item pages show these treatment effects from the table, and no lab run has confirmed that a meal or potion applies them.

What the compendium has:
- `CraftingMechanism` reads `ItemRecipes` joined to `WeaponComponentTable`, and the recipe rows that `Crafting:Get Correct Item` maps by hand, and publishes recipe sources with station, discipline, level, experience and output. A mapped row the table lacks goes to the maintainer report, as Round Mace's `RoundedMace` does. `CookingMechanism` and `FarmingMechanism` do the same for meals and harvests. `RecipeLine.svelte` shows a recipe.
- `Stations.For` maps disciplines to the anvil, focus assembly table, smelting oven, engineer's workbench, necromancer's altar and cooking fire.
- `CraftingData` in `SiteDataModel.cs` declares weapon families, parts, quality grades and armor qualities, but no producer fills it and `CatalogWriter` does not write it.
- `Recipe.Learned` exists in site data, and every producer leaves it null.
- `site/src/lib/rules/` holds the price, experience and progression rules and the rule check.

## Goals / Non-Goals

**Goals:**

- A part, quality, armorsmithing quality, rune or enchantment shows on the reference what it does to the finished item, with the rule's arithmetic.
- The weapon builder, the armor pages and `add-equipment-planner`'s Equipment compute from the same rules.
- Every published crafting rule rests on a lab run of the crafted and equipped item.

**Non-Goals:**

- The planner's Equipment and Item Bar, which `add-equipment-planner` adds.
- Combat simulation and damage per second.
- Crafting optimization, such as the cheapest route to an item.
- Station locations, which arrive with places, and station pages, which the step 3 decision rules out.
- Farming mechanics beyond the published yields, growth times and levels, which wait for the mechanics guides.

## Decisions

### Components

| Component | Owns | Consumes |
|---|---|---|
| Catalog producers beside `CraftingMechanism` | Weapon assembly, quality grades, armorsmithing qualities, rune attunement, enchanting, recipe learning and ingredient treatments, as typed `CraftingData` and item facts | Extracted tables, class defaults and parsed functions |
| Site data | A filled `CraftingData`, written as its own file, and recipe learning in `Recipe.Learned` | The producers |
| Rules in `site/src/lib/rules/` | Weapon assembly from parts and quality, armorsmithing armor, and armor and mobility for a set of pieces, each with worded steps and citations | `CraftingData` and the armor rule |
| Item and profession pages | Part, quality and armor facts with the rule's arithmetic, recipe learning, and links into the weapon builder | Rules and site data |
| Weapon builder | Choosing a family, parts and quality, with its state in the address | Rules and `CraftingData` |

Item sources stay with the source mechanisms. Crafting rules do not enter the source model.

### Reference before the planner

A crafted weapon's numbers depend on the quality of each copy, so the planner cannot show crafted gear from item pages alone. This change ships the rules and their pages first. `add-equipment-planner` designs Equipment's slot entries and link format once, for found and crafted gear, on the model this change publishes. The weapon builder is the first tool on the verified rules, and Equipment reuses its weapon editor.

### Lab first

Each rule ships only after a check under `tools/runtime/checks/` observes it in the running game, as the runtime-checks skill describes under "What an observation must show". Where two of the game's conversions disagree, the run decides which one the game applies. A quality reached through the minigame and one rolled by Auto Craft are checked separately.

### Recipes and treatments stay, other cooking and farming do not

How a recipe is learned answers how to get any crafted item, gear or meal, so recipe learning belongs here. The treatment check confirms facts that 27 ingredient pages already show. New cooking or farming mechanics have no specific question behind them yet, and the effects of meals and potions join the planner only with step 9's Effects section.

## Risks / Trade-offs

- The minigame may need a real cursor or timed input that a headless lab session cannot give. Auto Craft's roll then settles which grades exist, and the minigame's mapping to grades stays unknown until a window session measures it.
- Rune and enchanting values may be random within ranges. Pages then show the range and its odds, not a single number.

## Open Questions

- Which quality grades the anvil gives, how the minigame's result and Auto Craft's roll with the Weaponsmithing level map to them, which conversion applies to equipped weapons, and to which stats.
- Which parts fit which weapons, and how part contributions combine.
- Armor by class and quality, required levels, material costs and rune slots per armorsmithing quality.
- Attunement effectiveness and break chance by tier and level, and what enchanting changes.
- How a recipe becomes available to the player.
- Which treatments the cooking and potion windows apply.
