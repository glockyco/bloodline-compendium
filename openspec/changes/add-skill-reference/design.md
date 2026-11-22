# Design

## Context

Evidence comes from build 24464547: the extracted records, the code index in `records/code/index.json`, and lab runs under `artifacts/24464547/runtime/`. Each finding names its run.

### How the game presents progression

- The skill window opens four categories, Magic, Melee, Ranged and Tech, and each category's menu opens trees. Each of the 21 level-based trees is created by one button handler of `SkillTree_Subcategories_MAGIC`, `_Melee`, `_RANGED` or `_GADGETS` (code index). The button named Biomancy reads "Arcane" and opens `ArcaneLBSkillTree`. 21 older trees drawn with `ConnectionLine` widgets, such as `PyromancySkillTree`, are referenced only by themselves, so no player reaches them.
- A tree window shows a header with the discipline level, skill points and the discipline's proficiency points. Its left panel lists skills in rows "<Discipline> Level N:", and its right panel, "<Discipline> Proficiency Tree", lists passives in rows "Proficiency Level N:". Ranked passives show rank pips. Nothing connects the nodes (run treeshots1, screenshots of all 21 trees). Lute shows "You can find spellsongs hidden throughout the world!" over 13 cards that read "???" until learned.
- The live trees hold 327 skill slots, each with a different skill, and 138 passive buttons (run skilltrees1). The records hold the same slots and passives with the same values (extraction since commit `da42013`, compared field by field with skilltrees1).
- A slot names its skill class, `Required Level` and `Level To Check`, the discipline whose level it checks. Roanan's Call in the Druid tree checks Unarmed level 1. A passive names `Passive Name`, `Passive Description`, `Required Level`, `Level To Check`, `Point Cost`, `Point Increase Per Level`, `MultiLevel`, `Max MultiLevel` and `MultiLevel Passive Name`.

### Measured rules

- Unlocking a skill costs 1 skill point. Pressing it again refunds the point (run passives1).
- Rank k of a ranked passive costs `Point Cost + (k - 1) x Point Increase Per Level`, up to `Max MultiLevel` ranks: Lasting Servitude cost 2, 4, 6, 8 and 10, Lingering Souls 1 to 5. A single-rank passive charges `Point Cost` once (run passives1).
- A discipline level grants 1 proficiency point of that discipline, Necromancy 2 (run grants1). Necromancy's tracking adds one besides `Add Proficiency Points`.
- Each Blood Magic level up sets the Blood Magic points to the game instance's `Blood Level` plus 1, whatever was spent. `Blood Level` is the "Blood Levels" option of the video settings window, which `SettingsWindow1`'s Blood selection writes and enemies read to bleed. In run rowgate1 the window's own box chose "No blood", "A bit of blood", "Default Amount" and "BLOODBATH", and the next level up left 2, 3, 4 and 6 points. After spending 2 of 3 points, a level up restored 3. Blood pool rituals and necromancer shrines also grant points through `Grant Bloodmagic Proficiency Point` and `Grant Necromancy Proficiency Point`.
- Staff and Vampirism level ups grant no points after the first. Neither tree has passives.
- A new character starts every discipline, and every profession the game tracks, at level 1, with 0 skill points and 0 proficiency points (run facts1).
- L opens the skill window, and Tab opens the radial menu, whose Skill Tree button does the same: `IMC_InGameActions` maps L to `IA_OpenCloseSkillTree` and Tab to `IA_OpenCloseRadialMenu` (run facts1, `tools/runtime/checks/input-keys.lua`). The menu draws "Skill Tree" with an L beside it (run radial1).

### Where skills come from

- The skills table holds 599 rows for 593 classes. Three classes have two rows, three rows name no class, six names belong to two classes, and the tree skill Carrion Flock has no row.
- 34 placed `SkillBook_InteractionObject` actors teach their "Skill to Learn" ("Press to learn skill"), 3 of them tree skills. 3 dialogue `UnlockSkill_C` events add a skill. Code grants others, such as Feed in `DiseaseManager`'s "Become Vampire Event".
- 229 skill classes have no reference besides the table and the older trees: 96 sit only in older trees, and 133 appear nowhere else.
- 13 spellsongs are rows of `All_Spellsongs`, keyed by their tune, with name, description, charges and experience. 11 placed `Spellsong_Notesheet_Object` actors and two other interaction objects name a spellsong.

### What the catalog and the site have

`CatalogBuilder.Build` runs item selection, facts, icons, sources, statuses, gaps, tooltip comparison, known issues, versions, the graph and the item list in one method. `ISourceMechanism` returns `ItemSource` rows keyed by item class. Claims, statuses, the maintainer report and the site data writer are item types. On the site, `routes/items/+page.svelte` holds the whole list interface, `ItemVersion.svelte` the page, `previews.ts` registers the item tooltip only, and the header and home page name items. The specs for the kind registry, references, relations and the tooltip presenter are already written for any kind.

