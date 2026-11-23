## 1. Extraction

- [x] 1.1 Record each Blueprint widget's parent panel and position, and text widgets with their text, in `ExtractionRunner.Widgets`, and update the `ClassRecord` documentation
- [x] 1.2 Re-extract build 24464547 and confirm every tree row's slots and passives match the treeshots1 screenshots in order

## 2. Catalog by kind

- [x] 2.1 Save the current real-data site data, claims and report as the comparison baseline outside Git
- [x] 2.2 Introduce kind producers, move item selection, facts, icons, versions and tooltip comparison into `ItemProducer`, and make `CatalogBuilder` an orchestrator
- [x] 2.3 Make source mechanisms return sources for an entity reference, and move merging, statuses, gaps and status evidence to every kind with sources
- [x] 2.4 Make claims one file keyed by entity reference, read by `compare`, and make the writer write every kind's index and pages
- [x] 2.5 Add host kinds to the registry, with link targets, sitemap and graph checks for anchored entries
- [x] 2.6 Regenerate schema and types, update fixtures and C# tests, and confirm the real-data item output matches the baseline file by file

## 3. Site by kind

- [x] 3.1 Reorganize `site/src/lib/` into `items/`, `skills/`, `entity/`, `list/` and `ui/` folders
- [x] 3.2 Extract the shared list from `routes/items/+page.svelte` and keep the item list's behaviour, covered by the existing browser tests
- [x] 3.3 Extract the page shell and route factory, and register tooltips per kind in the presenter
- [x] 3.4 Read the Browse menu, footer lists, home entries, search lists and missing-page links from the kind registry
- [x] 3.5 Review the item list and an item page at 1440 and 390 px on real data against production

## 4. Skills in the catalog

- [x] 4.1 Find live trees from the subcategory menus' button handlers, with category, order and the label the game shows
- [x] 4.2 Produce disciplines with skill and passive rows from the widget layout, reporting entries whose level differs from their row
- [x] 4.3 Produce skills from the skills table and live trees, establish in the lab which row the game reads for the three classes with two rows, and report the rest
- [x] 4.4 Add source mechanisms for tree slots, skill books, dialogue unlocks and code grants, with the reviewed table of event wording and its fingerprint check
- [x] 4.5 Assign statuses to skills without a source, distinguishing older trees from no reference
- [x] 4.6 Produce passives with rank costs and spellsongs with their note sheet sources, and observe in the lab how tunes map to note directions
- [x] 4.7 Compare the tree records with the latest `skill-trees.lua` run in the maintainer report
- [x] 4.8 Read every tree skill's in-game tooltip in the lab and compare it with the skill's facts
- [x] 4.9 Add producer tests on invented records for reachability, layout, sources, statuses and rank costs

## 5. Rules and issues

- [x] 5.1 Add proficiency point and passive rank cost rules with worded steps, citations and recorded vectors from grants1 and passives1, and extend `rules:check`
- [x] 5.2 Reproduce the Blood Magic points defect through the settings window, and record it in `issues/known-issues.json` with its check in `tools/runtime/checks/issues.sh` if it reproduces

## 6. Pages

- [x] 6.1 Compare discipline page layouts in Plannotator with the game's tree window screenshots, at both widths
- [x] 6.2 Build discipline pages with skill rows, proficiency rows, Lute's spellsongs, facts and Bugs
- [x] 6.3 Compare skill page, skill tooltip, passive and spellsong tooltip designs and status wording in Plannotator
- [x] 6.4 Build skill pages, the skill list with its filters and reveal, and passive and spellsong anchors and tooltips
- [x] 6.5 Add fixtures with extreme content for every new kind and browser tests for the list, pages, anchors and tooltips
- [x] 6.6 Link every entry to the discipline whose level it checks, and head discipline rows "Level 5"
- [x] 6.7 Rebuild home and search as icon tiles and the site's own result rows, with two home buttons

## 6A. Links between pages

- [x] 6A.1 Add typed references for professions, settings, progression concepts, stats and stations, and resolve every condition and name to one
- [x] 6A.2 Resolve Markdown links with typed targets in text written for the site, failing the catalog on an unknown target, and report unreferenced mentions
- [x] 6A.3 Publish pages for professions, Blood Levels, and skill and proficiency points from recorded facts
- [x] 6A.4 Head the Used For column and filter "Profession" and link its values

## 6B. Gathering spots

- [x] 6B.1 Give placed harvestables the loot and level of their preset, named or drawn from a table, from a lab run
- [x] 6B.2 Add the harvestables that spawner boxes, open-world and dungeon populators, small event areas and the Harbinger tutorial create at runtime, with the place of each spawner
- [x] 6B.3 Compare every level's live harvestables with what the catalog says the level gives, from a census run, and fail on a difference without a known cause
- [x] 6B.4 Compare the gathering source rows, names and notes in Plannotator on real item pages

## 7. Ship

- [x] 7.1 Run the full check suite and the real-data build
- [x] 7.2 Review every page kind on real data at 1440 and 390 px and record the review in the commit bodies
- [x] 7.3 Update the README and the runtime-checks skill for new commands and checks
- [ ] 7.4 Push, wait for CI, deploy to staging and production, and pass the smoke checks
- [ ] 7.5 Validate, archive and sync the specs
