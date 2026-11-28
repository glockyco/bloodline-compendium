# Tasks

## 1. Link coverage check

- [ ] 1.1 Count plain-text names per kind from the references in site data and write them to `report.json`, and verify the creature count matches the 179 distinct creatures item and skill pages name today
- [ ] 1.2 Add the unlinked-name check to the publication gate over built pages, with longest-first whole-phrase matching and the skips the design names, and verify it reports Golden Talon Wand Shaft when a fixture row names it as plain text
- [ ] 1.3 Add `wanted:` link targets: the site renders them as text, the catalog lists them with their pages in `report.json` and fails on a wanted name an entity now holds, and verify with Divine Bond on the Harvested from Kills rows
- [ ] 1.4 Record today's counts and unlinked names in `coverage/link-coverage.json` with the reviewed list of skipped one-word names, make the gate fail on a rise and on an unlowered fall, describe the file in the README where coverage files are described, and verify a fixture that adds a plain place name fails the build

## 2. Previews and link style

- [ ] 2.1 Make the kind registry say which kinds have previews, make `previewKinds` read it, fail the entity graph gate for a kind with pages and no preview, and verify it fails today for resources, professions and mechanics
- [ ] 2.2 Compare and build previews for resources, professions and mechanics, with hover screenshots at 1440 and 390, and verify the gate passes
- [ ] 2.3 Compare and build one style for entity names in `EntityLink`, add the style rule to `site/tests/layout.ts`, and verify the layout check passes on the real-data review pages and fails on The Mad Monk in link colour
- [ ] 2.4 Add the link and preview check to `.omp/agents/comparison-check.md`

## 3. Creatures

- [ ] 3.1 Compare and build creature pages with drops, places, spawn conditions and the debuffs they can carry, and their preview, and verify every creature reference on item pages links and the creature count in the recorded file drops to zero

## 4. Places

- [ ] 4.1 Compare and build place pages with the creatures, containers, pickups, merchants and people each place holds, and their preview, and lower the recorded place count to zero

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

## 10. Integration

- [ ] 10.1 Rebuild real data, run every check, review changed pages at 1440, 768 and 390 px with the `visual-review` agent, and record the review in the commit bodies
