## Context

Findings come from Steam build 24464547. `curate-item-reference` first planned this work and moved it here, so that the item reference ships with what is verified.

What the game holds:
- `WeaponComponentTable` has 791 parts with their class, part kind, the weapon types they fit (`TypeFilters`), damage, speed, value and `WeaponsmithingLevelRequired`.
- `ItemRecipes` has 1,124 rows with up to four ingredients, their quantities, `CraftAmount` and `CraftingType`. `AllFoodRecipes` has 100 rows and `SeedDataTable` 18.
- `CustomWeapon_Base` assembles a crafted weapon from its parts. Two quality tables disagree: `Crafting` gives D 0.75 and F 0.5, and `CustomWeapon_Base` gives D 0.9 and F 0.8.
- `Armorsmithing` holds the armorsmithing qualities. The player also has rune tiers, an attunement table and enchantments with melee trigger types, as `establish-reference-architecture`'s design records.
- `AllIngredientEffects` has 27 rows of ingredient treatments, which the potion crafting window reads.

What the compendium has:
- `CraftingMechanism` reads `ItemRecipes` joined to `WeaponComponentTable` and publishes recipe sources with station, discipline, level, experience and output. `CookingMechanism` and `FarmingMechanism` do the same for meals and harvests. `RecipeLine.svelte` shows a recipe.
- `Stations.For` maps disciplines to the anvil, focus assembly table, smelting oven, engineer's workbench, necromancer's altar and cooking fire.
- `CraftingData` in `SiteDataModel.cs` declares weapon families, parts, quality grades and armor qualities, but no producer fills it and `CatalogWriter` does not write it.
- `Recipe.Learned` exists in site data, and every producer leaves it null.
- `site/src/lib/rules/` holds the price and experience rules and the rule check.

Not yet located: the quality table assets and the crafted armor classes in the extracted records, and the functions that apply attunement, enchanting and recipe learning.

## Goals / Non-Goals

**Goals:**

- A part, quality, armorsmithing quality, rune or enchantment shows on the reference what it does to the finished item, with the rule's arithmetic.
- The weapon builder and the planner's equipment part compute from the same rules as the pages.
- Every published crafting rule rests on a lab run of the crafted and equipped item.

**Non-Goals:**

- Combat simulation and damage per second.
- Crafting optimization, such as the cheapest route to an item.
- Station locations, which arrive with places.

## Decisions

### Components

| Component | Owns | Consumes |
|---|---|---|
| Catalog producers beside `CraftingMechanism` | Weapon assembly, quality grades, armorsmithing qualities, rune attunement, enchanting, recipe learning and hand-mapped recipes, as typed `CraftingData` and item facts | Extracted tables, class defaults and parsed functions |
| Site data | A filled `CraftingData`, written as its own file, and recipe learning in `Recipe.Learned` | The producers |
| Rules in `site/src/lib/rules/` | Weapon assembly from parts and quality, armorsmithing armor, and equipment totals for armor and mobility, each with worded steps and citations | `CraftingData` and the armor rule |
| Item pages | Part, quality and armor facts with the rule's arithmetic, and links into the weapon builder | Rules and site data |
| Weapon builder | Choosing a family, parts and quality, with its state in the address | Rules and `CraftingData` |
| Planner equipment part | Equipment slots, crafted weapons, runes and enchantments in a build, with totals | Rules, item facts and the skill planner's build links and characters |

Item sources stay with the source mechanisms. Crafting rules do not enter the source model.

### Lab first

Each rule ships only after a check under `tools/runtime/checks/` observes it in the running game, as the runtime-checks skill describes under "What an observation must show". Where two tables disagree, the run decides which one the game applies.

### Builds share one link format

Equipment joins the skill planner's builds and their link format rather than a second format, as Afallon's gear plan extends its character builds. The link budget of `add-skills-and-planner` applies to builds with equipment too.

## Risks / Trade-offs

- Crafted items carry their quality and parts per instance, so an equipped crafted weapon may need values that no class default holds. The lab reads them from the equipped instance.
- Rune and enchanting values may be random within ranges. Pages then show the range and its odds, not a single number.

## Open Questions

- Which quality table applies to equipped weapons, and to which stats.
- Whether every part that matches `TypeFilters` fits, and how part contributions combine.
- Armor by class and quality, required levels, material costs and rune slots per armorsmithing quality.
- Attunement effectiveness and break chance by tier and level, and what enchanting changes.
- How a recipe becomes available to the player.
- Which treatments the cooking and potion windows apply.
