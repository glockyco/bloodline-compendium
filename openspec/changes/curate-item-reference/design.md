## Context

The review behind this change compared the item reference with the Afallon, Ancient Kingdoms and Erenshor compendiums, audited every page kind on real data at 1440 and 390 px, audited the game data and researched what players ask about The Bloodline. The reports and screenshots are kept in `artifacts/design/ux-review/`, which Git ignores.

Players ask, in this order: where to get an item, including puzzles and prerequisites, whether it beats what they have, what it is for and whether to keep or sell it, what a recipe needs, what food restores, and whether a tooltip can be trusted. The community wiki covers cooking and some unique weapons, and Steam guides hold the rest as long outdated documents. No resource links items, sources, recipes and uses.

The data audit found published values the game does not use. All of them were read from bytecode and still need lab confirmation:

- Potion ingredient treatments come from `ItemInfo.ReagentInfo`, which no function reads. `Potioncrafting_Ingredient_Slot:Retrieve Prep Effects` reads `AllIngredientEffects`. 47 pages carry the old facts.
- `Window_Trading_Shop:GetBuyValueWithMultiplier` buys at `truncate(ItemValue × BuyMultiplier)` and `GetSellValueWithMultiplier` sells at `max(truncate(ItemValue × (1.5 - BuyMultiplier)), 1)`. `Calculate Discount Based On Respect` sets the multiplier from city respect: 1.50 below -499, 1.44 to -250, 1.42 to -1, 1.40 to 249, 1.30 to 499, 1.20 to 749 and 1.00 from 750. `Shop_Window_Base:Get_Cost` would charge `round(ItemValue × Discount Multiplier)`, and the settlement Blacksmith sets 0.5. Sixteen published shop prices are 0.
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
- `Price` gains `Rule`: `Trading` with the reference of the city whose respect applies, null outside those cities, or `Fixed`. A trading price stores the base value, and the site computes purchase and sale prices with the rules module.
- `Source.Odds` replaces `Weight`, `Chance`, `ChanceBasis` and `Picks`: the event (`Container`, `Kill`, `Delivery`, `Use`, `Catch`, or `Pick` where skill adds picks to an action), the chance of at least one copy, the expected copies, or null with a reason.
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

Facets and columns are declared per category in the catalog, so a new kind or update needs no site change. Counts follow Afallon's rule: each facet counts under the other facets' selections. Stat filters add sortable columns and combine with all or any, as Ancient Kingdoms does. Each item lists the ways to get it at each place, so a place filter keeps to the selected ways to get it: Materials from merchants in Elderglen leaves out materials only looted there. Search has a page for each category and type that links to the filtered list, such as Weapons: Dagger. Its title names the plural nouns that at least three of its item names end with, compounds such as Ultrasword included, because Pagefind ranks title words far above body words, and a list must reach the eight results the search box shows.

### Item page order

Title and identity, then the tooltip with any displayed-value notes and issue notes beside the first-screen answer, then recipe, uses, detailed sources grouped by method and place, and nothing else. The Related entities section goes, because sources, recipes and uses already render every relation. The page renders only sections with content. A phone shows the first three ways to get an item until the reader asks for the rest, and a page with no recipe, use or source table reads as one column.

Below 1024 px the page reads as one column, because a side column narrowed descriptions below a phone's measure at 768 px. A tablet keeps the answer first and the game card below it, at most 24rem wide. A phone shows the game card first, so what the item is for is on the first screen, and How to Get It follows. Both choices came from a review of rendered options on Apple, Iron Ore and two tablet pages.

### Coverage becomes a maintainer report

Coverage mixes player limits with extraction diagnostics, and the review found most of it unusable for players. Its player-relevant content becomes item statuses on pages and one About section. The rest goes to `report.json`, which the update procedure reviews.

### Spacing belongs to roles and layout primitives

Pages drifted although the tokens file closed every scale. 233 spacing declarations used 55 different values, 103 of them in the shared stylesheet, because each new section chose its own step. Text in the acquisition panel started at a different edge than text in the table below it, dividers were inset in one surface and full width in the next, a fixed-width chance column overflowed its table, and one section put its heading inside its surface while the next put it outside. A closed scale stops raw values, not different choices for the same role.

Spacing is chosen by role. `tokens.css` defines each role once: the inset of every surface and table cell, the block padding of rows, the gaps between lines of one entry, between entries, between a heading and its content and between sections, the inline gap and the page gutter. Stylelint accepts only roles for padding, margin, gap and inset outside `tokens.css`.

