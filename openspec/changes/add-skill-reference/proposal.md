# Proposal

## Why

Players plan characters around the game's 21 disciplines, but nothing outside the game shows a tree, what a skill does, where a skill book lies or what a passive rank costs. The official wiki lists 47 skills. The game defines 594. The catalog and the site were built for items alone, so a second kind needs the structure moved from items to kinds first, or every later kind repeats item code.

## What Changes

- The catalog runs one producer per kind. Source mechanisms find sources for any entity, and statuses, coverage, claims, the list index and the site data layout work for every kind. Items move onto this structure with unchanged output.
- The site serves every kind through one list, one page shell and one tooltip registry. The header gets a Browse menu and the home page and search read the kind registry. The site's library moves into folders by kind and by shared part.
- Extraction records each widget's place in its widget tree, so a tree's rows and order come from the game's own layout.
- New pages for the 21 disciplines that the skill window opens, each laid out as the game's tree window: skills in rows by required discipline level, passives in rows by proficiency level, and for Lute its spellsongs.
- New pages for every skill in the game's skill table, with its facts and every way to get it: a tree slot, a skill book, a conversation or an event in the game's code. A skill without a known way says so and is hidden from the list by default behind a counted reveal.
- Passives and spellsongs as entities shown on their discipline's page, with links and tooltips that lead there.
- Proficiency points per discipline level and passive rank costs as shared rules, checked against lab runs.
- A known issue for Blood Magic points, once the lab reproduces it through the settings window.

## Capabilities

### New Capabilities

- `catalog/skills`: disciplines, skills, passives and spellsongs, which trees a player reaches, where each skill and spellsong comes from, and the checks against the running game.
- `site/skill-reference`: discipline pages laid out as the game's tree window, skill pages, the skill list, and passive and spellsong entries with their tooltips.

### Modified Capabilities

- `catalog/entity-graph`: a kind can be shown as anchored entries on another kind's pages, and publication checks those anchors.
- `extraction/source-records`: class records hold the Blueprint widgets of widget trees with their place in the tree.
- `site/item-reference`: search, the header and the home page cover every kind with pages.
- `site/support-pages`: the home page and the missing-page view lead to every list.

## Impact

- `src/Bloodline/Catalog/`: `CatalogBuilder`, `SourceMechanism` and its mechanisms, `ItemStatuses`, `ItemClaims`, `MaintainerReport`, `CatalogWriter`, `CatalogGraph`, `EntityKinds`, and new producers for disciplines, skills, passives and spellsongs.
- `src/Bloodline/SiteData/SiteDataModel.cs` and the generated schema and types.
- `src/Bloodline/Extraction/ExtractionRunner.cs` and `RecordFiles.cs`.
- `site/src/lib/` reorganized, `site/src/routes/` gains discipline and skill routes, list and page shells shared with items.
- `site/src/lib/rules/` gains progression rules, `tools/runtime/checks/` gains tree and tooltip comparisons.
- `issues/known-issues.json`, fixtures, unit and browser tests.
