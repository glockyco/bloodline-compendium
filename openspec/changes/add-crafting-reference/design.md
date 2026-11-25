## Context

Findings come from Steam build 24464547. `curate-item-reference` first planned this work and moved it here, so that the item reference ships with what is verified.

What the game holds:
- `WeaponComponentTable` has 791 parts with their class, part kind, the weapon types they fit (`TypeFilters`), damage, speed, value and `WeaponsmithingLevelRequired`. Only 11 rows name the weapon types they fit, so an empty `TypeFilters` does not say which weapons a part fits.
- `ItemRecipes` has 1,124 rows with up to four ingredients, their quantities, `CraftAmount` and `CraftingType`. `AllFoodRecipes` has 100 rows and `SeedDataTable` 18. 41 of the 369 listed weapons and 2 of the 402 listed armor pieces have a recipe. Weapons assembled from parts are not listed items, because each is its own combination.
- Only weapons assembled from parts get a quality grade. A recipe weapon, such as Copper Claymore, comes out of Craft and Auto Craft with its class defaults every time: 34 crafts in run `craft-quality` gave 25 damage and 100 value. Assembling a weapon opens `BlacksmithingMinigame`, whose quality bar gives F at 0% to 24%, D to 49%, C to 69%, B to 89%, A to 99% and S at 100% (`artifacts/24464547/runtime/craft-quality.md`).
- A grade scales two things differently. The stored damage and value that the inventory and equipment slot show use `Crafting:Convert Quality to Stat Multiplier`, 0.50, 0.75, 1.00, 1.10, 1.20 and 1.30 from F to S, truncated. The equipped weapon rebuilds its combat damage and attack speed from the unscaled part sums with `CustomWeapon_Base`'s own conversion, 0.80, 0.90, 1.00, 1.10, 1.20 and 1.30. An F spear of 15 part damage stores 7 and hits for 12, run `craft-quality-assembly`.
- Assembly's Auto Craft button is hidden. Its event, called in the lab only, rolls F, D or C at 20%, 60% and 20% whatever the Weaponsmithing level.
- A recipe's Auto Craft also adds an empty weapon named "Empty" with "Quality: S", 0 damage and 0 value, reproduced on a fresh save in run `craft-quality-blank`.
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

An assembled weapon's numbers depend on its parts and the grade of each copy, so the planner cannot show it from item pages alone. This change ships the rules and their pages first. `add-equipment-planner` designs Equipment's slot entries and link format once, for found and crafted gear, on the model this change publishes. The weapon builder is the first tool on the verified rules, and Equipment reuses its weapon editor.

### Lab first

Each rule ships only after a check under `tools/runtime/checks/` observes it in the running game, as the runtime-checks skill describes under "What an observation must show". Where two of the game's conversions disagree, the run decides which one the game applies. A quality reached through the minigame and one rolled by Auto Craft are checked separately.

### Recipes and treatments stay, other cooking and farming do not

How a recipe is learned answers how to get any crafted item, gear or meal, so recipe learning belongs here. The treatment check confirms facts that 27 ingredient pages already show. New cooking or farming mechanics have no specific question behind them yet, and the effects of meals and potions join the planner only with the planner's Effects section in the plan's mounts and companions step.

## Risks / Trade-offs

- Pages that show an assembled weapon's damage must say which of the two numbers a player sees where, because the inventory shows the stored one and combat uses the other. The stored number for F and D is a known-issue candidate.
- Rune and enchanting values may be random within ranges. Pages then show the range and its odds, not a single number.

## Open Questions

- Which parts fit which weapons, and how part contributions combine.
- Armor by class and quality, required levels, material costs and rune slots per armorsmithing quality.
- Attunement effectiveness and break chance by tier and level, and what enchanting changes.
- How a recipe becomes available to the player.
- Which treatments the cooking and potion windows apply.
