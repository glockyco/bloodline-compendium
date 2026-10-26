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

A new lab save `LabTooltips` was driven through the file bridge in `C:\BloodlineLab`:
- The character creation button was pressed by calling the function its `ComponentDelegateBindings` entry names.
- Items were added with the pawn's `AddItemsToInventory` and each class's default `ItemInfo`.
- The character sheet was opened with the pawn's `Open/Close Character Sheet`.
- Captures were window-only.

The game fills one hidden `W_Tooltip_QualityWhite` per inventory item, and the widget's text blocks hold the name, the speed or type line, the stat line and the description. Reading them needs no hover and no screenshot.

The game window must stay in front: hidden, the game stops ticking and the bridge stops answering.

Disagreements found:

- Armor has two sources. The inventory, equipment slot and trader tooltip widgets read `ArmorDataTable` (`ArmorValue`, `ArmorClass`). `FirstPersonCharacter2` reads `ArmorInfo.DamageRating` and `Total Armor` and never reads the table. The catalog publishes the class defaults. Of 404 armor items, 47 have no table row. For 329 of the other 357 the ratings differ. Winter Warden Cuirass shows Heavy and 13 in game against Light and 2 in its class defaults.
- Food tooltips show `Heals: {restoration}hp`, while descriptions state other amounts: Apple Pie 15 against 200, Fondue 0 against 300.
- Materials and weapon parts show an empty `: 0` stat line.
- The catalog's "Weapon type" is `WeaponInfo.AttackType`, an attack style. All 19 rifles read Crossbow, and the Lonland Pistol is typed Rifle with weapon type Crossbow.
- Speed shows as a word such as `Slow`. The catalog publishes the multiplier.

Comments in `ItemFacts.cs` cite bytecode by line number in a dump that the repository cannot regenerate.

### What the sibling projects rebuilt

- **Afallon:** started on 2026-09-06.
  - It established a strict relational catalog schema and frozen entity references on 2026-09-14 and 2026-09-18, after 182 commits.
  - It restructured its page model three times in four days. `consolidate-entity-pages` merged records that share a player-facing name, `restructure-detail-pages` gave every kind one fixed question order, and `restructure-page-model` folded product, recipe and recipe-item pages into one.
  - It then needed `complete-evidence-pipeline-architecture` because the documented pipeline was not executable end to end.
  - It later added `crawlable-relation-links` to put relations into static HTML.
  - 255 of its commits mention entities, relations, cross-links or references.
- **Ancient Kingdoms:**
  - `unify-acquisition-usage-requirements` unified acquisition and usage semantics across pages after each page had grown its own.
  - Its citation lock over decompiled text changed in 182 commits.
  - It replaced its Markdown game-bug reports with a typed registry, page notes and a Known Bugs page.
  - It needed one shared number-formatting capability and an item page redesign.
- **Erenshor:** its code-facts spec, matched against decompiled syntax, changed in 45 commits, partly from decompiler output changes.

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

## Decisions

### One entity graph in the catalog

The C# catalog builds an in-memory graph of entities and relation rows and writes it as site data. Each entity has a kind, a key, a display name and, for kinds with pages, a slug. Each relation row has a kind, a source key, a target key and its own attributes, such as price, chance or quantity.

The writer resolves every relation into references embedded in both documents. A reference holds the kind, key, name, slug when the kind has pages, and the icon. The site therefore renders links without joins. A graph check runs in the catalog and again on the built site.

Keys derive from the game object:
- a class path for items and creatures,
- a table and row name for quests, skills, recipes and mounts,
- a level path for places,
- a level and actor path, or a dialogue asset, for people.

Slugs derive from display names under the existing qualification rules. URLs are `/<kind>/<slug>/`, and item URLs keep their current form.

Alternatives:
- A SQLite catalog queried by the site, as Ancient Kingdoms does. It suits a server-rendered site with ad hoc queries. This site is static, and the C# graph with generated types keeps one contract.
- Per-page relation arrays without relation rows, as the item catalog does today. Each direction is then produced separately and can disagree, which is what Ancient Kingdoms unified later.

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

Alternatives:
- Hashing decompiled text lines, as Ancient Kingdoms does. Its lock churned with every source movement.
- Matching decompiled syntax shapes, as Erenshor does. It breaks when the decompiler changes.

### The lab runtime tool

