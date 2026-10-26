# Tasks

## 1. Record where knowledge lives

- [x] 1.1 Add the rule from design.md "Where findings live" to AGENTS.md as one instruction that points to that section, and verify AGENTS.md names no finding itself
- [x] 1.2 Revise the `Independent presentation tokens` and `Readable accessible presentation` requirements in `publish-item-reference` so they no longer forbid the identity's webfonts and decoration, and verify `openspec validate publish-item-reference --strict` passes
- [x] 1.3 Rewrite the vault note's `Resume here:` line and tasks to point at this change, and verify the note names no session-only fact

## 2. Presentation foundation

- [x] 2.1 Finish the closed token scales and migrate app.css, the layout, Search and the home page to them, and verify `stylelint` passes on all site styles
- [x] 2.2 Add `stylelint` to `bun run lint` and CI, and verify a planted raw padding value fails the lint
- [x] 2.3 Mock the two palette options with a real item tooltip and the home page, decide, and record the choice in this design's Open Questions
- [x] 2.4 Add the shared primitives (card with subgrid rows, icon slot, section heading, count, button, relation table, entity link, issue note) and verify each renders on a fixture page with extreme content
- [x] 2.5 Add Playwright layout checks for overflow, cross-card row alignment, minimum text size and contrast at 390, 768 and 1280 px, and verify each fails on a planted defect in the fixture page
- [x] 2.6 Write the voice rules and banned-phrase list beside the `site/presentation` spec and add a check over built HTML, and verify it fails on a planted phrase
- [x] 2.7 Commit the emblem, header mark, favicons and link preview with the token work, and verify the built site serves every brand asset
- [x] 2.8 Add the stat vocabulary and one number formatting function, move every stat label and number on the site to them, and verify that the item list and a tooltip show an item's value identically and that every item slug and qualifier stays the same

## 3. Lab runtime tool

- [x] 3.1 Move the bridge mod and host into `tools/runtime/`, pin UE4SS by SHA-256, add `prepare` for a fresh lab copy, and verify a fresh lab copy answers a request
- [x] 3.2 Add the CLI with `launch`, `new-save`, `quit`, `give`, `equip`, `use`, `open-sheet`, `call`, `read-tooltips`, `sample`, `capture-screen` and `capture-model`, writing each run to a new `artifacts/<build>/runtime/<run-id>/`, and verify a run gives three items and reads their tooltip text and that `call` records the result of the game's `Set Required EXP` for chosen levels
- [x] 3.3 Refuse targets other than the configured lab copy and stop only the started process, and verify the refusal against the Steam library path
- [x] 3.4 Run the game off screen (`-nullrhi`, `-RenderOffScreen`) without taking focus, and record the result in this design
- [x] 3.5 Write `.agent/skills/runtime-checks/SKILL.md` with the procedure, the button binding lookup and the off-screen modes, and verify it names commands that exist

## 4. Code cross-reference, citations and rules

- [ ] 4.1 Build the per-build cross-reference of properties and tables to the functions and widgets that read or write them, and verify it lists `ArmorDataTable` under the three tooltip widgets and `ArmorInfo.DamageRating` under `FirstPersonCharacter2`
- [ ] 4.2 Compute function fingerprints from the parsed expression tree, and verify two extractions give equal fingerprints and that a changed fixture function changes its fingerprint
- [ ] 4.3 Attach claim types and `Package:Function` citations to item facts, reject citations to missing or partly parsed functions, and replace the line-number comments in `ItemFacts.cs`
- [ ] 4.4 List citations with changed fingerprints in the build comparison report, and verify it on two fixture builds
- [x] 4.5 Add the rules module with typed inputs, outputs and worded steps, evaluated at prerender and in the browser, with tests on invented inputs, and verify a rule with an unknown input returns unknown
- [x] 4.6 Implement the experience requirement as the first rule with its citation, record game results for it with `call`, add the local check against those results (`bun run rules:check <build>`), and verify it fails on a planted mismatch. The attack speed word is extracted data, `ItemInfo.Speed`, and moves to task 5.3
- [ ] 4.7 Spike character progression through the cross-reference and one lab run: which skill tree version players see, which points exist and how they are earned, how discipline, attribute and proficiency levels rise and what passive ranks cost, and record the findings in this design for the planner change

