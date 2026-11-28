# Proposal

## Why

Readers find their way through the compendium by following names, and too many names lead nowhere. Most names on item and skill pages belong to kinds without pages: 951 recipes, 214 pickup spots, 179 creatures, 106 merchants, 90 places, 82 people, 67 quests, 64 containers and 27 settlement buildings show as plain text. Text the catalog writes is checked for unlinked names only in known issue and mechanic text. Hover previews cover five kinds, and linked names appear in four different styles. Nothing measures any of this, so each new feature adds gaps that a reader finds first.

## What Changes

- A link coverage check reads every text the catalog and site write and reports each name of a known entity that it shows without a link, and each kind's count of names shown as plain text.
- The build records today's counts and fails when a count rises or a text gains an unlinked name. Recorded counts drop as pages land.
- A list of wanted pages ranks names that text uses but no entity holds, such as Divine Bond, Soulsap and Arcane Enchantment.
- Every kind with pages gets a hover preview, and the build fails for a kind with pages but no preview. Resources, professions and mechanics get theirs first.
- One look for entity names: a link is link blue and underlined whether or not it has an icon, and a name without a page is never link blue. A layout check fails on any other style.
- Pages for the kinds readers meet without pages today: creatures, places, people and merchants, quests, effects (debuffs, weapon enchantments and armor attunements), containers, settlement buildings and recipes. Each kind is compared with screenshots before it ships and ships with its preview.
- Comparison checks also check the links and previews in their screenshots.

## Capabilities

### New Capabilities

- `catalog/link-coverage`: what counts as a linked name, the plain-text counts per kind, the ratchet, and the wanted pages list.

### Modified Capabilities

- `site/presentation`: every kind with pages has a preview, and entity names have one style that a check enforces.
- `catalog/entity-graph`: the kinds readers meet in published data have pages.

## Impact

- Catalog: `LinkedText`, `CatalogBuilder` coverage and report, a recorded counts file under `coverage/`, new producers for each page kind.
- Site: `EntityLink`, previews and `TooltipPresenter`, new routes and page components for each kind, the layout check.
- Publication: `scripts/publication-gate.mjs` and `scripts/entity-graph-gate.mjs`.
- Process: the comparison-check agent's instructions gain the link and preview check.
