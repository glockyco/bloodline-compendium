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

### Harvest presets

A harvestable with a custom preset gives the loot and requires the level that `src/Bloodline/Content/harvest-presets.json` lists for its preset's name, not what its placement records, because the game sets both from that name in `NewFunction_0` and `Check Player Level` each time it applies the preset (runs harvest1 and presets2). Each gather gives one item of the loot, picked by its weight over the table's sum as `Choose Loot` draws, so a Pine Tree gives Pine Log 80% of the time and Bundle of Twigs 20%. Pine needs Woodcutting 1, Birch 5 and Ash 20, Iron needs Mining 10, and Beetle needs Foraging 2. A placement whose preset a table draws becomes one of the table's presets in play, picked by weight as `Choose Preset` draws (run presets2), so it gives each preset's loot with that preset's level, in a row of its own with a note that only some of those spots give the item. A source is named for its preset as the game's pools name theirs: a tree for a Woodcutting preset, such as Birch Tree, a deposit for a Mining preset, such as Coal Deposit, and the preset's own name otherwise, such as Skullcap Mushroom. The catalog fails when the harvestable class's Woodcutting, Mining and Foraging lists name other presets than the file, or a placement names or draws a preset the file lacks. Sources say "Requires Mining level 10" as recipes say "Requires Weaponsmithing level 12".

Spawners add harvestables in play, and a harvestable they add counts like a placed one with a drawn preset, at its spawner's level. A spawner box spawns from the General Mushroom Spawn Table or the General Ore Spawn Table when its flag is set, and from its own Custom Preset Table otherwise, as all 575 boxes that run census2 read held once they had spawned. A box that does not spawn on its own spawns when its level, a cave or an event calls it, as The Pale Pass does when you enter its caves. An open-world populator with an Ore (Iron) Count spawns that many Iron deposits (run outcrops1), and its tree, stone and copper counts spawned nothing in 56 levels, so the catalog gives them no harvestables. A dungeon populator spawns outcrops from its Ore Chances and trees from its Tree Chances when their spawn chances are above zero, each taking the preset its type names first, such as Iron for Iron Deposit, as Moonhorn Cliffs' outcrops did in run census3. The harvest sweep reads every level's harvestables in play after emptying the saved harvestables, because loading a save spawns the ones saved near the player again in whichever level loads, and the maintainer report lists every preset in play that the catalog does not give in its level and every loot that differs from the measured one.

A harvest's experience is a fixed amount per preset, which the harvestable's `Get Woodcutting EXP to Add`, `Get Mining EXP to add` and `Get Foraging EXP to Add` return, such as 5 for Pine, 7 for Coal, 25 for Iron and 90 for Gold and Hetrum (run yield1). The gathering minigame's `Bonus Loot` rolls `Calculate Bonus Chance` with `RandomBoolWithWeight` and calls `Give Bonus Loot`. That chance takes no input but the character's level. In run yield1 it returned 15% for every Foraging preset at every level, but a forage never rolls it, because the pick's `Pick Up` calls no bonus function and the minigame that does never opens for a forage. For Woodcutting and Mining it held one value up to a level above the requirement and then rose with the character's level, identical across presets: 18.4% at level 1, 30.3% at 11, 42.8% at 26, 58.6% at 51 and 83.5% at 101. Every Woodcutting and Mining value measured equals that curve at the larger of the level minus the requirement plus 1 and the requirement plus 1, so Iron, which needs Mining 10, gives 30.3% from Mining 10 to 20. A tree or deposit gives three items, one per swing, whatever you do in the gathering minigame. In a game window, a Coal deposit gave three items with only misses and with hits, and a Pine gave two Pine Log and a Bundle of Twigs (run yield7). A hit gives the preset's experience and rolls the bonus: the deposit harvested with hits gave 7 Mining experience and three more draws from its own table, two Blaststone and a Tier 1 Armor Rune, while the deposit harvested with misses gave no experience and no bonus (run yield7). The game's Gathering tutorial says the same: "If you miss, not to worry, you'll still get the base amount of resources, but no bonus EXP" (run yield4, `gathering-tutorial.png`). A mushroom or herb is one pick without the minigame: a Fly Agaric gave one Fly Agaric and 3 Foraging experience and was removed about eight seconds later (run forage1). A used tree is removed a few seconds after its last swing. A used deposit stays where it is and refuses further harvests. When the area loads again, every spot is fresh, and spots with a spawn chance roll it again: The Elderglen Woods held 97 Coal deposits on one load and 36 on the next, while its Pine count stayed near its 89 placements (runs yield1 and yield7).