## 5. Verified item facts

- [ ] 5.1 Equip two armor items whose class default and table values differ in opposite directions and read `Total Armor` and mobility in two runs, then publish the applied source and verify the catalog test
- [ ] 5.2 Sample Health every frame while eating Apple Pie and Fondue against a regeneration baseline in two runs, then publish the applied healing and verify the catalog test
- [ ] 5.3 Replace the `Weapon type` fact with the game's attack style label and speed word as the tooltip derives them, from `ItemInfo.WeaponType` and `ItemInfo.Speed`, and verify Lonland Pistol and a rifle
- [ ] 5.4 Add the bulk tooltip check over every inventory item, report differences with owners, and verify a report on the full catalog with no unexplained difference left unlisted in coverage

## 6. Entity graph contract

- [ ] 6.1 Add the kind registry, keys and relation rows to the catalog, and verify keys are stable across two extractions, unique per kind and equal to the class paths a lab save stores for its inventory
- [x] 6.2 Convert every source mechanism and use to relation rows with references to merchants, people, creatures, places, containers, recipes and buildings and with typed conditions, and verify sample items for each mechanism and the calendar event condition of the Hallowed Eve Merchant
- [x] 6.3 Write references and reverse relations into site data, regenerate the schema, types and fixtures, and verify the generated-file check
- [x] 6.4 Add the catalog graph check for dangling keys, duplicate keys and slugs, one-way relations and links to kinds without pages, and verify each on a planted fixture
- [x] 6.5 Extend the built-site gate to require every relation in static HTML, and verify it fails when a relation renders only after JavaScript
- [ ] 6.6 Render references through the entity link component, unlinked for kinds without pages, and verify an item sold by a merchant on real data
- [ ] 6.7 Feed routes, Pagefind kind filters, the sitemap and page metadata from the kind registry, and verify a search filtered to items and the sitemap on real data

## 7. Known issues

- [ ] 7.1 Add `issues/known-issues.json` with its schema, and verify the catalog fails on a page note naming a missing entry
- [ ] 7.2 Enter the issues reproduced in group 5 with player facts first, observed and derived parts separated, and confirmed builds, and verify each entry's steps against its run records
- [ ] 7.3 Render issue notes on affected items and the Known Issues page, link it from About and the footer, and verify search and sitemap include it
- [ ] 7.4 Add the recheck to the update procedure and write `.agent/skills/game-issues/SKILL.md` with the ownership table and report drafting, and verify it names files that exist
- [ ] 7.5 Draft developer reports for the confirmed entries for posting on the game's Discord, and verify each follows the entry

## 8. Item pages on the foundation

- [ ] 8.1 Build the item tooltip component in the game's order and wording with the catalog's verified values, and verify a weapon, an armor piece, a food and a weapon part on real data
- [ ] 8.2 Show entity tooltips on hover and keyboard focus for every item link, loading tooltip data per entity, and verify a recipe ingredient's tooltip with mouse, keyboard and with JavaScript off
- [ ] 8.3 Rebuild the home, list, item, coverage, About and 404 pages from the primitives with rewritten copy, and verify the presentation and voice checks pass
- [ ] 8.4 Review every page on real data at 1440 and 390 px, and record the review in the commit bodies

## 9. Integration

- [ ] 9.1 Run every check (dotnet, types, guard, lint, stylelint, format, svelte-check, unit, fixtures build, e2e, voice, OpenSpec strict validation), and verify all pass
- [ ] 9.2 Create the change for disciplines, skills and the skill planner from design.md "Order of later changes" and the findings of 4.7, and verify `openspec validate --strict` passes for it