Layout primitives own the roles. `Section` places a heading, its count and its controls above one body. The `surface` class applies the inset, and a surface's entries draw full-width dividers. `RelationTable` applies the cell inset, right-aligns numeric columns with their unit in the header, renders group rows as subheadings and lets columns size to their content and wrap at spaces. `EntityLink` with its icon makes an item's icon and name one link. Item pages and Known Issues compose these and put every heading outside its surface. The item list keeps its own filter panel and table, because it is the one page with sorting, a pinned name column and a filter sheet, and it uses the same roles.

Afallon's presentation is the default: a layout, density, control or pattern it has is copied unless a reason specific to this game or a defect in Afallon argues against it, and that reason is written beside the difference. The game-faithful item tooltip, the category art and the plum identity are such differences. The roles favour density, because a reference is scanned, not read like an article. Afallon's item list fits about twelve rows on a 900 px screen where this list fits seven: its rows use smaller icons and less padding, its name search sits at the top of the results, its count, chips and reveal control share one line, and its filters are collapsible groups without a framing panel. List rows here take a 32 px icon and the row padding of the compact role, names keep their qualifier on the same line, sections are spaced by the section role instead of 6rem, and page titles stay proportionate to the content below them.

A layout check loads extreme pages, such as the longest names, the most sources, places and uses, special-currency prices and items without icons, at 1440, 1024, 768 and 390 px. It fails on horizontal overflow, a surface or cell off its inset, differing text edges inside one column, numeric cells that are not right-aligned and list separators without a space. It runs on fixtures in CI and on the real-data preview before a UI commit, because invented fixtures alone missed every one of these defects.

### Type and spacing roles

A visual review at four widths found the tokens consistent but their use not: a home card title outranked an item page's sections, paragraph margins added to parent gaps, prose ran to 160 characters a line and phones spent a fifth of the screen on the header. Type now has roles, each with one size everywhere: the page title, sections at 24 px, subsections and card titles at 20 px, body text, an introduction at 18 px, and metadata at 14 px for labels, counts, table headings, release dates and secondary lines. Data rows use a tighter leading than prose. Text elements carry no outer margins and their parents own the gaps: 4 px within an entry, 12 px between blocks, 16 px from a heading to its body and 32 px between sections. Text pages keep a 70 character measure.

On phones, a relation table row puts its name on the full width and its other columns on a line below, and an item list row puts the name first and its type, labelled stats and value on the next line, all in one surface. The header keeps the brand and navigation on one row above the search, the footer leads with its links at touch size, and the filter sheet uses touch-sized rows. Item pages read the answer, the game card and the details in one order at every narrow width.

### A source names what the player meets

Source rows named the wrong things: ingot pickups said Sporing Gloves, ore deposits said Abyssberry Bush, a Miner's Crate said Cabinet and Wakazashi's merchant said Jasper Thorne while the summary above said Connor Selby. Mechanisms keyed pickups, containers, gathering nodes and some merchants by their actor class, which many different objects share, and the graph kept the first name it saw for a key. A source entity's key now identifies what the player meets: a container, gathering node or pickup by its class and the name the player sees, a person whose dialogue differently named people share by their placement, and an unnamed pickup takes the name of the item lying there. The graph rejects one key with two names, so a shared key fails the catalog instead of renaming sources.

### A place takes the name a player knows

41 levels that sources use had no name from the game's entrances, so pages showed internal level names such as "Generic Autumn Birch Area". Three rules now name them:
- The 22 `Generic_*` levels in `Small_Event_Areas` are the maps the game opens when the player makes camp on the world map. `TopDownCharacter_Working:Get Location Tag Maps` picks one by the terrain tag nearest the camp, such as "Pine Forest" or "Autumnal Birch Forest", so each is named after its tag, as "Pine Forest camp". The tag "Hostile Logging Camp" opens `Generic_Birch_LumberMill_01`. No tag opens `Generic_SnowPineArea_02`, which keeps a name read from its level name.
- A level whose name is another level's name and an underscore is part of that place, such as `TheMagesCollege_MainHall`, named "The Mage's College: Main Hall", with a colon because pages list places with commas.
- Other levels without a name read their level name, without "Generic" or numbering and with small words in lower case, such as "Cathedral of Arema".

Settlement buildings stand only in the player's settlement, so their shops and deliveries are marked as the settlement's and read "in your settlement".

The Delivered by Settlements section explains that completed buildings add resources each game week and that Resources Ready in the settlement overview is where to collect them.

### A recipe makes the item its row names

