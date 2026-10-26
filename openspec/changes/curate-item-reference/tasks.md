## 1. Contract

- [x] 1.1 Add the contract changes from design.md to `SiteDataModel.cs`, regenerate types and fixtures, and verify `bun run types:check` and the fixture tests pass

## 2. Lab confirmations

- [ ] 2.1 Craft a potion from a treated Gildleaf and verify the applied effect matches `AllIngredientEffects`, in two runs
- [x] 2.2 Buy and sell one item in a city shop at neutral respect and at a raised respect, buy one item in the settlement Blacksmith, and try to buy Leather Pants from the settlement Armorer, in two runs
- [x] 2.3 Open containers with equal pick bounds and count the items they make, and kill one enemy with a rare pool that no death event spawns and one that one does, until the pick count and the rare pools are settled
- [ ] 2.4 Craft and equip D and F quality weapons and read their equipped damage and speed, in two runs
- [ ] 2.5 Craft armor at each armorsmithing quality and read its armor, attune a rune at attunement level 1, and enchant a weapon and read its value, in two runs
- [ ] 2.6 Record each result in design.md under Lab findings and in a check script under `tools/runtime/checks/`

## 3. Catalog economy

- [x] 3.1 Publish ingredient treatments from `AllIngredientEffects` and remove the `ReagentInfo` facts, and verify Gildleaf's chopped treatment reads Restore Health 35
- [x] 3.2 Publish base values with each shop's price rule, add `site/src/lib/rules/price.ts` with the respect tiers checked by `rules:check` against the lab purchases, and verify the 16 zero prices follow the lab result
- [x] 3.3 Replace weights, chances and picks with odds per event for containers, settlement deliveries and enemy drops, publish rare pools only for death events that spawn them, and verify an Ethereal Chest and a settlement building against hand calculations
- [x] 3.4 Publish item status and the displayed tooltip values, and verify Allseer's Judgement and Leather Pants

## 4. Catalog sources

- [ ] 4.1 Read the hardcoded crafting mappings and faction shop defaults, and verify Copper Claymore and the Lionsmane Cuirass gain sources and Atacamite Ingot goes to the report
- [ ] 4.2 Read stables, contract scroll pools, seasonal gifts, archaeology rewards and level reward variables, and verify Brown Horse, Meteor Storm, Holly Harpoon, a fossil and Feral Maw gain sources
- [x] 4.3 Publish recipe stations, levels, experience and output, and verify Apple Pie and Angled Iron Blade
- [x] 4.4 Replace generic condition warnings with specific player conditions, translate dialogue states to actions where the dialogue shows them, and verify the Scarecrow merchant's condition
- [ ] 4.5 Name settlement buildings as the player's settlement and name places from entrances and event maps, and verify the Mages College Main Hall and the settlement Armorer
- [ ] 4.6 Read the weapon each enemy sets as its rare drop on construction and the finds Fenraen digs up, and verify a Bandit Cutter's weapon drop
- [ ] 4.7 Read the settlement Blacksmith's stock drawn from its item pool, and verify the Knight's Broadsword gains the Blacksmith with its chance per restock
- [ ] 4.8 Read how a recipe becomes available to the player and publish it as the recipe's learning, and verify Apple Pie

## 5. Catalog crafting

- [ ] 5.1 Write `crafting.json` with weapon families, part kinds, fitting parts, contributions and the lab-confirmed quality multipliers, and verify a greatsword's parts and its D quality damage
- [ ] 5.2 Add armorsmithing qualities, levels, armor by class and quality, rune slots and material costs, and verify heavy Masterwork armor 16 at level 60
- [ ] 5.3 Add rune attunement and enchanting rules confirmed by the lab, and verify the break chance at attunement level 1

## 6. Reader text and support pages

- [x] 6.1 Add the pipeline words and identifier patterns to `scripts/voice-rules.json`, rewrite labels and copy across the site, and verify the voice check passes on real data
- [x] 6.2 Move coverage to `report.json`, remove the Coverage route and its links, and add the reference's limits to About, and verify no page links /coverage/
- [x] 6.3 Rebuild Known Issues with an issue list, player facts first and a collapsed developer section, fix the release name, and verify it at 390 px
- [x] 6.4 Align home page counts and links with the list, route missing URLs to the missing-page view, and verify an unknown URL in the preview build

## 7. Item browsing

- [ ] 7.1 Render facets and columns from the published definitions with counts that follow the other filters, hidden empty values and narrowing fields only on long groups, and verify the Weapons category at 1440 and 390 px
- [ ] 7.2 Add stat filters that add sortable columns with all or any matching, and verify the healing sort for food
- [ ] 7.3 Make name search match every item with its status and keep the hidden-by-default count visible, and verify Coconut
- [ ] 7.4 Add Pagefind list entries for category and type words, and verify searching swords

## 8. Item page

- [x] 8.1 Order the page as design.md states, with the first-screen answer led by verbs and carrying conditions, and remove Related entities, and verify the Dark Brown Scarecrow Hat at 1440 and 390 px
- [x] 8.2 Render the recipe line and the uses list with quantities and filters, and verify Apple Pie and Iron Ingot
- [x] 8.3 Group detailed sources by method and place with labelled odds and conditions once, and verify Lonland Pistol and Gold Ingot
- [x] 8.4 Show displayed tooltip values beside applied values, statuses for items without sources, and previews on every item link with keyboard and touch, and verify Leather Pants, Allseer's Judgement and an ingredient link with JavaScript off

## 9. Player tools

- [ ] 9.1 Build the weapon builder from `crafting.json` with URL state and links from part pages, and verify a shared greatsword URL
- [ ] 9.2 Add the respect control to city prices with the choice remembered, and verify respect 750 in Elderglen
- [ ] 9.3 Add armor arithmetic to armor pages from the armor rule, and verify the Winter Warden Cuirass
- [ ] 9.4 Show seasonal state from the reader's date, and verify a Hallowed Eve item in October and in March

## 10. Integration

- [ ] 10.1 Run every check, rebuild real data, review every page kind on real data at 1440 and 390 px, and record the review in the commit bodies
