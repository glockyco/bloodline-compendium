# Proposal

## Why

The compendium is meant to become a full reference for The Bloodline, covering items, people, creatures, places, quests, skills, recipes, guilds, mounts, the settlement and the rules behind them, like the Afallon and Ancient Kingdoms compendiums. Today it is an item reference: sources name merchants, creatures and places as plain text, published facts come from fields nobody traced to the code that uses them, and the first in-game check found two armor values per item and food tooltips that contradict their descriptions. Both sibling projects rebuilt their entity model, cross-links, page structure and evidence pipeline after hundreds of commits. Settling these foundations before the second content kind costs a fraction of retrofitting them.

## What Changes

- Model published content as one graph of entities with keys that are the game's own identifiers, readable slugs, typed references, typed conditions such as calendar events or guild ranks, and reverse relations computed once by the catalog. A reference to a kind without pages yet stays a typed, unlinked reference, so the link appears when that kind is published. One registry of kinds feeds routes, search filters, the sitemap and page metadata.
- Classify every published fact as an extracted value, a formula, an observation or a known unknown. Name the game code each fact rests on by package and function with a fingerprint of that function, and recheck facts whose functions change in a new build.
- Implement each game rule once in the site, with worded steps, so pages, formulas, calculators and the planner compute the same results. Check each rule against results recorded by calling the game's own functions.
- Add a lab runtime tool with named commands that drive the game's own functions in a separate modded copy of the game, read the text the game fills into its widgets, and write each run's results to an immutable run directory.
- Decide the owner of every disagreement between the catalog, the game's code and the running game before recording it. Fix catalog and tool defects at their producer. Record reproduced game defects in a typed registry that feeds notes on affected pages, a Known Issues page and drafts for the game's developer.
- Build the reader presentation on closed token scales, shared primitives, one stat vocabulary and written voice rules, enforced by stylelint, layout checks and a fixture page. Present item facts in the order and wording of the game's own tooltip, and show entity tooltips on every entity link.
- Prepare the character planner: keys a save and a build link can refer to, a spike on how skill trees, points and levels work, and the constraints the planner's own changes follow. The planner itself, starting with skills, is the second change after the items ship.
- **BREAKING** for the site-data contract: sources, uses and facts reference entities by key instead of carrying display strings. Item URLs do not change.
- Revise the presentation requirements of `publish-item-reference`, which require a neutral theme without webfonts, to match the visual identity.

## Capabilities

### New Capabilities

- `catalog/entity-graph`: entity kinds, stable keys and slugs, typed references, reverse relations, unresolved references and the integrity of the published graph.
- `site/game-rules`: one implementation per game rule with worded steps, used by pages, calculators and the planner, and checked against the running game.
- `verification/game-claims`: claim types, code citations with fingerprints, the ownership of disagreements, and rechecking claims after a game update.
- `verification/runtime-lab`: running the lab copy of the game safely and reproducibly through named commands, with recorded runs.
- `site/known-issues`: the registry of reproduced game defects, notes where readers meet them, the Known Issues page and developer report drafts.
- `site/presentation`: token scales, shared primitives, the item tooltip presentation, entity tooltips, the stat vocabulary, voice rules and the checks that enforce them.

### Modified Capabilities

None in `openspec/specs/`. The unarchived change `publish-item-reference` revises its `site/item-reference` presentation requirements before it archives.

## Impact

- C# catalog: entity keys and references for every source mechanism, reverse relations, claim metadata and code citations. The site-data JSON Schema, generated TypeScript types and invented fixtures change with it.
- Site: entity link, tooltip and reference components, the rules module, the stat vocabulary, a fixture page for primitives, a Known Issues page, stylelint, and layout checks in Playwright.
- Runtime: the bridge mod, host and capture scripts move from Git-ignored directories into the repository. UE4SS stays outside Git and is pinned by checksum. Game files, saves, captures and run results stay in the ignored `artifacts/` directory.
- Process: procedures for runtime checks and game issues, and a recorded rule that findings, decisions and open work live in OpenSpec, the registry and the vault rather than in session history.
- Later content kinds and the map arrive in their own changes, ordered in design.md.