## Goals / Non-Goals

**Goals:**

- Every kind passes through the same producer, source, status, coverage, claim, list, page and tooltip structure, so a kind adds what is its own and nothing else.
- Discipline pages read as the game's tree windows, so a player finds a skill where the game puts it.
- Every skill the game defines is published with its known sources or its status.
- Every published progression number traces to a record, a cited function or a named lab run.

**Non-Goals:**

- The planner and its rules for character levels, attributes and allocation, which `add-skill-planner` builds.
- Computing what a passive or skill does beyond the numbers the game stores. Descriptions stay game text.
- Pages for places and people. Skill books and note sheets name their level as unlinked text.
- The older trees as pages.

## Decisions

### The catalog produces kinds

`CatalogBuilder` becomes an orchestrator over kind producers. A producer selects its entities from the records, resolves each page document with facts, claims and gaps, and defines its list facets and columns. Items become `ItemProducer`, which keeps today's facts, icons, versions and tooltip comparison. Disciplines, skills, passives and spellsongs get their own producers.

Source mechanisms return sources for an entity reference instead of an item class. Merging, conditions, places and status evidence move from items to every entity with sources. `ItemStatus` becomes a status any producer assigns, and the maintainer report's gaps name the entity's kind. Claims become one claim file keyed by entity reference, read by `compare`. The writer writes each kind's index and pages under its route.

Before any new kind is added, the moved catalog must write the same item site data as before, compared file by file on the real build. A changed byte is a regression unless the design names it.

An alternative was to add skill producers beside the item code. It was rejected because every later kind would copy the same merging, statuses and coverage, and drift.

### The site serves kinds

One list component renders any kind's index from its facet and column definitions, with filters, counts, sorting and address state. The item list keeps its behaviour through it. One page shell holds metadata, breadcrumbs, the title, the Bugs section and search markers, and each kind supplies its body. The tooltip presenter registers a tooltip per kind. Routes stay thin per kind, each a loader from a shared factory and the kind's page component. The header's Browse menu, the home page entries, search lists and the missing-page view read the kind registry.

`site/src/lib/` moves into `items/`, `skills/`, `entity/`, `list/` and `ui/` folders. Each folder owns one part, and kinds import shared parts but not each other's.

The `routes/items` list is the largest file on the site, and moving it into the shared list is the riskiest step. Browser tests on fixtures and a real-data review at both widths must show the item list unchanged before the skill list uses it.

### Reachable trees from code

The producer finds live trees as the widget classes that the subcategory menus' button handlers create, from the code index. Their category follows from the menu. Their order follows from the menu's button order. A tree class that no handler creates is not published, and its skills are sourced only through what else teaches them. No list of tree names is kept.

### Tree layout from the widget tree

Extraction records every widget of a widget tree with its parent panel and position, and the text of text widgets. A row is the panel that holds a level label, and each slot sits in its own scale box inside it, so the producer follows a slot's chain of panels up to the row and orders a row's entries by their chain of positions. Each entry keeps its own required level, because that level gates it: in run rowgate1, at Necromancy level 6, Lasting Servitude, which requires level 5 and sits in the row labelled level 10, bought its ranks, while Death Dealer in the same row, which requires level 10, stayed locked. Entries whose level differs from their row's label go to the maintainer report.

The discipline name is the label of the menu button that opens the tree, such as "Arcane" for the button named Biomancy and "Focuses" for Staves. Extraction records each widget Blueprint's delegate bindings, so the producer follows a button's click binding to its handler and the handler to the tree class it creates. Handler names alone mislead, because copied handlers keep the name of the button they were copied from, such as the Flintlock button's handler named after Pyromancy.

### Skills

A skill is a skill class. Its row is the table row named by the class's `SkillId`, matched without regard to case, because the skill slot's tooltip looks rows up that way (`SkillSlot:HandleTooltip`). Its facts are what that tooltip shows: the row's name, description and cooldown, the row's cost as "Mana Cost" or, when the class's `Uses Stamina` is set, "Stamina Cost", and "Base Damage" from the `SkillInfo.Damage` of the class the row names, shown only above zero. In run skilltips1 this rule matched the tooltips of all 327 tree skills, including Thunder Staff, whose class has two rows, and Carrion Flock, whose row names an older class. Rows that no class's `SkillId` names are reported and not published.

A skill's sources are:

| Source | Where it comes from | What it shows |
|---|---|---|
| Tree slot | Live tree slot | Discipline, required level, 1 skill point |
| Skill book | Placed `SkillBook_InteractionObject` | The level it lies in |
| Conversation | Dialogue `UnlockSkill_C` | The person, as the dialogue mechanisms name them |
| Event | A cited function that grants it | Reader text written for that function |

