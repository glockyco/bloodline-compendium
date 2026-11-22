## 1. Extraction

- [ ] 1.1 Record each Blueprint widget's parent panel and position, and text widgets with their text, in `ExtractionRunner.Widgets`, and update the `ClassRecord` documentation
- [ ] 1.2 Re-extract build 24464547 and confirm every tree row's slots and passives match the treeshots1 screenshots in order

## 2. Catalog by kind

- [ ] 2.1 Save the current real-data site data, claims and report as the comparison baseline outside Git
- [ ] 2.2 Introduce kind producers, move item selection, facts, icons, versions and tooltip comparison into `ItemProducer`, and make `CatalogBuilder` an orchestrator
- [ ] 2.3 Make source mechanisms return sources for an entity reference, and move merging, statuses, gaps and status evidence to every kind with sources
- [ ] 2.4 Make claims one file keyed by entity reference, read by `compare`, and make the writer write every kind's index and pages
- [ ] 2.5 Add host kinds to the registry, with link targets, sitemap and graph checks for anchored entries
- [ ] 2.6 Regenerate schema and types, update fixtures and C# tests, and confirm the real-data item output matches the baseline file by file

## 3. Site by kind

- [ ] 3.1 Reorganize `site/src/lib/` into `items/`, `skills/`, `entity/`, `list/` and `ui/` folders
- [ ] 3.2 Extract the shared list from `routes/items/+page.svelte` and keep the item list's behaviour, covered by the existing browser tests
- [ ] 3.3 Extract the page shell and route factory, and register tooltips per kind in the presenter
- [ ] 3.4 Read the Browse menu, footer lists, home entries, search lists and missing-page links from the kind registry
- [ ] 3.5 Review the item list and an item page at 1440 and 390 px on real data against production

## 4. Skills in the catalog

- [ ] 4.1 Find live trees from the subcategory menus' button handlers, with category, order and the label the game shows
- [ ] 4.2 Produce disciplines with skill and passive rows from the widget layout, failing on mixed levels in a row
- [ ] 4.3 Produce skills from the skills table and live trees, establish in the lab which row the game reads for the three classes with two rows, and report the rest
- [ ] 4.4 Add source mechanisms for tree slots, skill books, dialogue unlocks and code grants, with the reviewed table of event wording and its fingerprint check
- [ ] 4.5 Assign statuses to skills without a source, distinguishing older trees from no reference
- [ ] 4.6 Produce passives with rank costs and spellsongs with their note sheet sources, and observe in the lab how tunes map to note directions
- [ ] 4.7 Compare the tree records with the latest `skill-trees.lua` run in the maintainer report
- [ ] 4.8 Read every tree skill's in-game tooltip in the lab and compare it with the skill's facts
- [ ] 4.9 Add producer tests on invented records for reachability, layout, sources, statuses and rank costs

## 5. Rules and issues

- [ ] 5.1 Add proficiency point and passive rank cost rules with worded steps, citations and recorded vectors from grants1 and passives1, and extend `rules:check`
- [ ] 5.2 Reproduce the Blood Magic points defect through the settings window, and record it in `issues/known-issues.json` with its check in `tools/runtime/checks/issues.sh` if it reproduces

## 6. Pages

- [ ] 6.1 Compare discipline page layouts in Plannotator with the game's tree window screenshots, at both widths
- [ ] 6.2 Build discipline pages with skill rows, proficiency rows, Lute's spellsongs, facts and Bugs
- [ ] 6.3 Compare skill page, skill tooltip, passive and spellsong tooltip designs and status wording in Plannotator
- [ ] 6.4 Build skill pages, the skill list with its filters and reveal, and passive and spellsong anchors and tooltips
- [ ] 6.5 Add fixtures with extreme content for every new kind and browser tests for the list, pages, anchors and tooltips

## 7. Ship

- [ ] 7.1 Run the full check suite and the real-data build
- [ ] 7.2 Review every page kind on real data at 1440 and 390 px and record the review in the commit bodies
- [ ] 7.3 Update the README and the runtime-checks skill for new commands and checks
- [ ] 7.4 Push, wait for CI, deploy to staging and production, and pass the smoke checks
- [ ] 7.5 Validate, archive and sync the specs