The crafting window spawns the class of a recipe's `WeaponComponentTable` row and gives it the row's `ItemName`, as `BlacksmithingMinigame` names `Item Being Crafted` after the window's item text. 16 classes have several rows, so the Angled Steel Blade row builds an item called Angled Steel Blade on the Angled Mystcopper Blade class. Such a row is no recipe of the class's page and goes to the maintainer report. A row whose name is the class's item name with up to two letter edits makes that item, because rows and items misspell each other, as the Silver Gothic Guard row and the Silver Gothic Gaurd item do. Rows that make one item from the same inputs, level and experience under one name, such as `SteelRapierGuard` and `SteelJianGuard`, are one recipe.

Recipe rows without a component row, such as ingots and food, are made through `Crafting:Get Correct Item`, which compares the item to craft with 96 classes and names a row for each. Extraction records those pairs in the code index, and the game's own function returned the same row for each of the 96 classes in run recipe-rows-1. The pair for Round Mace names the row `RoundedMace`, which the recipe table lacks, so the report lists it.

### A weapon has one type

Weapons carry two type fields. `ItemInfo.WeaponType` is the attack type: equipping, proficiency experience and the enchantment stances read it, and the player's attack fires a gun only for the attack types Gun and Pistol. `ItemInfo.SpecificItemType` picks between firing branches inside that attack, and the inventory search matches both. Neither appears in a tooltip. A weapon's type is its attack type, so Lonland Pistol is a Pistol, and a weapon without one, such as a shield, takes its item type. Title, list and filter use this one value.

The speed word sorts by its meaning from Slow to Fast, with Standard beside Average. It is the game's authored label, not a conversion from `WeaponInfo.AttackSpeed`. The Attack Speed number is not published on pages, previews or the list, because weapon-specific animation and firing paths do not make it a universal attack-rate comparison. `WeaponInfo.Range` is not published, because no reader uses it as a distance. Crossbow bolts compare it with accuracy thresholds that every crossbow passes, the player's gun attack only tests whether it is zero, and the weapon traces that read it end near the world origin instead of in front of the player. A list cell is empty when the item lacks the stat and reads Unknown when the build does not tell it.

### Wording a player can act on

A rare pool's drop says it comes from a separate roll in addition to the normal drop, since its chance can be 100%. A container's own appearance chance says that the container is there only part of the time, apart from the chance per container. An item lying where no source names the place says it lies in an unknown place. A bounty note reads "Receive when accepting the quest" and names no place or condition, because the quest is the source.

### Order of work

Values come before presentation, because pages built on wrong numbers would be rebuilt twice: catalog economy first, then reader text and support pages, the item page, browsing, recovered sources, crafting and tools. Lab checks run as each value needs them, one session at a time, because the lab has one game copy.

## Risks / Trade-offs

- Lab checks may contradict the bytecode reading. The rule that produces the value changes, and the finding goes into this design.
- Removing the Coverage page removes a public page. The review found no player question it answers that item statuses and About do not.
- Prices depend on respect that the site cannot read. Pages state the assumed respect and let the reader change it.

## Lab findings

Lab confirmations are recorded here as they complete.

- Prices, run prices-1. Every merchant's dialogue fires `OPENdialogueSHOP` or `OPENdialogueSHOP_New`, both open `Window_Trading_Shop`, and the `ShopKeeper_Base` that settlement buildings spawn has a dialogue with the same event. `Shop_Window_Base`, which would apply a building's `Discount Multiplier`, is only hidden by that event and never opened, so the settlement Blacksmith's 0.5 has no effect. `Get Current City` names a city only in the Elderglen, Arema, Khenmoore and Woodhurst levels, and `Get City Respect` reads saved respect only for those, so every other merchant trades at the neutral multiplier 1.4. The game's own `GetBuyValueWithMultiplier`, `GetSellValueWithMultiplier` and `Calculate Discount Based On Respect`, called on the trading window's class default for 84 value and multiplier pairs and 14 respect values, agree with `site/src/lib/rules/price.ts` in all 188 results.
- Purchases, run shops-1. The settlement Armorer and Blacksmith, built beside the player, spawned shopkeepers holding their default item info. The Armorer's shopkeeper held Leather Pants, Leather Bracers and Curved Steel Plate Shoes with no item class, and the shop event and the window's stock both skip such entries, so the Armorer never offers them. In a trading window owned by the Blacksmith's shopkeeper with its 0.5 discount, the multiplier was 1.4, the Knight's Broadsword with base value 350 cost 490 and sold for 35, and Short Chain Sleeves with base value 0 cost nothing and sold for 1. With the window's city set to Elderglen, saved respect 600 gave 1.2, a cost of 420 and a sale of 104, and respect -600 gave 1.5, a cost of 525 and a sale of 1. The other 13 zero prices are free items. The shop event's own delay needs a world that its dialogue object lacks outside a conversation, so the run created the window as the event does.
- Container picks, run prices-1. `Generate New Loot` on a live container made 5 entries for bounds 4 and 4, 2 for 1 and 1, and 1 for 0 and 0, in 400 calls each. Bounds 3 and 11 gave 4 to 12 entries, most often 6 or 7, because the loop redraws its bound on each pass.
- Dialogue states. `AddSpecialTagDialogue` sets a tag that `CheckIfDialogueTagIsValid` and `CheckIfDialogueTagIsNOTValid` test. The Hallowed Eve Merchant, the Scarecrow shopkeeper's conversation, sets `SpokeWithScarecrow` when the player asks what he sells and tests it on the way to his stock, so the stock has no requirement beyond the season.
- Enemy drops, run odds-1. Enemies spawned beside the player and killed with the engine's ApplyDamage. A Copper Crown Bandit dropped its one-entry rare pool on each of 3 kills, and a Herald Ashbinder dropped its five-entry pool on 8 of 23 kills, against the 25 percent gate its Drop Loot draws first. A Bandit Cutter and a Bandit Heavy Swordsman each dropped a one-entry table holding a weapon outside their authored rare pools, because their construction scripts replace the pool. The loose-loot placer's loop runs from 1 to its maximum without a condition on the ground trace, read from its BeginPlay and not yet observed.

