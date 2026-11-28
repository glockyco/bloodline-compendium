# Tasks

## 1. Link coverage check

- [x] 1.1 Count plain-text names per kind on the built pages of a real-data build, and verify the creature count matches the creatures item and skill pages show without a link (166)
- [x] 1.2 Check built pages for unlinked names with longest-first whole-phrase matching and the skips the design names, and verify with `site/tests/link-coverage.test.js` that a row naming Golden Talon Wand Shaft as plain text is reported
- [ ] 1.3 Add `wanted:` link targets: the site renders them as text, the build lists them with their pages in `artifacts/link-coverage.json` and the catalog fails on a wanted name an entity now holds, and verify with Divine Bond on the Harvested from Kills rows
- [x] 1.4 Record today's counts in `coverage/link-coverage.json`, make real-data builds fail on a rise, naming the new names, and on an unlowered fall, describe the record in the README, and verify both failures against the recorded counts

## 2. Previews and link style

- [x] 2.1 Fail publication for a kind with pages or hosted entries that `previewKinds` lacks, and verify with `site/tests/publication.test.js`
- [x] 2.2 Compare and build previews for resources, professions and mechanics, with hover screenshots at 1440 and 390, and verify the gate passes
- [x] 2.3 Compare and build one style for entity names, underlined in the link colour with or without an icon, add the style rule to `site/tests/layout.ts`, and verify the layout check passes on the real-data review pages
- [x] 2.4 Add the link and preview check to `.omp/agents/comparison-check.md`
- [x] 2.5 Add the `page-design` skill, the `design-research` agent and `scripts/check-comparison.mjs`, have the `comparison-check` agent run the script and take only a path, and verify with `site/tests/comparison-check.test.js` that one built design asked against the current page is reported

## 3. Creatures

- [x] 3.1 Compare and build creature pages with drops, each with its areas, places, spawn conditions and their preview, and verify every creature reference on item pages links and the creature count in the recorded file drops to zero. The debuffs a creature can carry move to 5.2, because effects have no pages yet
- [ ] 3.2 Read and check in the lab what a creature's health, armor, damage, resistances and experience on kill do, compare how creature pages and their previews show them, and make the preview describe the creature by them instead of where it lives

## 4. Places

- [x] 4.1 Compare and build place pages with the creatures, containers, pickups, merchants and people each place holds, and their preview, and lower the recorded place count to zero
- [ ] 4.2 Read which terrain tag opens which camp map, and list on each camp place the world map events that lead there with their chances
- [ ] 4.3 List on place pages the creatures placed there that give no item, from the spawn sites, with task 3.2's stats
- [ ] 4.4 Read services and crafting stations by place, such as stables, workbenches and cooking fires, and show them on place pages whether or not they give items
- [ ] 4.5 Read the transitions between levels and list each place's connected places
- [ ] 4.6 Extract actor positions and compare a map figure for place pages

## 5. Effects

- [ ] 5.1 Read the creature debuffs, weapon enchantments and armor attunements and how each is applied, and record the readings in this design
- [ ] 5.2 Compare and build effect pages and their preview, replace the `wanted:` marks for Divine Bond, Soulsap, Arcane Enchantment and Arcane Pulse with links, and verify the Harvested from Kills rows link them

## 6. People and merchants

- [ ] 6.1 Compare and build pages for people and merchants with what they sell, give and say, and their preview, and lower both recorded counts to zero

## 7. Quests

- [ ] 7.1 Compare and build quest pages with their stages, rewards and givers, and their preview, and lower the recorded quest count to zero

## 8. Containers and settlement buildings

- [ ] 8.1 Compare and build container pages with their loot and places, and their preview, and lower the recorded container count to zero
- [ ] 8.2 Compare and build settlement building pages with what they deliver and cost, and their preview, and lower the recorded building count to zero

## 9. Recipes and pickup spots

- [ ] 9.1 Compare recipes as pages of their own against anchored entries on the pages of what they make, build the chosen one with its preview, and lower the recorded recipe count to zero
- [ ] 9.2 Compare how pickup spots link, as entries on place pages or as map positions, build it, and lower the recorded pickup count to zero
- [ ] 9.3 Compare and build pages for the gathering skills Mining, Woodcutting, Foraging and Fishing, which conditions such as "Requires Mining level 10" name as plain text without an entity, with their levels and resources, and their preview, and link those conditions

## 10. Integration

- [ ] 10.1 Rebuild real data, run every check, review changed pages at 1440, 768 and 390 px with the `visual-review` agent, and record the review in the commit bodies
