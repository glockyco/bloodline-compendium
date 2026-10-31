## 1. Contract

- [x] 1.1 Add the contract changes from design.md to `SiteDataModel.cs`, regenerate types and fixtures, and verify `bun run types:check` and the fixture tests pass

## 2. Lab confirmations

- [x] 2.2 Buy and sell one item in a city shop at neutral respect and at a raised respect, buy one item in the settlement Blacksmith, and try to buy Leather Pants from the settlement Armorer, in two runs
- [x] 2.3 Open containers with equal pick bounds and count the items they make, and kill one enemy with a rare pool that no death event spawns and one that one does, until the pick count and the rare pools are settled

## 3. Catalog economy

- [x] 3.1 Publish ingredient treatments from `AllIngredientEffects` and remove the `ReagentInfo` facts, and verify Gildleaf's chopped treatment reads Restore Health 35
- [x] 3.2 Publish base values with each shop's price rule, add `site/src/lib/rules/price.ts` with the respect tiers checked by `rules:check` against the lab purchases, and verify the 16 zero prices follow the lab result
- [x] 3.3 Replace weights, chances and picks with odds per event for containers, settlement deliveries and enemy drops, publish rare pools only for death events that spawn them, and verify an Ethereal Chest and a settlement building against hand calculations
- [x] 3.4 Publish item status and the displayed tooltip values, and verify Allseer's Judgement and Leather Pants

## 4. Catalog sources

- [x] 4.3 Publish recipe stations, levels, experience and output, and verify Apple Pie and Angled Iron Blade
- [x] 4.4 Replace generic condition warnings with specific player conditions, translate dialogue states to actions where the dialogue shows them, and verify the Scarecrow merchant's condition
- [x] 4.5 Name settlement buildings as the player's settlement, name places from entrances and event maps, and map internal area levels such as Generic Autumn Birch Area to the places players know, and verify the Mages College Main Hall, the settlement Armorer and Raw Leather
- [x] 4.9 Key source entities by what the player meets and make the graph reject one key with two names, and verify Iron Ingot's pickups, Copper Ore's deposits, Astral Ore's container and Wakazashi's merchant
- [x] 4.10 State a level or chance in a summary only when every summarized source shares it and otherwise its range, and verify Standard Mystcopper Axe Head and Azure Lover
- [x] 4.11 Read ore refining and publish smelting recipes, trace recipes whose output differs from their name and recipes listed twice, and verify Iron Ore, Angled Mystcopper Blade and Steel Claymore Guard
- [x] 4.12 Word rare pools, secondary container chances, unresolved pickups and bounty notes as a player needs them, and verify The Copper Crown, Ethereal Archstone and A Walk in the Woods

## 6. Reader text and support pages

- [x] 6.1 Add the pipeline words and identifier patterns to `scripts/voice-rules.json`, rewrite labels and copy across the site, and verify the voice check passes on real data
- [x] 6.2 Move coverage to `report.json`, remove the Coverage route and its links, and add the reference's limits to About, and verify no page links /coverage/
- [x] 6.3 Rebuild Known Issues with an issue list, player facts first and a collapsed developer section, fix the release name, and verify it at 390 px
- [x] 6.4 Align home page counts and links with the list, route missing URLs to the missing-page view, and verify an unknown URL in the preview build

## 7. Item browsing

- [x] 7.1 Render facets and columns from the published definitions with counts that follow the other filters, hidden empty values and narrowing fields only on long groups, and verify the Weapons category at 1440 and 390 px
- [x] 7.2 Add stat filters that add sortable columns with all or any matching, and verify the healing sort for food
- [x] 7.3 Make name search match every item with its status and keep the hidden-by-default count visible, and verify Coconut
- [x] 7.4 Add Pagefind list entries for category and type words, and verify searching swords
- [x] 7.5 Scope the place filter to the selected ways to get an item, offer only the selected category's stats, order filter groups by use with a skip link to the results, keep counts whole, keep the name column visible in wide tables and keep focus on the availability toggle, and verify Materials with Merchants and Elderglen Woods, Armor at 1440 px and Weapons at 768 px
- [x] 7.6 Give weapons one type vocabulary across list, filters and pages, sort Speed by speed, drop Range, which is no distance, and mark unknown values, and verify Lonland Pistol, Musket Sniper and the Speed sort

## 8. Item page

