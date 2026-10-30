# Proposal

## Why

The reference needs the disciplines, skills, passives and spellsongs that explain character progression. A skill planner makes those references actionable using the same verified rules rather than a second model of the game.

## What Changes

- Publish progression entities, their reachable level-based trees, unlock conditions and reverse relations through the existing entity graph.
- Add searchable discipline, skill, passive and spellsong pages with facts, citations, coverage gaps and shared entity tooltips.
- Add the planner's first part: character, discipline and attribute levels, unlocked skills, passive ranks and separate point balances.
- Add versioned compressed build links, browser-stored characters and optional active-character cues on otherwise neutral reference pages.
- Make builds easy to discuss where players talk: a hotbar, a link preview for each shared build, a copyable text summary, and link previews for skill pages.
- Extend the shared rules and recorded lab checks for progression. Preserve unknown results where observations do not establish a rule.

## Capabilities

### New Capabilities

- `catalog/skills`: progression entities, level-based trees, facts, claims, conditions and route-scoped planner inputs.
- `site/skill-reference`: discipline, skill, passive and spellsong reference pages, browsing, search and tooltips.
- `site/skill-planner`: verified progression planning, build links, browser characters and active-character page context.

### Modified Capabilities

None. The new capabilities consume `catalog/entity-graph`, `site/game-rules`, `site/presentation`, `verification/game-claims`, `verification/runtime-lab` and `site/known-issues` without replacing their contracts. They extend the reference published by `publish-item-reference` without changing item behavior.

## Impact

- Extend catalog producers, generated site-data types, the kind registry, shared rules and their recorded-result adapters.
- Add static reference routes and a planner route with a separately budgeted payload. Browser state remains local and shared links contain only explicit build state.
- Add a Worker in front of the static site that answers build links with each build's link preview and image. Every other route stays a static asset.
- Record game-derived data and lab outputs only in Git-ignored build artifacts. CI uses invented fixtures.
- Implementation depends on the architecture foundation and the item publication contract. This change contains planning artifacts only.
- Crafting, equipment, weapon building, runes, enchantments, armor and mobility totals, combat simulation and save reading are out of scope. They are not prerequisites for skill planning.