## Persona review

Six reviewers used the real-data preview as a phone newcomer, a weaponsmith, a gear optimizer, a completionist, a design critic and a keyboard user, at 1440, 1024, 768 and 390 px. Their reports and screenshots are in `artifacts/design/ux-review/personas/`. Beyond the spacing and source identity problems above, they found:

- Summaries merge facts of different sources. Standard Mystcopper Axe Head's summary requires weaponsmithing 40 while one of its recipes needs 10, and Azure Lover's summary gives 1.7% for six containers whose chances reach 38.5%.
- Crafting chains break. Ores say they refine into ingots, but no smelting recipe is published. Some steel recipes produce a mystcopper part, and some parts list identical recipes twice.
- Places include internal area names such as "Generic Autumn Birch Area", which bury the places players know.
- Weapons carry two type vocabularies: the list and title show the item type, such as Rifle, while the filter and details show the weapon type, such as Pistol. Speed sorts alphabetically, Range has no unit and absent values show as blank cells.
- Wording misleads: a guaranteed drop is called rare, a container row adds an unexplained second percentage, an unresolved pickup reads "Find lying", and a bounty note is received from its own quest.
- The missing page lost its styles at nested addresses, a first tap followed links instead of previewing them, and places ran together without spaces. All three are fixed.
- Interaction gaps: filter counts break inside numbers, the desktop filters put 100 controls before the results, the availability toggle drops focus, Show more leaves focus behind the revealed rows, wide tables lose the item name when scrolled, tall tooltips clip at 200% zoom and long tooltip names run under the ribbon's notch.

The same six personas reviewed the real-data preview again after groups 4, 7, 8 and 10, with reports in `artifacts/design/ux-review/personas-2/`. Most first-review findings were fixed. The new findings changed the following:

- A pinned item name covered the leading digits of a damage value scrolled under it at 768 px, so the pinned column draws an edge there, and its heading stays above the other headings.
- An item preview left out the known issue that changes its numbers, such as knee armor that counts only after another piece is equipped, so previews name the issues that affect the item.
- Sources sort by best chance, place groups follow their best source, and gathering, fishing and farming lead the ways found in the world, while random loot, which names no place or chance, comes last.
- Used For counts the uses its filters show and says so when none match, a filter's search field stays while it holds text, focus stays on a control after a filter is added or removed, and a list opened with a category starts with that group closed.
- Phone previews dock away from the tapped link, a list cell without a value reads None, a currency price reads as one phrase, and search excerpts no longer repeat the item name from the game card.

Findings left open, each with its owner:

- Weapon part pages do not explain assembly, compatibility or finished weapon stats, and the part list cannot shortlist by crafting level or ingredient cost. Both belong to `add-crafting-and-equipment`.
- A bounty note does not say where its quest is accepted, because quest givers are not read yet. Places and people in `openspec/plan.md` own it.
- The list's name filter now also matches a name the game misspells by one letter, such as Steel Rapier Gaurd, and a source row names each camp place once, since the camp maps of one terrain share their terrain's name.
- Task 10.8 still holds the staff's Halberd explanation. Settlement delivery now names the weekly trigger and collection screen, and only the game's Speed word is published.

## Reader text

The site's own text reads as machine-written. Four research reviews, twelve player and reviewer personas from two model families, and a comparison with human-written references looked for the cause. The reports, the extracted text, the measurements and sample rewrites are in `artifacts/design/writing/`.

