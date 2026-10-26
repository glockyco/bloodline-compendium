## Context

The review behind this change compared the item reference with the Afallon, Ancient Kingdoms and Erenshor compendiums, audited every page kind on real data at 1440 and 390 px, audited the game data and researched what players ask about The Bloodline. The reports and screenshots are kept in `artifacts/design/ux-review/`, which Git ignores.

Players ask, in this order: where to get an item, including puzzles and prerequisites, whether it beats what they have, what it is for and whether to keep or sell it, what a recipe needs, what food restores, and whether a tooltip can be trusted. The community wiki covers cooking and some unique weapons, and Steam guides hold the rest as long outdated documents. No resource links items, sources, recipes and uses.

The data audit found published values the game does not use. All of them were read from bytecode and still need lab confirmation:

- Potion ingredient treatments come from `ItemInfo.ReagentInfo`, which no function reads. `Potioncrafting_Ingredient_Slot:Retrieve Prep Effects` reads `AllIngredientEffects`. 47 pages carry the old facts.
- `Window_Trading_Shop:GetBuyValueWithMultiplier` buys at `truncate(ItemValue × BuyMultiplier)` and `GetSellValueWithMultiplier` sells at `max(truncate(ItemValue × (1.5 - BuyMultiplier)), 1)`. `Calculate Discount Based On Respect` sets the multiplier from city respect: 1.50 below -499, 1.44 to -250, 1.42 to -1, 1.40 to 249, 1.30 to 499, 1.20 to 749 and 1.00 from 750. Settlement buildings use `Shop_Window_Base:Get_Cost`, `round(ItemValue × Discount Multiplier)`, and the Blacksmith sets 0.5. Sixteen published shop prices are 0.
- `LootableObject:Generate New Loot` loops while its counter is at most a random draw between the minimum and maximum, redrawn each pass, starting at 0. A container with bounds 4 and 4 makes 5 picks.
- 51 rare-pool rows claim a chance per kill, but only some death events call `Spawn Dropped Loot`.

It also found about 100 items whose sources come from mechanisms the catalog does not read, 27 items referenced only from test levels, no recipe stations, weapon assembly through `CustomWeapon_Base` with two quality tables (`Crafting` gives D 0.75 and F 0.5, `CustomWeapon_Base` gives 0.9 and 0.8), armorsmithing qualities in `Armorsmithing`, 292 generic dialogue warnings, 219 spawner warnings and 49 unnamed places.

## Goals / Non-Goals

**Goals:**

- Every number on a page is one the game applies, labelled with the event it belongs to.
- Every page answers its player questions on the first screen and keeps evidence one step away.
- The site's own words describe the game and never the reference's internals.

**Non-Goals:**

- The interactive map, place pages, creature pages and merchant pages. They remain later changes. Sources keep naming places and merchants without links until those pages exist.
- Facts the game data does not hold: weight, durability, stack size, equip level, set bonuses and rarity for items outside `Item_Rarity_DataTable`.

## Decisions

### Contract changes

The site-data contract in `SiteDataModel.cs` changes as follows. Task 1.1 added them with defaults, and each later task fills the fields it owns.

- `ItemPage.Status`: `Obtainable`, `NoKnownSource` or `NotInNormalPlay`.
- `Fact.GameShows`: the displayed value where a known tooltip cause differs from the applied value.
- `Price` gains `Rule`: `City` with the city's reference, `Settlement` with the multiplier, or `Fixed`. A city price stores the base value, and the site computes purchase and sale prices with the rules module. A source whose purchase failed in the lab carries no price and a reason.
- `Source.Odds` replaces `Weight`, `Chance`, `ChanceBasis` and `Picks`: the event (`Container`, `Kill`, `Delivery`, `Use`), the chance of at least one copy, the expected copies, or null with a reason.
- `Source.Recipe`: station, discipline, required level, experience, output quantity and how it is learned. Recipe notes such as "Cooking experience: 35." go away.
- `Source.Place` grouping keys and `Source.Settlement` for settlement buildings.
- `ItemSummary` gains `Status`, a facet map and a column map that the catalog fills from the stat vocabulary, so the list is driven by data, as Afallon's kind registry drives its lists.
- `ItemIndex` gains the facet and column definitions per category, with labels and value order.
- `ItemPage.Uses` gains the use's discipline and output category for filtering.
- New files: `crafting.json` with weapon families, part kinds, parts, contributions and quality multipliers, and armorsmithing qualities. `issues.json` stays.
- `coverage.json` leaves the site data. The catalog writes `report.json` beside the site data with gaps, unresolved conditions, unread objects, tooltip differences without a cause and status evidence.