Event wording lives in one reviewed table keyed by function citation. The catalog fails when a cited function's fingerprint changes or a granting function has no entry, so an update cannot leave stale wording.

A skill with no source is published with a status. A skill held only by an older tree reads that the game does not offer it. A skill with no reference reads that it has no known source. Both are hidden from the list by default behind one counted reveal, as items are.

The in-game skill tooltip is compared with the page's facts for every tree skill, through a lab sweep like the item tooltip sweep, and unexplained differences go to the maintainer report.

### Passives and spellsongs live on their discipline's page

The game shows passives and spellsongs only inside their tree window, so they get no pages of their own. Each is an entity with a key, a tooltip and an anchor on its discipline's page, and a link to one leads there. The kind registry gains a host kind for such kinds, which routes, the sitemap and publication checks honour.

A passive's key is its tree class and widget name. Its facts are its name, description as game text, required level, cost per rank and number of ranks, from the passive rule. A spellsong's key is its table row. Its facts are name, description, tune, charges and experience, and its sources are the placed objects that name it. The game draws a tune's notes as arrows: 1 up, 2 down, 3 right and 4 left, as its own note sheet built them for the tune 1,2,3,4 (`tools/runtime/checks/spellsong-arrows.lua`).

### Progression rules

Proficiency points per discipline level and passive rank cost become rules in `site/src/lib/rules/` with worded steps, citations of `Add Proficiency Points`, the tracking functions and `PassiveSkillButton`'s purchase functions, and recorded vectors from grants1 and passives1. Discipline pages show the points a level grants through the rule. Blood Magic shows the known issue instead of a number.

### Checking records against the game

The catalog compares the tree records with the latest `skill-trees.lua` run of the build when one exists, as it compares tooltips, and lists each difference in the maintainer report. A game update therefore shows when the records and the game's built windows part.

### Level references

An entry's level is a reference to the discipline whose level it checks, not a level name, because a few entries check another discipline's level, such as Roanan's Call on Druidism checking Unarmed. A level name belongs to the tree whose entries check it most, and a tree that owns none, such as Utility Gadgets, has no level of its own. Discipline pages head rows "Level 5" for skills and passives alike, and group passives by the level that unlocks each rather than the row the skill window shows them in, because the game checks that level (run rowgate1) and the window's rows misled both build planners in the trials. An entry that checks another discipline adds "Unlocks at Spears level 1" with a link.

### Gathering levels

A harvestable's required level comes from `src/Bloodline/Content/gathering-levels.json`, not from its placement, because `Check Player Level` sets `Required level` and the level it checks from the preset's name each time it runs (run harvest1). Pine needs Woodcutting 1, Birch 5 and Ash 20, Iron needs Mining 10, and Beetle needs Foraging 2. Its evidence names the runs, and the catalog fails when the harvestable class's Woodcutting, Mining and Foraging lists name other presets than the file. A table-drawn preset picks its name in play, so it gets no requirement. Sources say "Requires Mining level 10" as recipes say "Requires Weaponsmithing level 12".

### Links between pages

Every name the site shows for something in the game is a typed reference in the data, whether or not its kind has pages, so a kind that gains pages turns every mention into a link without rewriting text. This change adds pages for professions, and mechanic pages for Blood Levels, discipline levels, and skill and proficiency points, because a setting and a progression concept are both rules a player reads about rather than things in the world. Crafting stations are references without pages, and stats are named by their `Stat`, which the site turns into labels. Woodcutting, Mining and Foraging levels stay text in gathering conditions until gathering sources are complete enough to give them profession pages. Text written for the site marks a reference as a Markdown link with a typed target, such as `[Blood Levels](mechanic:blood-levels)`, and the catalog resolves it and fails on an unknown target. The maintainer report lists each issue or mechanic page that names a discipline, profession or mechanic without linking it anywhere, so the first mention gets its link. A name that starts a sentence or joins another capitalized word does not count, as in "Daggers, spears and some swords" or "Arcane Bolt". Skill and item names stay out of that check, because many are everyday words. The game's own text stays as the game shows it, without links, because matching names in it links the wrong sense, such as "frozen ground" in Path of Ice.

Weaponsmithing, Cooking, Engineering, Focus Crafting, Smelting and Farming are professions, as large RPG wikis call leveled crafts. Used For heads their column "Profession", with values such as "Weaponsmithing 12", and lists uses from the lowest level they need, because a crafter unlocks them in that order.

### Professions and mechanics