The bridge mod, the host, `shot.py` and `hover.py` move from `artifacts/runtime` into `tools/runtime/`. A small CLI exposes named commands:
- `launch`, `new-save` and `quit`,
- `give`, `equip`, `use` and `open-sheet`,
- `read-tooltips` and `sample`,
- `capture-window` and `capture-model`.

The Lua side keeps one implementation per command. The protocol follows HotRepl's request and response shape, so a UE4SS HotRepl host can replace the file bridge without changing callers.

Runs write to `artifacts/<build>/runtime/<run-id>/`. The tool refuses any target that is not the configured lab copy.

A spike tests whether the game keeps ticking off screen with `-RenderOffScreen`, `-nullrhi` for non-visual checks, or a window placed on a second space. Until then, runs need the game in front, and runs are batched to keep that short.

### Settling disagreements

The order is: trace both code paths through the cross-reference, form a hypothesis, then observe through the game's own functions, repeating each measurement. The ownership table in `verification/game-claims` decides where the result goes.

The armor case runs first: equip two items whose class default and table values differ in opposite directions and read `Total Armor` and mobility. Food runs second: an in-game sampler records Health every frame while a meal is eaten, against a natural regeneration baseline.

### Known issues as typed data

Entries live in `issues/known-issues.json`, validated by a JSON Schema in the repository. The catalog reads the file to attach issue keys to affected facts and to fail on stale references. The site renders notes and the Known Issues page from site data.

How an entry was verified goes in the commit body, not in the entry.

Screenshots are derived from the game, so they live in the build's artifacts and are published at build time like icons. Developer reports are drafted from entries for posting on the game's Discord.

### Presentation foundation

- **Tokens:** one tokens file with closed scales: about nine spacing steps, six type steps, two radii and role-named colours. Stylelint rejects raw values elsewhere.
- **Components:** shared primitives use subgrid where rows must align.
- **Checks:** a fixture page renders each primitive with extreme content. Playwright checks overflow, row alignment, minimum size and contrast at 390, 768 and 1280 px, and axe runs on every page.
- **Voice:** rules sit with the `site/presentation` spec and a banned-phrase list in the repository, checked on built HTML.
- **Item tooltip:** item pages present facts in the game tooltip's order and wording, with Arbutus Slab (SIL OFL) for item names.

The emblem is a single front-facing tome illustration generated from the key art's style. It is the source of the header mark, the favicons and the link preview image.

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

Each item below is its own change with its own specs. Each publishes into the graph, so references to its kind already exist and become links on publication.

1. Finish `publish-item-reference` on this contract:
   - verified armor, food and weapon facts,
   - CI, preview and production deploys.
2. Places and people:
   - levels as places with their merchants,
   - conversation givers,
   - containers and settlement buildings.
3. Creatures and loot, with drop tables.
4. Crafting: smithing with weapon parts and quality, crafting, cooking with ingredient treatments, and farming.
5. Quests, achievements and guilds with their ranks.
6. Skills and spellsongs.
7. Mounts, companions, diseases, calendar events and the arena.
8. The interactive map from level captures and placements.
9. Mechanics guides: experience, combat, armor, crafting quality and settlement income.
10. 3D model renders of items, creatures and people.

## Risks / Trade-offs

- [The graph is designed before most kinds exist] → Only the key, reference and relation shapes are fixed now. Kinds and relation kinds are added by their own changes.
- [Function fingerprints change with CUE4Parse upgrades] → CUE4Parse updates are reviewed separately from game updates, and the report separates the two causes.
- [Runtime checks need the game in front] → Batch runs, and test running off screen early.
- [Typed references make site data larger] → References carry only kind, key, name, slug and icon. The file-size gate catches growth.
- [Known issues published on unconfirmed causes] → An entry requires an observation, and code-derived causes are labelled as derived.

## Migration Plan

The site-data contract changes in one cutover. The catalog writes references, the schema and generated types are regenerated, fixtures are rewritten and the site reads references. Item URLs stay the same, so no redirects are needed. `publish-item-reference` archives after its remaining tasks pass on this contract. Rollback is a revert of the cutover commits.

## Open Questions

- The page palette: plum surfaces from the key art with items in the game's brown tooltip style, or a warm brown theme throughout. Mock-ups decide. Either choice leaves the token roles unchanged.
- The heading typeface beside Arbutus Slab.