### Prices are computed in the rules module

Prices depend on the player's respect, which only the browser knows. The catalog publishes base value and rule, and `site/src/lib/rules/price.ts` applies the multipliers, checked against lab purchases with `bun run rules:check` like the experience rule. Pages default to neutral respect and name it.

### Odds come from read selection rules only

A chance is published only where the selection code is read and a lab run confirms the pick count. Settlement deliveries and containers are read. Enemy rare pools are published only for classes whose death event spawns them, and the rest go to the report. Odds read as about 1 in N below 1 percent.

### Reader-text rules are enforced twice

`scripts/voice-rules.json` gains the pipeline words and identifier patterns so the build fails on them. The rules that a pattern cannot catch, such as text about the reference or repeated caveats, are review criteria in the `site/reader-text` spec, checked on real data at 1440 and 390 px before each commit.

### Item list driven by facet and column definitions

Facets and columns are declared per category in the catalog, so a new kind or update needs no site change. Counts follow Afallon's rule: each facet counts under the other facets' selections. Stat filters add sortable columns and combine with all or any, as Ancient Kingdoms does. Search records for category words are added to Pagefind as list entries.

### Item page order

Title and identity, then the tooltip with any displayed-value notes and issue notes beside the first-screen answer, then recipe, uses, detailed sources grouped by method and place, and nothing else. The Related entities section goes, because sources, recipes and uses already render every relation. The page renders only sections with content.

### Coverage becomes a maintainer report

Coverage mixes player limits with extraction diagnostics, and the review found most of it unusable for players. Its player-relevant content becomes item statuses on pages and one About section. The rest goes to `report.json`, which the update procedure reviews.

### Order of work

Values come before presentation, because pages built on wrong numbers would be rebuilt twice: catalog economy first, then reader text and support pages, the item page, browsing, recovered sources, crafting and tools. Lab checks run as each value needs them, one session at a time, because the lab has one game copy.

## Risks / Trade-offs

- Lab checks may contradict the bytecode reading. The rule that produces the value changes, and the finding goes into this design.
- Removing the Coverage page removes a public page. The review found no player question it answers that item statuses and About do not.
- Prices depend on respect that the site cannot read. Pages state the assumed respect and let the reader change it.

## Lab findings

Lab confirmations are recorded here as they complete.

- Prices, run prices-1. Every merchant's dialogue fires `OPENdialogueSHOP` or `OPENdialogueSHOP_New`, both open `Window_Trading_Shop`, and the plain `ShopKeeper_Base` that settlement buildings spawn uses the `Shopkeeper_Base` dialogue with the same event. `Shop_Window_Base`, which would apply a building's `Discount Multiplier`, is only hidden by that event and never opened, so the settlement Blacksmith's 0.5 has no effect. `Get Current City` names a city only in the Elderglen, Arema, Khenmoore and Woodhurst levels, and `Get City Respect` reads saved respect only for those, so every other merchant trades at the neutral multiplier 1.4. The game's own `GetBuyValueWithMultiplier`, `GetSellValueWithMultiplier` and `Calculate Discount Based On Respect`, called on the trading window's class default for 84 value and multiplier pairs and 14 respect values, agree with `site/src/lib/rules/price.ts` in all 188 results.
- Container picks, run prices-1. `Generate New Loot` on a live container made 5 entries for bounds 4 and 4, 2 for 1 and 1, and 1 for 0 and 0, in 400 calls each. Bounds 3 and 11 gave 4 to 12 entries, most often 6 or 7, because the loop redraws its bound on each pass.