A profession page answers what each level lets you make and what it takes, so it lists every recipe in one table under level headings, with its ingredients and the experience it gives, and leads to the item list for sorting and filtering. A profession row carries its inputs and output count from the item's source, so the page reads no item pages. Whole items, such as weapons, guns and ingots, have no level in their recipe row. The crafting window that the anvil, the engineer's workbench and the smelting oven open lists them in seven tiers per profession, such as WeaponsmithingLevel1 to WeaponsmithingLevel7, and its `Get Player <Profession> Level and Set Available Items` functions open tier N from level N (run tiers1). The catalog gives such an item the profession and level of the first tier that lists it, over the type its row names, as Rune-Tempered Gear's row names Weaponsmithing while the engineer's workbench offers it. A whole item that no tier lists, such as Cannon, cannot be made there, so its recipe is unconfirmed.

The profession and mechanic lists are cards three to a row, because six professions and a few mechanics read as a table of two columns with most of the page empty. A profession card shows where you work, its levels, its item count and six examples spread from its lowest level to its highest.

Discipline Levels is a mechanic page for what new players asked in the trials: how a discipline levels, that it starts at level 1 and has no highest level, and which key opens the skill tree. Discipline pages link to it.

### Text pages and measure

Prose keeps a measure of 72 characters, inside the 45 to 90 that typography guides recommend and under WCAG's 80. Tables, panels and side columns use the page's width. About, Known Issues and the mechanic pages share a text layout with a side column for the page's contents and related pages, as GOV.UK's two-thirds layout does, so wide screens use the width beside the prose.

### Unknown values

A weapon part whose class other rows share, such as Steel Claymore Guard and Steel Skaneguard, takes the rows named like it, because the crafting window lists each row by its own name and takes that row's stats (run parts1). Where the catalog cannot follow a ranged weapon's firing code, such as a bow whose arrow spawns an explosion, `src/Bloodline/Content/measured-damage.json` gives the damage a lab run measured, with the run, and a bow's damage when fully drawn where it differs. A weapon whose damage the code later shows fails the catalog until its measurement is compared and removed. A fact without a value carries the reason, which item pages show on hover, focus and tap. A section's count is a pill beside its heading, so a single entry no longer reads as part of the name, such as "Skill Tree 1".

### Home and search

The header lists Items, Disciplines, Skills and Professions, the kinds players browse most, and the footer lists every kind. A line under each discipline's title says how disciplines level and links Discipline Levels. The home page leads with two buttons, Browse Items and Browse Skills, and shows item categories and disciplines as icon tiles with a count, the disciplines in one grid with their skill window category in each tile's line. The site's own search shows each result's icon, name and a line with its kind and context, such as "Skill · Magic · Pyromancy", in one list ranked by best match.

## Risks / Trade-offs

- Moving items onto kinds can change item output silently. The file-by-file comparison and the existing tests guard it.
- Widget positions can be absent for some trees. The catalog fails, and the lab screenshots show the intended layout to compare with.
- Event wording is hand-written. The citation and fingerprint check keeps it tied to code.
- A long skill list on phones. The shared list keeps the name visible and scrolls columns, as items do.

## Migration Plan

Refactor the catalog, then the site, each verified by unchanged item output and tests, before adding a kind. Regenerate schema and types and the invented fixtures in the same steps. Rollback reverts the change's commits. No reader data or links change for items.

## Open Questions

- How Performance, Song of Hastening and Inferno Strum are learned. No placed note sheet names them, and the Lute window shows every spellsong as unknown on a new character.
- Where two skill events happen, which the site words only as far as the code shows. Warp Bolt: walking into Karraq's TriggerSphere_1 starts the `Move to Player` timeline, whose end stuns you and unlocks the skill. The sphere stands about 700 m above the level's other triggers (run anvil1), and no place name lies near it. Cloud Catch: The Elderglen Woods' TriggerBox8_3 and its `Ragdoll Checker` start `Screen FX`, which slows time while you fall and calls `Esros Comment on Fall`. The box stands 88 m up, about 106 m from the Abandoned Farmstead and 164 m from the Steps of Esros (run anvil1). A run that plays each event would let the site name the spot.

- Which branch of Thomas Dornscheidt's dialogue gives which of the two items named The Embersnap. One fires Embersparks, and the other, a HandCannon, fired nothing in run ranged4. A run that plays both branches would show whether players can get the broken one, which would make it a known issue.

- Which items the harvest presets give that the records do not name. 1,339 placed harvestables draw their preset from a table, and 249 more name a preset whose items the game picks in code the catalog does not read, such as 121 Birch trees, so Birch Log, Ash Log and Skullcap Mushroom list no gathering source. The maintainer report lists each preset. A run that gives one harvestable each preset name, runs its construction and reads its loot would supply the items, as run harvest1 did for the levels.

## Where Findings Live

Use the architecture change's [Where findings live](../archive/2026-11-01-establish-reference-architecture/design.md#where-findings-live). This design owns the kind structure and identity decisions. Measured rules live in the rules with their vectors, defects in `issues/known-issues.json`, unresolved facts in the maintainer report, and run outputs under the build's artifacts.