- [x] 8.1 Order the page as design.md states, with the first-screen answer led by verbs and carrying conditions, and remove Related entities, and verify the Dark Brown Scarecrow Hat at 1440 and 390 px
- [x] 8.2 Render the recipe line and the uses list with quantities and filters, and verify Apple Pie and Iron Ingot
- [x] 8.3 Group detailed sources by method and place with labelled odds and conditions once, and verify Lonland Pistol and Gold Ingot
- [x] 8.4 Show displayed tooltip values beside applied values, statuses for items without sources, and previews on every item link with keyboard and touch, and verify Leather Pants, Allseer's Judgement and an ingredient link with JavaScript off
- [x] 8.5 Read which tooltip lines the game shows in render mode for food, materials, parts and armor without a row, correct `TooltipCheck`, the displayed values and the known issues, reproduce every known issue on screen and by effect in two runs with `tools/runtime/checks/issues.sh`, and observe every page an armor note reaches with `tools/runtime/checks/noted-armor.mjs`, and verify Apple Pie, Roasted Apple, Iron Ingot and Leather Pants
- [x] 8.6 Show item icons wherever a page lists items, rebuild Used For as a table of product, skill and level, and amount with type, level and name filters, and drop the repeated item name and the subtitle that repeats the category, and verify Iron Ingot and Gold Coin
- [x] 8.7 Keep the first-screen answer short on phones with a jump to uses, lay out sparse pages in one column, make each ingredient one target, fit currency prices and long tooltip names, move focus into revealed rows and put the unknown-source caveat beside the status, and verify Iron Ore, Leather Pants, Amethyst Wand Shaft and Spear of Chaos
- [x] 8.8 Set a payload budget for item pages and the item list and check it in the build, and verify Iron Ingot and the list
- [x] 8.9 Load the missing page's assets from the site root, open previews on a first tap and separate places with a space, and verify a nested missing address, a phone tap and Gold Coin
- [x] 8.10 Capture every item's tooltip in render mode with `tooltip-sweep.mjs --capture`, compare each screenshot with the lab's shown-only reading, and give every difference an owner, and verify that no published tooltip value lacks a screenshot that shows it
- [x] 8.11 Link a GitHub issue form for wrong facts from every page's footer, with the page's address filled in, and verify the link from Iron Ingot opens the form with its address

## 10. Layout system

- [x] 10.1 Define spacing roles in `tokens.css`, make stylelint accept only roles for padding, margin, gap and inset outside it, and migrate every declaration
- [x] 10.2 Build the Section, Surface, DataTable and ItemRef primitives and move every page onto them, with headings outside surfaces and no page spacing, and verify every page kind at 1440, 1024, 768 and 390 px
- [x] 10.3 Add the layout check over extreme pages at four widths, run it on fixtures in CI and on the real-data preview, and verify it fails on each persona review defect before the fix
- [x] 10.4 Fix the remaining presentation findings: the header at 768 px, breadcrumb separators at line starts, count badges apart from their headings, the Known Issues expansion gap and tall tooltips at 200% zoom
- [x] 10.5 Repeat the persona review on the real-data preview after groups 4, 7, 8 and 10, and record it in design.md
- [x] 10.6 Apply type roles, parent-owned spacing, a prose measure and phone layouts for relation tables, item list rows, the header, the footer and the filter sheet, and verify every page kind at 1440, 1024, 768 and 390 px
- [ ] 10.7 Match misspelled game names in the list's name filter, tell apart hunting places that share a camp name, say what a settlement delivery is, read in the game what the Attack Speed number does and explain it beside the Speed word, and say where staves are typed Halberd, and verify Steel Rapier Gaurd, Deer, Azure Lover, Lonland Pistol and Acolyte Cryomancer Staff

## 11. Entity readiness

- [x] 11.1 Show every kind's preview through one presenter that picks the kind's tooltip, with the item tooltip as its first kind, and verify item previews on hover, focus and tap behave as before and an invented second kind gets its own tooltip
- [x] 11.2 Index every kind with pages for search from the kind registry, and verify an invented second kind's page is found by a word prefix with its kind filter
- [x] 11.3 List entities of one kind that share a name in `report.json` with their keys and places, and verify the merchants named Shopkeeper and the levels named Elderglen are listed

## 12. Integration

- [ ] 12.1 Run every check, rebuild real data, review every page kind on real data at 1440 and 390 px, and record the review in the commit bodies
