# Design

## Context

`add-skill-reference` publishes disciplines, skills, passives and spellsongs, moves the catalog and the site onto kinds, and adds the proficiency point and passive rank cost rules. This change builds the planner on them. Evidence comes from build 24464547 and lab runs under `artifacts/24464547/runtime/`.

### Measured rules

- A new character starts at level 1 with 0 skill points and 0 level up points. Each level grants 1 skill point and 3 level up points, with no cap through level 122 (runs progression2 and passives1).
- Character experience to the next level runs 150, 200, 250, 325, 375, 450 and reaches 12,050 at level 100 (run progression2). The rule in `site/src/lib/rules/experience.ts` cites the function.
- Discipline levels had no cap past level 1,000. Curves differ by group: the elemental schools start at 152, 156, 161, 168, 177, Arcane, Blood Magic, Druidism and Vampirism at 65, 255, 575, 1025, weapons at 50, 400, 700, 1100, Dagger at 25, 225, 625, 900, Unarmed at 60, 255, 570, 1020 (runs progression3 and grants1).
- The level up window shows nine attributes: Constitution, Dexterity, Endurance, Fortitude, Intelligence, Strength, Vitality, Willpower and Wisdom. Intelligence is stored as `Agility Level`. Each starts at 1, each point costs 1 level up point, and the decrease buttons refund (run passives1).
- From 150 health, 200 stamina and 100 mana, a point gives: Constitution 15 health and 15 stamina, Dexterity 15 stamina, Endurance 30 stamina, Fortitude 25 health, Intelligence 25 mana, Strength 10 stamina, Vitality 10 health, Willpower 5 mana, Wisdom 10 mana, linear through level 8 (run passives1).
- Regeneration modifiers are 0.025 x sqrt(level - 1) for Constitution and Wisdom and 0.045 x sqrt(level - 1) for Vitality and Willpower. Health Regen Speed is 1 minus the Constitution and Vitality modifiers, and Mana Regen Delay 1 minus the Willpower and Wisdom modifiers (run passives1).
- The skill bar holds two rows of five slots, switched by a key (run passives1).
- Skill unlock, refund, passive rank costs and proficiency grants are the reference change's rules.

### How players share builds

Builds are shared as prose with the attribute split, the disciplines, and the hotbar skills with what each does, as in [Meta Build?](https://steamcommunity.com/app/1159290/discussions/0/4040356333293170223) and [What skills/build for your first character?](https://steamcommunity.com/app/1159290/discussions/0/4360121479831850670). Afallon's planner shares a compressed, versioned build as a link.

## Goals / Non-Goals

**Goals:**

- A build is valid in the planner exactly when the game accepts it, by the same rules the pages show.
- A shared build reads in Discord as a link and in a forum post as text.
- One planner state model, evaluated by shared rules, feeds the editor, the link, the text and the page cues.

**Non-Goals:**

- Equipment, crafted weapons, runes and enchantments, which `add-crafting-and-equipment` adds to the same builds.
- Experience totals and how to earn experience. Mechanics guides cover the curves.
- Per-build link previews, as `plan.md` keeps for later.
- Reading saves.
- Spellsongs in builds, because the game plays them on the lute rather than placing them on the skill bar or buying them with points.

## Decisions

### One state, one evaluator

The planner keeps one typed state: game build, character level, discipline levels, attribute points, unlocked skills, passive ranks and hotbar slots, each keyed by catalog key. One evaluator in `site/src/lib/planner/` runs the rules over the state and returns balances, derived values and diagnostics. The editor, link, text and page cues read its result and never compute themselves.

Rules for character level grants, attribute effects and regeneration join `site/src/lib/rules/` with worded steps, citations and recorded vectors, and `rules:check` compares them with the recorded runs. Attribute effects are published only within the measured range, and the lab measures a high point count before the planner allows it.

### Inputs as their own payload

The catalog writes the planner's inputs as one payload: disciplines with their rows, slots with required levels and checked disciplines, passives with rank costs, skills with names and icons, and rule inputs. It loads only on the planner route, under a budget the payload gate enforces.

### Blood Magic points

Blood Magic proficiency points depend on the game's gore setting and the blood pool rituals, not on its level. The planner asks for the Blood Magic points the character has, links the known issue, and checks Blood Magic passives against that number.

### Skills from outside the trees

A skill learned from a book, a conversation or an event can sit on the hotbar. The planner lets a reader mark such skills as known. Whether learning them costs a skill point is observed in the lab before the planner decides.

### Edits keep invalid choices

Lowering a level or removing points keeps the choices that no longer fit, marks each with what it lacks, and shows the build as invalid until the reader fixes it. A removed skill or passive rank is a planning edit. The game refunds skills, so this matches the game for skills. Whether it refunds passive ranks is observed before the planner says so.

### Build links

A build travels as `/planner/?build=v1.<data>`, where the data is the state as compact JSON, compressed with raw deflate and written in base64url. Entities are written as short digests of their keys, and publication fails when two keys share a digest. The decoder limits the encoded and expanded sizes, rejects unknown versions and malformed data, and keeps keys missing from the current catalog as unresolved entries without substituting others. A whole link stays within 1,500 characters, inside Discord's 2,000-character message limit with room for a comment.

The query is used rather than the fragment because a server receives it, so per-build previews can be added later without changing links. The static site serves the planner for any query, so no request-time code is needed now.

### Copy as text

"Copy as text" writes the game build, the character level, the attribute split, the disciplines with their levels, the hotbar skills in order with their page links, and the build link, in the order players post them.

### Characters in the browser

A versioned store in local storage keeps several named characters and the active one. Creating, renaming, duplicating, selecting and deleting are explicit. Opening a build link shows a preview that changes no stored character until the reader saves it. A failed write keeps the plan in memory, says it was not saved, and still offers the link.

Discipline and skill pages show cues for the active character, such as its level against a skill's required level, from the same evaluator. Without an active character or without JavaScript the pages stay neutral.

### Presentation

The planner's layout, the attribute panel, the hotbar, cues and the text format go through Plannotator comparisons before they ship. The tree panels reuse the discipline page's rows, so a reader plans in the layout the game shows.

## Risks / Trade-offs

- Attribute effects beyond the measured range could bend. The lab measures high counts, and the planner shows unknown beyond the measured range.
- Links outlive game updates. Each link names its game build, and missing keys stay visible.
- Local storage can be cleared. The link remains the portable form.

## Migration Plan

Build after `add-skill-reference` ships. No existing planner, store or link format exists. Rollback removes the route, payload and cues together.

## Open Questions

- Whether learning a skill from a book or conversation costs a skill point.
- Whether the game refunds passive ranks.
- Whether attributes can be lowered below their starting value of 1, and attribute effects above 8 points.
- What the HealthButton of the level up window does for a player, which adds 25 health and a Fortitude point without raising Fortitude.

## Where Findings Live

Use the architecture change's [Where findings live](../archive/2026-11-01-establish-reference-architecture/design.md#where-findings-live). Rules and their vectors live in `site/src/lib/rules/`, run outputs under the build's artifacts, defects in `issues/known-issues.json`.
