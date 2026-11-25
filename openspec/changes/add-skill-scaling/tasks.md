## 1. Evidence

- [x] 1.1 Trace every published skill's damage, mana cost and effect inputs in the code, with function and statement citations, in `artifacts/24464547/runtime/skill-scaling.md`
- [x] 1.2 Write `tools/runtime/checks/skill-damage.lua`, which casts a hotbar skill through its own input at a still enemy and reads the target's Health per frame, the caster's mana and every actor the cast spawns
- [x] 1.3 Measure Ice Shard at Cryomancy damage ranks 0, 1, 2 and 5 and with two different passives, and Spire Barrage, Earth Spikes, Inferno Lance and Emberspark with and without ranks
- [ ] 1.4 Measure Heavy Sweep with two weapons, an unarmed skill at two Unarmed levels, Heartwrench at two Vampirism levels, an undead summon with and without its damage and health passives, Bard's Bolt with two lutes, and Mending Melody with and without Lute's Embrace
- [ ] 1.5 Measure an Arcane and a Necromancy skill with and without their older passives, and a Druidism form with Bestial Fury
- [ ] 1.6 Measure the landing hit of the 16 skills whose tooltip's Base Damage differs from the base their formula uses. Walking Capacitor is the one left, because it channels only while its key is held.
- [ ] 1.7 Measure the mana spent by one skill per elemental school at mana passive ranks 0 and 5, and whether Lightning Fury and Thunderous Smite charge twice their cost and Ice Spike charges nothing
- [ ] 1.8 Record each result and its run in design.md, and each lab-confirmed difference from a passive's description in `issues/known-issues.json` as the game-issues skill describes
- [ ] 1.9 Measure Ice Shard and Spire Barrage at rank 5 of each Cryomancy damage passive not yet measured (Icebreaker, Arctic Might, Frost Mastery), and one skill per other elemental school at rank 5 of each of its five damage passives, and identify Spire Barrage's separate 25 hit

## 2. Catalog

- [ ] 2.1 Publish each skill's applied damage base and typed inputs, with the tooltip's base as a known issue where they differ, with their coefficients, unknown where the lab has not confirmed the hit, and verify Ice Shard, Spire Barrage, Heavy Sweep and an unarmed skill
- [ ] 2.2 Publish each scaling passive's coefficient and the skills it affects, and verify Fire Affinity and Fire Efficiency
- [ ] 2.3 Add the inputs to the planner payload under its budget

## 3. Rules and pages

- [ ] 3.1 Add skill damage and mana cost rules to `site/src/lib/rules/` with worded steps, citations and the lab runs in `rules:check`
- [ ] 3.2 Compare how a skill page shows its scaling, with options for an elemental skill, a weapon skill and a fixed-damage melee skill, and build the answer
- [ ] 3.3 Show on each scaling passive the skills it affects, after the comparison, and verify Fire Affinity's anchor
- [ ] 3.4 Show each hotbar skill's damage and mana cost in the planner from the build's passive ranks and levels, and weapon scaling as unknown until builds hold a weapon
- [ ] 3.5 Compare where to explain Frozen Ground and how to link to that explanation from site-authored text without changing game descriptions

## 4. Integration

- [ ] 4.1 Run every check, rebuild real data, review skill, discipline and planner pages on real data at 1440, 768 and 390 px with the `visual-review` agent, and record the review in the commit bodies
