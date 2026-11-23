# Proposal

## Why

Players share builds on Steam and Reddit as prose: how they split attributes, which disciplines they combine and which skills sit on their hotbar. No planner exists for the game. With the skill reference published, a planner can check a build against the game's own point rules and share it as a link and as text.

## What Changes

- A planner page that edits one character: character level, discipline levels, attribute points, unlocked skills, passive ranks and the ten hotbar slots.
- Point balances for skill points, level up points and each discipline's proficiency points, and the health, stamina, mana and regeneration the attributes give, from shared rules measured in the lab.
- Invalid choices stay visible with what they lack after a level is lowered.
- Build links in the query that fit a Discord message, a fixed link preview, and "Copy as text" in the form players post.
- Several characters kept in the browser, and cues on discipline and skill pages for the active one.

## Capabilities

### New Capabilities

- `site/skill-planner`: planning a character's progression, its point balances and attributes, sharing builds and keeping characters in the browser.

### Modified Capabilities

- `catalog/skills`: the catalog writes the planner's inputs as their own payload.

## Impact

- `site/src/routes/planner/`, `site/src/lib/planner/`, `site/src/lib/rules/`.
- A planner payload written by the catalog, with its budget in `scripts/payload-gate.mjs`.
- Discipline and skill pages gain active-character cues.
- `tools/runtime/checks/progression.lua` vectors for the rule check.