### Resources

A gathering resource is the thing a player meets by its name, such as Coal Deposit or Abyssberry Bush, so its name is its key and its page, whichever class, preset or seed row gives it. Its page holds what the resource gives and where, so its item rows link to it with a count of areas instead of the area list, as Ancient Kingdoms and the official wiki keep node pages apart from item pages. A place row counts each spot by how likely it is to be this resource on a load: its spawn chance, which the game rolls again each time the place loads, times the resource's weight in the table when the spot draws its preset. Spawners count apart, because how many they place varies. The single objects a player takes once, such as a key on a table or a candle, are pickups without pages, because each is named after the item it gives. The interactive map will show a resource's spots on its page and link each area to the map with the resource marked.

A resource page lists what it gives with each item's chance, how many one tree or deposit gives on average and the item's value, then the areas with the average number a visit finds and the most that can appear. Its Harvest panel beside the tables gives the level it needs, the yield, the experience and a bonus calculator that reads the measured chance at the level you enter, because a gatherer asks what they need and what their level earns while reading what a spot gives. Each area counts a spot by the chance it is there times its share of a drawn table, and a spawner box by the average of its Min and Max Spawn Count, which 617 of 636 boxes in run census3 fell between, so the reader never meets spawners. The list shows each group as a tab of cards three to a row, sorted by the level it needs, because 76 resources in one page bury the later groups.

### Side columns

A page's side column, such as an item's game card, a resource's Harvest panel or a mechanic page's contents, starts level with the first card of its main column, below that section's heading and intro, so the two columns read as one row however long the intro is. `site/src/lib/ui/align-side.ts` measures the offset, the stylesheet's offset serves readers without JavaScript, and on a phone the side column comes first.

### Map deltas

Extraction keeps the keys an object's map removes from its archetype's map, because Unreal saves such a map as the keys it removes and the entries it adds or changes, and the catalog merges it over the archetype's map as the game loads it. Spawner boxes, dungeon populators, enemy spawners and loot placers change their tables this way, such as a Snowy Cliffside ore box removing the default Pine and Birch while a Khenmoore box keeps the default Birch beside its own Ash, Oak and Pine.

### Links between pages

Every name the site shows for something in the game is a typed reference in the data, whether or not its kind has pages, so a kind that gains pages turns every mention into a link without rewriting text. This change adds pages for professions, and mechanic pages for Blood Levels, discipline levels, and skill and proficiency points, because a setting and a progression concept are both rules a player reads about rather than things in the world. Crafting stations are references without pages, and stats are named by their `Stat`, which the site turns into labels. Woodcutting, Mining and Foraging levels stay text in gathering conditions until gathering sources are complete enough to give them profession pages. Text written for the site marks a reference as a Markdown link with a typed target, such as `[Blood Levels](mechanic:blood-levels)`, and the catalog resolves it and fails on an unknown target. The maintainer report lists each issue or mechanic page that names a discipline, profession or mechanic without linking it anywhere, so the first mention gets its link. A name that starts a sentence or joins another capitalized word does not count, as in "Daggers, spears and some swords" or "Arcane Bolt". Skill and item names stay out of that check, because many are everyday words. The game's own text stays as the game shows it, without links, because matching names in it links the wrong sense, such as "frozen ground" in Path of Ice.

Weaponsmithing, Cooking, Engineering, Focus Crafting, Smelting and Farming are professions, as large RPG wikis call leveled crafts. Used For heads their column "Profession", with values such as "Weaponsmithing 12", and lists uses from the lowest level they need, because a crafter unlocks them in that order.

### Professions and mechanics

A profession page answers what each level lets you make and what it takes, so it lists every recipe in one table under level headings, with its ingredients and the experience it gives, and leads to the item list for sorting and filtering. A profession row carries its inputs and output count from the item's source, so the page reads no item pages. Whole items, such as weapons, guns and ingots, have no level in their recipe row. The crafting window that the anvil, the engineer's workbench and the smelting oven open lists them in seven tiers per profession, such as WeaponsmithingLevel1 to WeaponsmithingLevel7, and its `Get Player <Profession> Level and Set Available Items` functions open tier N from level N (run tiers1). The catalog gives such an item the profession and level of the first tier that lists it, over the type its row names, as Rune-Tempered Gear's row names Weaponsmithing while the engineer's workbench offers it. A whole item that no tier lists, such as Cannon, cannot be made there, so its recipe is unconfirmed.