Findings:

- The cause is not chatbot vocabulary, which the voice gate already keeps out. The site has no participle tails and fewer nominalizations and passives than the wikis it was compared with. Instead it narrates what is known, talks about itself and its method, and turns records into sentences. Per 1000 words of the site's full sentences, "known" or "unknown" occurs 7.4 times against at most 0.2 on OSRS, Terraria and bg3.wiki item pages and in Bloodline players' Steam posts. Words for the page or site occur 10.4 times against at most 0.8, "the game" 11.2 times against at most 1.6 and "only" 8.9 times against at most 2.9. Modals such as can and may occur 3.0 times against 9.9 to 21.1. Sentences average 9.1 words with a standard deviation of 5.0, against 11.1 to 17.0 words and 7.6 to 9.4, and 44% open with one of five word pairs, against 8% to 17%.
- Both model families found the same defects. Ten of the twelve reviewers named Home calling the 1,844 items with a known source "All", and nine named unconfirmed records such as zero yields, unlocated gathering and unused dialogue that read as ordinary sources. Eight named summaries that join names and places without pairing them, and eight the passive method prose on About. Six or seven named container spawn notes that do not say how they relate to the chance, item notices that describe a group of items instead of the item, and search descriptions that paste table headings into sentences. Four or five named the building requirement "Requires the completed Smithy.", Currency payments listed as a skill, and quantities such as "1 items".
- Bans on surface signs cannot fix this. Wikipedia's guide to AI writing says the signs are not the problem itself, syntactic templates survive word bans (Shaib et al. 2024), and no study supports banning a list of words as a cure. The voice rules and the agent rules this project inherits are all bans, and two of them favor the signs above. Short factual sentences without hedging gave short uniform sentences whose hedging moved into "is known". Describing the system without describing a person gave passives such as "the game's code that chooses the item is read".
- Model reviewers cannot judge the voice. The same-model reviewer of machine-written text found no basis to attribute the text to a model, the reviewers' own rewrites repeat phrases such as "is not established here", and model judges favor longer text (Saito et al. 2023). Frequent users of language models detect such text reliably (Russell et al. 2025), so a person judges the voice, and models check facts and find defects.
- The first sample rewrites were drafted from these findings and still read as machine-written: convoluted, stiff, and with an unnamed speaker on About. The measurements did not prevent this, because they describe symptoms and do not point to a better sentence, and a second model still preferred the drafts in 11 of 20 pairs. The second drafts started from how large game wikis handle each situation, read on their pages and checked with page counts on the OSRS, Minecraft and Terraria wikis, such as "chance to spawn" on 70, 187 and 92 pages against none for the site's "is there only". They came closer to what the review asked for, and one round of review is the only evidence for that so far. The counts can settle a single phrasing but not the register: "does not" appears on 2,943, 2,564 and 917 pages and "doesn't" on 927, 1,395 and 77, and OSRS writes "you can" on 3,861 pages and "players can" on 4,209. The counts are in `artifacts/design/writing/research/wiki-phrases.md`.

Decided in the review of 2026-10-31, open to change like everything else here:

- Text about the site's own process goes, wherever it appears: what was read, run, reported or confirmed, unless it changes what a player does. A bug report link stays because a player can follow it, and a note that a bug is not reported yet goes.
- The site has no unnamed first-person speaker, such as the "I" of the first drafts, which reads as out of place on a wiki-style reference.

Proposed for the sample review and not decided:

- Reader text starts from the player's question and uses the players' words, not the data model's. A sentence names the thing and what it does, keeps a condition next to the fact it limits, and connects facts with but, so and because. It is as long as its meaning needs. Words that carry a fact, a relation, a condition, an example or a next step stay, and announcements, assurances and repeated facts go.
- An unknown is stated once, where it changes a decision, with its reason. A chance that depends on several factors reads Varies with those factors. The site's method is described once, on About.
- Labels stay terse. One term names one thing on every page. Facts that a table can carry, such as a container's spawn chance, go into the table instead of a sentence.
- Drafts start from how large game wikis word the same situation, in the present tense with the item or mechanic as the subject. Whether the site contracts and whether it addresses the player as you or as the player stays open, because the wikis do both.
- Each source kind gets its own template that is grammatical for every count, keeps each name with its place, and keeps unconfirmed records out of the summary. A notice on an item page is about that item, and the full list of affected items stays on Known Issues.
- A person reviews a sample of rewrites before the full pass, a second model checks only that each rewrite keeps its facts, and the same personas run again on the result. The voice gate keeps checking mechanical defects.
