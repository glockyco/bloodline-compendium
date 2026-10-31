# Design

## Context

See proposal.md for motivation. Findings below come from Steam build 24464547 unless stated otherwise.

### What the game holds beyond items

Extraction already writes every DataTable, class default, placed actor and dialogue. The gameplay tables and their row counts:

| Domain | Source | Rows |
|---|---|---:|
| Quests | `DT_UQC_Quests` | 534 |
| Skills | `SkillsDataTable` | 599 |
| Spellsongs | `All_Spellsongs` | 13 |
| Smithing and crafting recipes | `ItemRecipes` | 1,124 |
| Weapon parts | `WeaponComponentTable` | 791 |
| Cooking recipes and effects | `AllFoodRecipes`, `FoodEffectDataTable`, `AllIngredientEffects` | 100, 140, 27 |
| Farming | `SeedDataTable` | 18 |
| Achievements | `AllAchievements_DataTable`, `DT_UQC_Achievements` | 128, 13 |
| Guild ranks | `MagesCollgeRankTable`, `TheChurchOfArosaRankTable` | 3, 10 |
| Mounts | `MountDataTable` | 24 |
| Shops | `DT_AllShops`, plus 90 placed merchants | 20 |
| Settlement buildings | `Building_Construction_Recipes`, `BuildingResourceAndIncomeValues` | 20, 20 |
| Loot | `AllLootSpawnRates`, `PresetLootTables_DT`, `AllLootPresets` | 282, 40, 138 |
| Diseases | `DiseaseDataTable` | 4 |
| Arena | `Preset_Arena_Battles`, `Preset_Arena_Waves_DT` | 8, 13 |
| Calendar events | `CalendarEventInfo`, `PresetCalendarEvents` | 23, 2 |
| Armor values | `ArmorDataTable` | 393 |

Levels (108 read, one too large to load), 607 dialogue assets and 149 creature kinds supply places, people and creatures. The item catalog already names 84 places, 59 people who give items in conversation, 90 merchants and 43 container kinds, all as display text.

### What the running game showed

Lab saves were driven through the bridge in the lab copy. Every observation below came from the game's own functions:
- Menu and character creation buttons were pressed by calling the function their `ComponentDelegateBindings` entry names.
- Items were added with the pawn's `AddItemsToInventory`, passing the item info of an item actor built as the world builds one. The class defaults of 95 items lack their own item class, which only the construction script fills in.
- The character sheet was opened with the pawn's `Open/Close Character Sheet`.
- Items were equipped and eaten by clicking their inventory slot, whose handler calls the slot's `Use`.

An inventory slot builds its tooltip in its own `OnMouseEnter`, so calling that handler gives the text that hovering shows.

The game runs off screen at full rate:
- With `-nullrhi` it has no window and runs Blueprints, widgets and tooltips.
- With `-RenderOffScreen` it renders without a window, and macOS lists it as a background app, so it never takes focus.
- In both modes the bridge answered while other apps stayed in front.
- The game resets `t.MaxFPS` to 0 on every level load, and the first level opens with a tutorial popup that pauses the game.

Armor, measured in runs armor-1, armor-2, armor-slots, armor-slots-2, knees-1 to knees-4, misc-1 and misc-2:
- Damage taken follows the armor table. `FirstPersonCharacter2:TakeDamage` multiplies damage by (1 - Total Armor / 100). `PlayerPassiveLevelStatComponent:Retrieve Player Armor` sums `Get Armor Slot` over the character sheet's torso, head, arms, legs, knee, feet and shoulder slots. `Get Armor Slot` reads `ArmorDataTable` by the item's English name without spaces and apostrophes, uses `ItemInfo.Damage` for crafted armor, and gives 0 when no row matches.
- Feral Maw (class default 5 Medium, table 1 Light) let a 20 point hit through as 19.8. Adding the Winter Warden Cuirass (class default 2 Light, table 13 Heavy) made it 17.2. The player's `ArmorTotal`, which sums the class defaults, changed nothing measured.
- Mobility follows the same table values. `Calculate Effective Armor` lowers each item's value by 1.4% per level of its armor class proficiency, and `Retrieve Player Armor` turns the sum into a speed factor. With the cuirass at heavy armor level 1, walking speed fell from 350 to 307, as the formula gives.
- The item slot named "Back" in item data is the arms slot. Amulets and support items add no armor, and knee armor adds none to mobility.
- Equipping knee armor does not recalculate armor. Broad Knee Guards (table 7) added nothing, also after a sword was equipped, and counted in full once another armor piece was equipped. The knee branch of `FirstPersonCharacter2:Attach Equipment` attaches the guards without calling `Set player Speed based on Armor`, which every other armor branch calls and which recalculates the total through `Retrieve Player Armor`.
- The character sheet's visible `Armor:` line shows `Total Armor`. In render run issues-sheet it read 0 with Leather Pants and 13 after the Winter Warden Cuirass. A second binding, `Get_Stats_2_Text_0`, formats the class-default sum `ArmorTotal` with experience and movement speed.
- Leather Pants (class default 4) and Velosian Plume-helm match no table row. Neither added armor, in two runs each. Leather Pants' tooltip showed its class default, `ARMOR: 4` with an empty type line. The plume helm's tooltip showed `ARMOR: 9` and `Type: Medium`, so the tooltip widgets find rows that `Get Armor Slot` misses.