The profession and mechanic lists are cards three to a row, because six professions and a few mechanics read as a table of two columns with most of the page empty. A profession card shows where you work, its levels, its item count and six examples spread from its lowest level to its highest.

Discipline Levels is a mechanic page for what new players asked in the trials: how a discipline levels, that it starts at level 1 and has no highest level, and which key opens the skill tree. Discipline pages link to it.

A discipline page opens with one sentence written from its rules that says how it levels, what its level unlocks and how many proficiency points each level up gives, such as "Necromancy levels up as you use it. Its level unlocks the skills below, and each level up gives you 2 proficiency points to spend on its passives.", because a player reads the opening anyway and it can say what the points buy. Blood Magic's sentence names its points bug instead of a number, and a tree without a level of its own names the levels its skills check. Discipline and profession pages head with the title and its line and no icon, as item and skill pages do, so every page kind has the same heading on one left edge. The Proficiency Points page gives Focuses "1 for the first level up, then none" and Vampirism "None", and says their points buy nothing because they have no passives.

### Tabs

A page whose parts a reader switches between, such as the resource groups and the planner's sections, uses `site/src/lib/ui/Tabs.svelte`: each tab has its own address fragment, the arrow keys move between tabs, and a count follows a label where the part is a list. Until the script runs every part shows with its heading, so search and readers without JavaScript see all of them. Tabs stay in one row: where they do not fit, the component replaces them with a full-width dropdown of the same parts, as the five resource groups on a phone, because tabs that scroll hide later parts and tabs that wrap lose their link to the panel. The planner's three tabs fit on a phone and stay tabs.

### Missing pages

A missing address shows the site's search results for the words in its last part, preferring the kind its first part names, as cards like the list pages, and then every list as a card to browse, because the address usually names what the reader wanted, such as /items/knights-broadsword/. The header's search is the only search field, so the page never shows two.

### Text pages and measure

Prose keeps a measure of 72 characters, inside the 45 to 90 that typography guides recommend and under WCAG's 80. Tables, panels and side columns use the page's width. About, Known Issues and the mechanic pages share a text layout with a side column for the page's contents and related pages, as GOV.UK's two-thirds layout does, so wide screens use the width beside the prose.

### Unknown values

A weapon part whose class other rows share, such as Steel Claymore Guard and Steel Skaneguard, takes the rows named like it, because the crafting window lists each row by its own name and takes that row's stats (run parts1). Where the catalog cannot follow a ranged weapon's firing code, such as a bow whose arrow spawns an explosion, `src/Bloodline/Content/measured-damage.json` gives the damage a lab run measured, with the run, and a bow's damage when fully drawn where it differs. A weapon whose damage the code later shows fails the catalog until its measurement is compared and removed. A fact without a value carries the reason, which item pages show on hover, focus and tap. A section's count is a pill beside its heading, so a single entry no longer reads as part of the name, such as "Skill Tree 1".

### Home and search

The header lists Items, Disciplines, Skills and Professions, the kinds players browse most, and the footer lists every kind. The home page leads with two buttons, Browse Items and Browse Skills, and shows item categories and disciplines as icon tiles with a count, the disciplines in one grid with their skill window category in each tile's line. The site's own search shows each result's icon, name and a line with its kind and context, such as "Skill · Magic · Pyromancy", in one list ranked by best match.

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

- Where dungeon populators place their outcrops. Every populator with an outcrop chance gives its ores in the catalog, but in run census3 outcrops appeared only in Moonhorn Cliffs, six of them at the world's origin. The open-world populators' tree, stone and copper counts spawned nothing, and Woodhurst Flatlands' 25 Iron deposits all stand at the populator's own spot, which may be a known issue.

- Whether travel spawns the harvestables saved near you in the next area. Loading a level in the lab spawns the saved harvestables at their saved spots in whichever level loads, so run census3 found the Harbinger tutorial's trees and stones in Squalls End and the Necropolis. A run that travels on the map as a player does and then reads the area would show whether players meet them, which would make it a known issue.

- How the bonus's extra draws are counted. In run yield7 one hit gave three extra items, and `Give Bonus Loot` adds between one and three, by its call list. A run that harvests many deposits with hits would give the distribution.

- Maps inside structs and sets keep only the entries they store, because extraction reads removed keys for an object's top-level maps alone.

## Where Findings Live

Use the architecture change's [Where findings live](../archive/2026-11-01-establish-reference-architecture/design.md#where-findings-live). This design owns the kind structure and identity decisions. Measured rules live in the rules with their vectors, defects in `issues/known-issues.json`, unresolved facts in the maintainer report, and run outputs under the build's artifacts.
