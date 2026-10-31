## 1. Evidence

- [ ] 1.1 Locate the quality tables, the crafted armor classes and the functions that apply weapon assembly, quality, armorsmithing, attunement, enchanting and recipe learning, and record them in design.md
- [ ] 1.2 Craft and equip D and F quality weapons and read their equipped damage and speed in the running game
- [ ] 1.3 Craft armor at each armorsmithing quality and read its armor, attune a rune at attunement level 1, and enchant a weapon and read its value in the running game, repeating a result that a random roll can change
- [ ] 1.4 Craft a potion from a treated Gildleaf and verify in the running game that the applied effect matches `AllIngredientEffects`
- [ ] 1.5 Record each result in design.md and as a check script under `tools/runtime/checks/`

## 2. Catalog

- [ ] 2.1 Read the hardcoded crafting mappings in `Crafting:Get Correct Item`, and verify Copper Claymore gains its recipe and Atacamite Ingot goes to the report
- [ ] 2.2 Fill and write `CraftingData` with weapon families, part kinds, fitting parts, contributions and the lab-confirmed quality multipliers, and verify a greatsword's parts and its D quality damage
- [ ] 2.3 Add armorsmithing qualities, levels, armor by class and quality, rune slots and material costs, and verify heavy Masterwork armor 16 at level 60
- [ ] 2.4 Add rune attunement and enchanting rules confirmed by the lab, and verify the break chance at attunement level 1
- [ ] 2.5 Read how a recipe becomes available to the player and publish it as the recipe's learning, and verify Apple Pie

## 3. Rules and pages

- [ ] 3.1 Add weapon assembly, armorsmithing and equipment total rules to `site/src/lib/rules/` with worded steps, citations and recorded-run checks, and verify them against the lab runs with `rules:check`
- [ ] 3.2 Show part, quality and armor facts on item pages with the rule's arithmetic, and verify Angled Iron Blade and the Winter Warden Cuirass
- [ ] 3.3 Build the weapon builder from `CraftingData` with its state in the address and links from part pages, and verify a shared greatsword address

## 4. Planner equipment

- [ ] 4.1 Add equipment slots, crafted weapons, runes and enchantments to planned builds with armor and mobility totals from the shared rules, and verify a build with the Winter Warden Cuirass and a crafted greatsword
- [ ] 4.2 Carry equipment in build links, link previews and the text copy within the skill planner's link budget, and verify a full build's link length and its preview

## 5. Integration

- [ ] 5.1 Run every check, rebuild real data, review the weapon builder, armor pages and the planner on real data at 1440 and 390 px, and record the review in the commit bodies