Food, measured in runs food-a, food-b, food-c, misc-1 and misc-2: eating applies the food's row of `FoodEffectDataTable` through `FoodManager:Get Item`, found by the item's name without spaces and apostrophes and else by its display name. Each effect name selects a function: "Heal Player" adds its amount to Health at once, and buffs such as "Boost Health Regeneration" last their value in seconds. The effect raised regeneration from 1 to 4 Health per second. `PotionInfo`'s restoration amount did not apply:
- Apple Sauce (`PotionInfo` 0, table 75) healed 75, and Roasted Apple (`PotionInfo` 75, table 25) healed 25, in two runs each.
- Apple Pie (`PotionInfo` 15, table 200) and Fondue (`PotionInfo` 0, table 300) healed from 15 and 26 to the maximum of 150 in three runs.
- Druid's Stew lists "Boost Foraging EXP " with a trailing space, which matches no effect. Its foraging boost stayed at 1 after eating, in two runs. In runs foraging-icons-1 and foraging-icons-2 the status bar then showed only Druidsight, while Carrot Cake added a `Foraging EXP Buff` icon with 10:00 left and set the boost to 1.25.
- Wheat Seeds lists "Heal PLayer", which the switch matches because effect names are FNames and compare without regard to case. It healed 5 in runs food-wheat-1 and food-wheat-2.

The tooltip sweep with screenshots (run tooltips-sweep of 2026-10-30) read the shown lines of 2,065 of 2,087 published items and took 261 screenshots, eight tooltips each. The lab finds no inventory slot for the fetch quest item, Sack of Seeds, and hovering 21 more items, such as contracts, bounties, currencies and trade goods, builds no tooltip widget. Hovering all 21 with the real cursor in a game window (run hover-check-2) showed no tooltip either, so players see none. `UI_InventoryItem_2:OnMouseEnter` fills the tooltip:
- The stat line shows `ItemInfo.Damage`. Armor shows instead the `ArmorDataTable` row whose `ArmorName` equals the item's name, as 350 of 398 armor tooltips did.
- Crafted armor writes its armor in place of the description, through `Translate Custom Armor Description`, and shows a lone colon where other tooltips label a stat.
- Potions replace the description with `Translate Potion Data to Description`, which writes nothing for the five published potions. With the real cursor, four of them show only their name, icon and a lone colon, and Base Potion shows no tooltip.

Every screenshot was compared with the lab's reading of the same tooltip, by text recognition and by eye. They agree for every tooltip the screenshots show. The placement in the viewport draws no tooltip for the five potions, Blastfuel and Bladed Mace, and the real cursor showed those with the text the lab read.

The catalog compares every item's facts with the latest shown-only tooltip a run read for it, assigns each difference a cause from `TooltipCheck.cs`, writes them to `tooltip-check.json` and lists differences without a cause in Coverage. On build 24464547 it compared 2,065 tooltips and found 174 differences. 55 have no cause: the weapon damages below.

Headless, bound text such as the character sheet's `Armor:` line is not evaluated and keeps its design-time text, so runs read the values the bindings show instead.

Disagreements found:

- The catalog published armor from the class defaults (`ArmorInfo.DamageRating`, `ItemInfo.ArmorType`), which the game does not apply. Owner: the catalog. It now publishes what `Get Armor Slot` returns: the row's class and value, the class `Custom` with `ItemInfo.Damage` for the six crafted armor classes, and `Unarmored` with no armor when no row matches. Amulets and support items get an armor rating of 0 and no armor type.
- Three published armor items store no `ItemInfo.ItemClass`: Curved Steel Plate Shoes, Leather Pants and Leather Bracers. `Get Armor Slot` gives no armor to an item without a valid class. `BP_BaseItemAsset`'s construction script sets the class on every item actor, so this matters only where the game adds such an item from its class defaults, which no run has shown. Open question.
- 30 published armor pieces for body slots, not counting the six crafted armor classes, match no row of `ArmorDataTable` by the game's lookup, so the game applies no armor for them while their tooltips show `ItemInfo.Damage` or the row whose `ArmorName` is the item's name. Observed for Leather Pants and Velosian Plume-helm. Owner: the game, a known issue. 13 more published items without a row are amulets and support items, which add no armor in any case.
- Knee armor counts only after another armor piece is equipped. Owner: the game, a known issue.
- Druid's Stew's foraging boost does not apply, because its effect name has a trailing space. Owner: the game, a known issue.
- Weapon damage follows the normal attack's applied component. Staff-class weapons take the Damage assigned to the projectile created by their reachable `Staff Attack` entry, or the projectile class default when no assignment overrides it. A missing, partly parsed or unreadable producer leaves damage unknown with a reason. The two Soulstaffs whose inherited normal entry creates no projectile also have unknown normal-hit damage, not their special explosion's damage. The six halberd-style staves retain their melee `WeaponInfo.Damage`. Staff of Minor Flames and Staff of Minor Rot also show a separate Projectile damage fact from their reachable `Swing Event`. The Bugs section names the tooltip's damage where it matches no damage that a normal attack applies, so an unknown applied damage, or a tooltip that shows a separate projectile's damage, is no difference. The catalog reads executable records, not lab measurements. [The attack-mode and producer evidence](../../../artifacts/design/game/task-10-8.md#5-attack-mode-range-and-complete-projectile-producer-follow-up) separates normal initialized damage from the close-range creation-overlap defect and records which values were measured.
- The five published potions' tooltips show no description, Base Potion shows no tooltip, and potions and crafted armor show a lone colon. Owner: the game, as the causes `potion-tooltip-text` and `unlabeled-stat-line`, which are not known issues because they change nothing a player can do. Item pages show the description from the item's data.
- Food tooltips fill a heal line with `PotionInfo`'s restoration amount but keep it in a collapsed box, so a player sees only the name, icon and description. Run hover-check showed this for eleven items with the real cursor in a game window, and the lab's own placement of the same tooltips matched it. Earlier lab readings took text in collapsed boxes for shown text, which made the line look like a tooltip defect. Owner: the lab tool. `texts()` now treats text inside a collapsed or hidden panel as hidden, tooltip batches mark such readings `shownOnly`, and the catalog uses no others.
- 74 published foods have no effect when eaten, because `FoodEffectDataTable` has no row for them by the game's lookup. Most are ingredients, seeds and fish whose descriptions promise nothing. Steak, Soup, Turkey Leg, Cooked Red Wrangler, Red Wrangler Sandwich and Potato Seeds promise healing and were used up without any heal in runs issues-steps-4 to issues-steps-7, while Roasted Apple healed 25. Owner: the game, a known issue for these six. Soggy Sandwich and Sweet Bun promise a fishing boost and army morale, which no run observed, and their status rows stayed empty. The catalog publishes `Effects: None` for all 74.
- Materials and weapon parts show no stat line without a label. They keep it in a collapsed box, as food does, in runs hover-check and tooltip-spike. Owner: the lab tool, as above.
- The catalog's "Weapon type" was `WeaponInfo.AttackType`, an attack style. All 19 rifles read Crossbow, and the Lonland Pistol is typed Rifle with weapon type Crossbow. Owner: the catalog. It now publishes `ItemInfo.WeaponType`, which equipping and the ranged weapon checks read and which names the Lonland Pistol a Pistol and the Lonland Rifle a Gun. A weapon whose weapon type is None, as 33 are, most of them shields, shows its specific item type instead.
- Six flintlock pistols, the Lonland Pistol among them, have the specific item type Rifle and the weapon type Pistol. The player's attack fires a gun only for the weapon types Gun and Pistol, and the specific item type picks between firing branches inside it, so pages show the weapon type. Owner: the catalog.
- The player carries two actors for each equipped head item. Swapping helmets in a fresh save kept two, so equipping does not accumulate actors in normal play. The five Winter Warden Cuirass actors seen earlier followed repeated equips of copies of an item that was already equipped, as `capture-model` did. That case was not repeated.
- Speed shows as a word such as `Slow`, which is the display name of `ItemInfo.Speed`, an `AttackSpeeds` enum: Slow, Below Average, Average, Above Average, Fast or Standard. All seven flintlock pistols read Slow, as their tooltips do. Owner: the catalog, which publishes the word as Speed and keeps the `WeaponInfo.AttackSpeed` multiplier of the attack montage as Attack speed.

### Character progression in the game data

Read from the widget and struct assets of build 24464547 with the design harness, then confirmed in lab run progression-1 where stated.

- Skill trees are widget data. Each of 21 disciplines has a tree widget whose slots name the skill class, the required discipline level (`Required Level`, `Level To Check`) and passive buttons with their names, descriptions and required proficiency levels. Pyromancy's passive Burning Efficiency, for example, needs proficiency 85 and reduces pyromancy mana cost by 3.5% per rank.
- Players see the level-based trees (`*LBSkillTree`). The skill window offers four categories, Magic, Melee, Ranged and Tech, and every discipline button opens its level-based tree, the Magic menu's Biomancy button the Arcane tree. The older trees with skill point prerequisites are not reachable from the menu (run progression-1).
- Points, observed in run progression-1. The game instance holds them, and the passive stat component holds the levels:
  - A character level grants 1 skill point and 3 level up points.
  - Unlocking a skill in a tree costs 1 skill point and needs the slot's discipline level, as Ember Flick did at Pyromancy 1.
  - Level up points raise attributes by 1 each in the level up window, which the radial menu's Level Up opens through `PlayerPassiveLevelStatComponent:Level Up`. Its decrease buttons refund them. One point of Fortitude raised Max Health from 150 to 175.
  - A discipline rises with its own experience, such as `Pyromancy Level Tracking`, which casting calls. Pyromancy needed 152 experience at level 1 and 156 at level 2, and 50 experience given counted as 51.
  - Every discipline level grants 1 proficiency point of that discipline. A passive rank costs the passive's `Point Cost` in those points and needs its required discipline level, up to its `Max MultiLevel` ranks: Blazing Fire cost 1 at Pyromancy 2, and a second click without points did nothing.
- Armor proficiencies rise through `Damage Armor EXP Tracking` and the armor class's level tracking, not yet observed.
- The disciplines are the magic schools Pyromancy, Cryomancy, Electromancy, Geomancy, Aeromancy, Arcane, Blood Magic, Necromancy, Druidism and Vampirism, the weapon disciplines Dagger, One-Handed, Two-Handed, Spear, Staff, Shield and Unarmed, and Bow, Flintlock, Lute and Utility Gadgets.
- `SkillsDataTable` holds 599 skills with mana cost, cooldown, cast time and a skill class. Its tab column groups them as Magic 371, Blades 145, Ranged 45, Gadgets 17 and Leadership 9.
- `PlayerStatLevels` names 12 attributes: Fortitude, Wisdom, Dexterity, Strength, Constitution, Vitality, Intelligence, Perception, Agility, Reflexes, Acrobatics and Willpower.
- The lab save also stores levels for Endurance, Focus Crafting, and Light, Medium and Heavy Armor with their EXP. So the armor type decides which armor proficiency a player trains.
- Calling the game's `Set Required EXP` matched the experience formula read from bytecode at 14 levels in an earlier spike.
- The player has 11 equipment slots. Further layers are armor runes in three tiers with an attunement table, enchantments with melee trigger types, grapple mods, mounts and companions.
- The save stores inventory items by class path, such as `/Game/BloodlinesAlpha/Interactables/Crafting_Items/Apple.Apple_C`. It also stores attribute and proficiency levels, the character level, completed achievements and the rolled calendar events.

### What the sibling projects rebuilt

- Afallon started on 2026-09-06.
  - It established a strict relational catalog schema and frozen entity references on 2026-09-14 and 2026-09-18, after 182 commits.
  - It restructured its page model three times in four days. `consolidate-entity-pages` merged records that share a player-facing name, `restructure-detail-pages` gave every kind one fixed question order, and `restructure-page-model` folded product, recipe and recipe-item pages into one.
  - It then needed `complete-evidence-pipeline-architecture` because the documented pipeline was not executable end to end.
  - It later added `crawlable-relation-links` to put relations into static HTML.
  - 255 of its commits mention entities, relations, cross-links or references.
- Ancient Kingdoms:
  - `unify-acquisition-usage-requirements` unified acquisition and usage semantics across pages after each page had grown its own.
  - Its citation lock over decompiled text changed in 182 commits.
  - It replaced its Markdown game-bug reports with a typed registry, page notes and a Known Bugs page.
  - It needed one shared number-formatting capability and an item page redesign.
- Erenshor's code-facts spec, matched against decompiled syntax, changed in 45 commits, partly from decompiler output changes.

### Features the siblings built or planned

| Feature | Sibling evidence | What it needed underneath |
|---|---|---|
| Character planner | Afallon proposed it on 2026-09-28. Talent and gear planning were split into separate changes on 2026-10-03. The planner payload, compressed build links and the multi-character store landed on 2026-10-11. `build-character-planner` still has 15 of 23 tasks open, `plan-character-gear` 21 of 21 and `plan-party` 31 of 31. | A route-scoped payload of trees, nodes, ranks, costs and conditions, separate from page documents. One tested evaluator (`apps/site/src/lib/planner/talents.ts`). Build links in a versioned compressed fragment that refer to keys. Characters kept in the browser. The active character adds cues to reference pages (`page-context.ts`), and level controls are shared. |
| Combat planner | Ancient Kingdoms held 213 mutually blocking tasks in `add-gear-and-rotation-planner` and `add-combat-verification-harness` before `rebuild-combat-model` (2026-09-18). It retired the engine on 2026-10-16: the engine "produced no results the site could publish, yet every game update cost fixture refreshes". | A simulator and fixture characters measured in game. It was never finished. |
| Calculators and formulas | Ancient Kingdoms' `unify-formula-presentation` (2026-10-13) replaced five formula notations that had drifted from each other. Afallon has `add-kill-calculator` and `shared-level-control`. | One calculation model with worded steps, evaluated both when pages prerender and in the browser. Shared remembered character inputs. |
| Entity tooltips | Afallon's `compendium-tooltips` spec. | Tooltip data for each kind, shown on hover and keyboard focus with the active character's context. |
| Search over every kind | Ancient Kingdoms added global search (`add-global-entity-search`, 2026-09-27), then specified entity registries a day later. | A registry of kinds that feeds search, the sitemap, artwork and page metadata. |
| Requirements and seasons | Ancient Kingdoms retrofitted seasonal availability (`integrate-seasonal-content`, 2026-10-25) after season facts had spread over booleans and sentinel values, and unified requirements in `unify-acquisition-usage-requirements` (2026-10-18). | Typed conditions on every source, use and unlock. |
| Stat names and numbers | Ancient Kingdoms' `unify-stat-vocabulary` (2026-10-17) and `unify-number-formatting` (2026-10-25). | One stat vocabulary and one number format boundary. |
| Interactive map | Both siblings. Afallon kept placements off relation rows (2026-09-18). Ancient Kingdoms plans links from lists to the map. | Placements keyed by entity, published separately from page documents, with map state in the URL. |
| Save progress | Afallon's `show-save-progress` (25 of 25 tasks open). | Keys that match the identifiers saves store. Reading the save in the browser. |
| Changes per update | Afallon's `show-update-changes` (18 of 18 tasks open). | Stable keys and a comparison of facts between builds, linked to the patch notes. |
| Known bugs | Ancient Kingdoms' `add-known-bugs-page`. | Covered by `site/known-issues`. |

## Goals / Non-Goals

**Goals:**

- A contract that every later content kind publishes into without changing existing pages.
- Facts that state what in the game they rest on, and a cheap way to recheck them after an update.
- Runtime checks that scale to every item and run without hand-written scripts.
- One place for each kind of finding, so understanding survives between sessions.

**Non-Goals:**

- Publishing pages for kinds other than items. Each kind arrives in its own change, in the order below.
- A general interpreter for Blueprint code. Calling the game's own functions tests behaviour more cheaply.
- Running checks in CI. CI has no game files.
- A combat simulator or damage optimizer. Ancient Kingdoms retired one that never produced a publishable result and cost work on every update.

## Decisions

### One entity graph in the catalog

The C# catalog builds an in-memory graph of entities and relation rows and writes it as site data. Each entity has a kind, a key, a display name and, for kinds with pages, a slug. Each relation row has a kind, a source key, a target key and its own attributes, such as price, chance or quantity.

The writer resolves every relation into references embedded in both documents. A reference holds the kind, key, name, slug when the kind has pages, and the icon. The site therefore renders links without joins. A graph check runs in the catalog and again on the built site.

`CatalogGraph` turns each acquisition source into one row from its provider to its item. Ingredient and currency uses become separate rows from the consumed item to the acquired item. Item source and use lists and the graph's reverse documents project those rows, so they cannot compute contradictory directions. Mechanism attributes remain on the row. Embedded references contain only identity and presentation fields.

`graph.json` retains documents for reference-only entities as well as items. The publication gate reads the prerendered graph and requires each paged document's relation ID and direction in static HTML. It also rejects invalid keys, slugs and page decisions. `EntityLink` reads the published kind registry before creating a link.

Relation rows carry typed conditions where the game sets them: a calendar event or season, a quest or quest stage, a guild rank, a discipline or proficiency level, a settlement building, or another named game state. A condition the catalog cannot read is published as unknown, never as absent. Pages, the map and the planner read the same conditions, which Ancient Kingdoms had to retrofit for seasons.

Build 24464547's Woodhurst level keeps actors tagged `Halloween Decor` only when the persisted game calendar month is Truill or the persisted real-world month is October. `Woodhurst_C:ExecuteUbergraph_Woodhurst` takes the OR of those checks and otherwise destroys the tagged actors. The Hallowed Eve Merchant has that tag. `CheckIfIsHallowedEve:IsConditionMet` repeats the same check. The source therefore carries a calendar-event condition rather than inferring availability from the merchant's name. The build's code index lists both functions with their fingerprints. Hallowed Eve has no row in the extracted calendar tables, so its reference has a label without a key and appears in coverage.

Dialogue requirements are read from their owning nodes. When an ancestor's check can gate a grant through alternative paths, the catalog publishes an unknown requirement rather than joining the paths into a false conjunction. Unsupported condition classes and missing thresholds also remain unknown and appear in coverage.

Reference-only settlement buildings retain the identifiers of their producer. Stock uses the finished actor class, while deliveries use the resource-and-income table row. For example, Alchemist and Alchemist (settlement) remain separate graph identities. Consolidating them needs an authored association, not display-name matching, before building pages can publish one player-facing identity.

Keys are the identifiers the game itself uses, so saves, build links and update comparisons can refer to them:
- a class path for items, skills and creatures, which is also what saves store,
- a table and row name for quests, recipes and mounts,
- a level path for places,
- a level and actor path, or a dialogue asset, for people.

Keys never derive from display names.

Two fresh extractions and catalog builds of 24464547 produced the same 3,488 unique kind-and-key identities. All 2,087 item slugs matched the site-data snapshot taken before cataloging. The six held-item class paths in the copied `LabRender.sav` matched item keys, including the Winter Warden Cuirass.

Placements in levels are published apart from page documents, as the map's own data keyed by entity. Pages show counts and places, not coordinate lists. Afallon moved placements off its relation rows for the same reason.

A registry of kinds lists each kind once with its page decision, route, search filter and structured-data type. Routes, Pagefind filters, the sitemap and page metadata all read it.

Slugs derive from display names under the existing qualification rules. URLs are `/<kind>/<slug>/`, and item URLs keep their current form.

Alternatives:
- A SQLite catalog queried by the site, as Ancient Kingdoms does. It suits a server-rendered site with ad hoc queries. This site is static, and the C# graph with generated types keeps one contract.
- Per-page relation arrays without relation rows. Each direction would then be produced separately and could disagree, which is what Ancient Kingdoms unified later.

### Page kinds follow what a player thinks of

A kind gets pages when a player looks it up by name. Records with one player-facing identity become one entity, such as an NPC placed in several levels. A record that always pairs one-to-one with another folds into it, such as a recipe into its product.

Reference-only kinds still get keys, so a later change can promote them without changing references. Kinds and their page decisions are listed in a registry in the catalog, and the graph check reads it.

### One detail page structure

Every kind's page answers its questions in a fixed order:
1. identity and the game's own tooltip or summary,
2. how to get or reach it,
3. what it is used for or gives,
4. where it is,
5. the rules that govern it,
6. its gaps.

Pages use the shared entity header, entity link, relation table and issue note components. Relations render into static HTML, so search engines and Pagefind see them.

### Claims and citations by function fingerprint

The catalog marks each fact as extracted, formula, observed or unknown. Facts whose meaning depends on game code cite the consuming functions as `Package:Function`. The fingerprint is a hash of the function's parsed expression tree with object references reduced to paths, so a rebuild of the package without logic changes keeps it.

A per-build cross-reference lists, for each property and table, the functions and widgets that read or write it. It is built from the same parse and written to the build's artifacts. Writing a citation starts from that cross-reference, and the update report lists citations whose fingerprints changed. The line-number comments in `ItemFacts.cs` are replaced.

Extraction enables CUE4Parse script reading for all registered Blueprint classes and widget Blueprint classes and the Blueprint functions exported by worlds. No `/Game/` folder is excluded. Build 24464547 produces 57,869 functions in 4,532 packages, including `/Game/BloodlinesAlpha`, `/Game/UI`, `/Game/fantasy_gui_4`, the skill system, AI, animations, cities and the overworld.

The index attributes an event stub's accesses and calls to its named entry in `ExecuteUbergraph_<Class>`. It follows jumps, conditional branches, the execution-flow stack and latent action resume offsets. Array element type pointers are metadata, not value reads. References retain full object and property paths. Fingerprints hash the reached expression graph with traversal positions instead of byte addresses. Context skip lengths and latent action IDs are normalized without discarding their expression structure. Vector constants retain their components.

The reference kinds are class and instance properties, struct fields, DataTables, table row structs and referenced objects. Function locals, compiler temporaries and parameters stay in the reached code but are excluded from the state cross-reference. Their scope metadata does not affect the fingerprint. Two full extractions produced identical fingerprints for all 57,869 functions. The invented changed-code fixture changes its fingerprint, while moving an unrelated event does not.

The baseline extraction took 432.05 seconds and peaked at 2,675,228,672 sampled bytes. The first full-export index took 680.19 seconds and 8,227,684,352 bytes, exceeding the memory target.

Loading only function exports, writing canonical entries one at a time and excluding local references reduced the final extraction to 551.47 seconds and 4,711,219,200 sampled bytes. That is 1.28 times the baseline duration and 1.76 times its sampled memory. The final index holds 52,797 references in 139,544,955 bytes, down from 538,111,729 bytes. Measurements, fingerprint comparisons and byte-comparison results stay in `artifacts/24464547/`.

Unresolved computed entry points, unread execution targets, unsupported tokens and execution-flow states that exceed the analysis bound are marked partial. Raw ubergraphs have no fixed entry and cannot be cited as a substitute for an event. Virtual dispatch retains the unresolved function name rather than inventing a receiver class. The index does not interpret native functions or infer runtime branch outcomes.

Item facts keep claim metadata inside the catalog. `item-claims.json` beside `site/` records each published item's identity fields and stats, the build, input fields, claim type and consuming function fingerprints. Citations use the current structs and shared player, item, crafting and tooltip consumers rather than every skill that copies a field. The catalog rejects missing and partly parsed cited functions with the item and fact named. The site contract is unchanged.

Directly extracted fields can have no value-reading consumer. The reagent treatment powers and durations are such fields in this build. `PotionCrafting:Craft Potion` writes them into new item structs, but that does not establish an applied gameplay effect. Their claims retain the extracted inputs without inventing a consuming citation. Armor and food retain their current extracted sources here. Their applied meanings are settled by the later runtime tasks.

The `compare` command reads the previous build's claim report and the current build's code index, so review can start before the new catalog accepts its citations. Its report groups changed or removed citations with every item fact that used them. Invented fixture builds exercise the command, and missing and partly parsed consumer fixtures exercise catalog rejection. With publication provenance held constant, all 2,090 real-data JSON files are byte-identical with and without claim collection. The 871 published icons also match the pre-change byte hashes.

Alternatives:
- Hashing decompiled text lines, as Ancient Kingdoms does. Its lock churned with every source movement.
- Matching decompiled syntax shapes, as Erenshor does. It breaks when the decompiler changes.

### The lab runtime tool

`tools/runtime/lab.mjs` and the Lua commands in `tools/runtime/commands/` drive the lab copy. `.agent/skills/runtime-checks/SKILL.md` gives the procedure.

- `prepare` makes the lab copy for the installed Steam build as an APFS clone, which takes no space.
- It installs UE4SS from an archive pinned by SHA-256, with only the bridge mod enabled.
- A file bridge carries each request, and Lua runs it on the game thread. Requests can reply after several frames, which per-frame sampling needs.
- Every request sends a shared prelude and all commands, so one command can run another at a set time.
- The protocol follows HotRepl's request and response shape, so a UE4SS HotRepl host can replace the file bridge without changing callers.

The game runs off screen, headless or rendering, so the machine stays free to use (see "What the running game showed"). Window capture and window automation are not used. Screenshots come from the engine's own `Shot` command. Off screen, the engine writes those images with alpha 0 wherever the scene is drawn and the colours intact, so the tool drops the alpha channel.

Runs write to `artifacts/<build>/runtime/<run-id>/`, with the build, the commit, a hash of the bridge and commands, and every command with its arguments and result. A command that does something a player cannot names it in a `-- lab setup:` line, which the run records with each use. The tool refuses a lab directory that overlaps the Steam library or lies outside the bottle's C: drive, and it stops only the process it started.

Alternatives:
- Keeping the window in front, as the first spike did. It took the screen for every run.
- Clicking by screen position. Button bindings name the function a click calls.

### Presenting item models

An item render shows the item alone, with a transparent background, as the game shows it in use. Erenshor's renders, which use the same split, set the pattern.

- Worn items are seen three quarters from the front: the camera stands 25 degrees to the subject's left and 12 degrees above it.
- Weapons and shields are seen face-on and upright with the grip down, turned 15 degrees so their thickness shows.
- Firearms and crossbows lie on their side with the muzzle to the right. A crossbow's thinnest axis is its height, so it shows from above with its limbs.
- The camera has a 25-degree field of view, and the picture is cropped to the subject with a margin of 5% of its longer side.

Colours come from the game's own picture of equipment: the character sheet's preview, `3DMeshCapture_BP`. It is a lit diorama far above each level, with the level's Ultra Dynamic Sky and a point light. Its capture stores scene colour without tonemapping, and the sheet shows that unchanged. A render of the preview's own view with the same capture source matched the sheet. So renders are taken in that diorama, with the same source, at noon in clear weather, which `set-time` and `set-weather` set through the sky's and the weather's own setters. The preview follows the day, so the hour is fixed.

What the game draws for an item decides the subject:
- The body meshes that armor puts on come from its class default `ArmorInfo`: one mesh, or left and right meshes with their upper parts. `ItemInfo.SkeletalMesh` is a leftover. For the Winter Warden Cuirass it names `SK_Chr_Torso_Male_13`, while the game draws `SK_Chr_Torso_Male_25`.
- Each body mesh has a female counterpart of the same name with `Female` for `Male`.
- Helmets name no body mesh. The game attaches the item's own actor at the socket that `ArmorInfo.AttachPoint` names.
- Weapons are the item's own actor, carried at a socket.
- The game colours body parts at runtime through material instances of Synty's `ModularFantasyHeroCharacters` with leather, metal, primary and skin colour parameters. Item data names no colours, so the item is equipped and the preview's parts are rendered in place.
- Weapons are copied with their materials, so they can face the camera whatever socket holds them.
- Views start from the way the preview character faces, its mesh's local +Y. The preview turns the character 27 degrees from its camera, and a view taken from the camera's direction turned each subject differently.

The subject's pixels come from a second capture of device depth, which is 0 only where nothing was drawn. Both captures are rendered at twice the output size and combined into smooth edges. A normal-buffer mask failed: it stores raw world normals, which read 0 on faces turned away from the world axes.

Alternatives:
- Own studio lights on a separate lighting channel. They gave consistent but invented colours.
- Rendering the mesh named in item data. It showed the wrong model.
- Rendering the player's own parts. First-person flags and the current pose broke them.

### Settling disagreements

The order is: trace both code paths through the cross-reference, form a hypothesis, then observe through the game's own functions, repeating a measurement whose result can vary. The ownership table in `verification/game-claims` decides where the result goes.

The armor case runs first: equip two items whose class default and table values differ in opposite directions and read `Total Armor` and mobility. Food runs second: an in-game sampler records Health every frame while a meal is eaten, against a natural regeneration baseline.

### Known issues as typed data

Entries live in `issues/known-issues.json`. Its JSON Schema, `issues/known-issues.schema.json`, is generated from the C# `KnownIssue` type like the site-data schema, and the catalog reads the file with the same type, so an unknown or missing field fails. The catalog also rejects duplicate IDs, entries without steps, citations or a confirmed build, and citations of functions that the build's code index lacks or did not parse completely.

Notes reach pages in two ways. Tooltip causes in `TooltipCheck.cs` that are game defects name their entry, and a difference of such a cause notes its page when it concerns a value the page shows, so an empty stat line notes nothing. Defects that no tooltip shows, such as knee armor and ignored food effects, have detectors in `KnownIssues.cs`. A note or cause naming a missing entry fails the catalog. The catalog publishes `issues.json` with the pages that note each entry, and the site renders the notes with the shared issue note and lists every entry on the Known Issues page.

How an entry was verified goes in the commit body, not in the entry. `scripts/issue-report.mjs` drafts a developer report for the game's Discord from an entry, so a draft cannot drift from its entry.

### Presentation foundation

- One tokens file holds closed scales: about nine spacing steps, six type steps, two radii and role-named colours. Stylelint rejects raw values elsewhere.
- Shared primitives use subgrid where rows must align.
- A fixture page renders each primitive with extreme content. Playwright checks overflow, row alignment, minimum size and contrast at 390, 768 and 1280 px, and axe runs on every page.
- Voice rules sit with the `site/presentation` spec and a banned-phrase list in the repository, checked on built HTML.
- Item pages present facts in the game tooltip's order and wording, with Arbutus Slab (SIL OFL) for item names.
- Pages keep the plum night surfaces sampled from the key art, and item tooltips use the game's brown panel, bronze frame and cream ribbon. Mock-ups of both options on the home and Lonland Pistol pages (`artifacts/design/palette/`) showed that a warm brown page made the tooltip blend into it and left a muddy seam under the lavender hero art, while on plum the tooltip reads as an object from the game.

The emblem is a single front-facing tome illustration generated from the key art's style. It is the source of the header mark, the favicons and the link preview image.

### Rules implemented once in the site

Every game rule a reader can recompute is implemented once, in TypeScript, as a function with typed inputs, typed outputs and worded steps. Examples are the experience requirement, the attack speed word, weapon part totals, crafting quality and armor totals.

The same function produces the values pages prerender, the formulas mechanics pages show, the calculators and the planner. The catalog publishes the inputs. A rule that depends on an unknown input returns unknown.

Rules are tested in two ways:
- Invented fixtures in Git cover their behaviour, because game-derived values may not enter the repository.
- A local check compares each rule with vectors recorded by calling the game's own function in the lab with chosen inputs. The vectors live in the build's artifacts.

Each rule cites the game functions it mirrors, so a changed fingerprint flags it on update.

Alternative: rule code in the C# catalog. The browser could then not recompute rules for a reader's inputs, and a second copy would drift, as Ancient Kingdoms' five formula notations did.

### Stats and numbers

One stat vocabulary maps each game stat to its reader name, unit and number format, using the game's own terms. Tooltips, tables, filters, formulas, calculators and the planner read it, and all numbers pass through one format boundary.

### Entity tooltips

Every entity link shows that entity's tooltip on hover and keyboard focus, in its kind's game-familiar style. The item tooltip of group 8 is the first. Tooltip data loads per entity without loading the target page, and the link works without JavaScript.

### The planner covers what the game enforces

The planner lets a reader plan a character against the game's own trees, unlock levels and rules. It builds on the foundation above and arrives early, in the order below.

It plans what the game enforces and computes only what verified rules compute:
- discipline and attribute levels,
- the skills and passive ranks they unlock,
- points and their costs,
- equipment, crafted weapons, runes and enchantments, with armor and mobility totals.

It does not simulate fights. A rule or condition the catalog does not know stays visible as a gap, as in Afallon's planner.

Constraints that apply from the start:
- Build links are versioned, refer to keys, are compressed into the address's query, so that link previews can show the build, and have size limits.
- Characters are kept in the browser, and shared builds open as previews.
- Planner data is a route-scoped payload with its own size budget, separate from page documents.
- Reference pages prerender neutral content. The active character adds cues in the browser, such as the discipline level a skill needs against the character's level. Search engines and readers without a character see the same pages.
- Planning is split by domain, skills first and equipment second, each its own change, because Afallon's single planner plan had to be split.

### Where findings live

Session history is not a record. Each finding goes where the next reader looks:

| Finding | Home |
|---|---|
| Why an approach was chosen, and evidence that shaped it | This change's or a later change's `design.md` |
| Open work | The active change's `tasks.md` |
| Required behaviour | Spec deltas, then `openspec/specs/` |
| A game defect | `issues/known-issues.json` |
| A gap or unexplained disagreement | Catalog coverage |
| Why a commit exists, and how something was verified | The commit body |
| Dated decisions, progress and the next step | The vault note `Projects/The Bloodline Compendium.md` |
| Captures, run outputs and research dumps | `artifacts/`, named by build, referenced from the design that uses them |

A session that learns something writes it to its home before it ends.

### Order of later changes

`openspec/plan.md` holds the order of later changes and what crosses them. Each later change publishes into the graph, so references to its kind already exist and become links on publication.

## Risks / Trade-offs

- The graph is designed before most kinds exist. Only the key, reference, condition and relation shapes are fixed now, and each later change adds its own kinds and relation kinds.
- Function fingerprints change with CUE4Parse upgrades. CUE4Parse updates are reviewed separately from game updates, and the report separates the two causes.
- The lab tool runs the game through CrossOver on macOS only, and model renders depend on the character sheet's preview. A game update that changes the preview changes every render.
- Typed references make site data larger. References carry only kind, key, name, slug and icon, and the file-size gate catches growth.
- Known issues could be published on unconfirmed causes. An entry requires an observation, and code-derived causes are labelled as derived.
- The planner could grow into one change that never closes, as in both siblings. It is split by domain, it plans only what the game enforces, and unknown rules stay gaps.
- Recorded rule vectors could go stale when a game update changes a rule. The update report lists rules whose cited functions changed, and their vectors are recorded again.

## Migration Plan

The site-data contract changes in one cutover. The catalog writes references, the schema and generated types are regenerated, fixtures are rewritten and the site reads references. Item URLs stay the same, so no redirects are needed. `publish-item-reference` archives after its remaining tasks pass on this contract. Rollback is a revert of the cutover commits.

## Open Questions

- The heading typeface beside Arbutus Slab.
