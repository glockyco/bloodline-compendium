## 1. Evidence

- [ ] 1.1 Locate the crafted armor classes and the functions that apply weapon assembly, armorsmithing, attunement, enchanting and recipe learning, and record them in design.md
- [x] 1.2 Craft a recipe weapon and an assembled weapon through Craft, Auto Craft and the minigame, read each copy's grade and its stored and equipped damage, speed and value, and observe a hit, in runs `craft-quality`, `craft-quality-assembly` and `craft-quality-blank`
- [ ] 1.3 Assemble weapons from parts, including parts whose `TypeFilters` is empty, and record which parts the crafting window accepts and the finished weapon's equipped stats
- [ ] 1.4 Craft armor at each armorsmithing quality and read its armor, attune a rune at attunement level 1, and enchant a weapon and read its value in the running game, repeating a result that a random roll can change
- [ ] 1.5 Cook a meal and craft a potion with a treated ingredient, such as chopped Gildleaf, and record which treatments each window applies and what the result applies when eaten or drunk
- [x] 1.6 Check which crafting defects a player can reproduce. Record the grade D spear's lower damage in Theo Macgowan's Trading tooltip. Copper Claymore's extra "Quality: S" weapon requires a hidden handler and is not a known issue.
- [ ] 1.7 Record each result in design.md and as a check script under `tools/runtime/checks/`

## 2. Catalog

- [x] 2.1 Read the hand-mapped recipes of `Crafting:Get Correct Item`, and verify Copper Claymore's 25 damage version gains its recipe and Round Mace goes to the report
- [ ] 2.2 Fill and write `CraftingData` with weapon families, part kinds, fitting parts, contributions and the lab-confirmed quality grades and multipliers, and verify a greatsword's parts and its damage at one grade
- [ ] 2.3 Add armorsmithing qualities, levels, armor by class and quality, rune slots and material costs, and verify one quality's armor against the run
- [ ] 2.4 Add rune attunement and enchanting rules confirmed by the lab, and verify the break chance at attunement level 1. Link the Arcane Enchantment and Arcane Pulse rows under Harvested from Kills on Divine Favor to where they are published
- [ ] 2.5 Read how a recipe becomes available to the player and publish it as the recipe's learning, and verify Apple Pie
- [ ] 2.6 Publish ingredient treatments as the lab applied them, and verify Gildleaf's chopped effect

## 3. Rules and pages

- [ ] 3.1 Add weapon assembly, armorsmithing and armor rules to `site/src/lib/rules/` with worded steps, citations and recorded-run checks, and verify them against the lab runs with `rules:check`
- [ ] 3.2 Show part, quality and armor facts on item pages with the rule's arithmetic, and verify Angled Iron Blade and the Winter Warden Cuirass
- [ ] 3.3 Build the weapon builder from `CraftingData` with its state in the address and links from part pages, and verify a shared greatsword address

## 4. Integration

- [ ] 4.1 Run every check, rebuild real data, review the weapon builder, part, armor and profession pages on real data at 1440, 768 and 390 px with the `visual-review` agent, and record the review in the commit bodies
