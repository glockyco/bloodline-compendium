## Context

Findings come from Steam build 24464547. `artifacts/24464547/runtime/skill-scaling.md` holds the trace of every published skill's damage path with its function and statement citations. `artifacts/24464547/runtime/skill-damage.md` holds the lab measurements of `tools/runtime/checks/skill-damage.lua`, in runs `skill-damage-recovery` and `skill-damage-followups`, which cast each skill from the hotbar at a still Goblin Brute with zero Armor and read its Health every frame.

What the code does:
- `PlayerPassiveLevelStatComponent` has a damage helper and a mana helper per elemental school. Each reads the ranks of five named passives from the saved passive list, which is the passive's rank and not the discipline's level. The damage helper sums 0.02, 0.02, 0.03, 0.03 and 0.05 per rank, and a usual cast deals `base * (1 + sum)`. The mana helper sums 0.024, 0.024, 0.03, 0.036 and 0.036 per rank, and `Skill:Consume Mana` charges `ManaCost * (1 - sum)` for those five schools.
- Several spells compute the bonus and then hit with something else: Spire Barrage hands its ice spear a literal 30, Earth Spikes sets no damage on its spike actor, and Inferno Lance and Spark replace the result with an older path. Emberspark has a second fixed hit beside its scaled one.
- `Skill:Check Damage Passives` holds older per-school passives, such as Holy Retribution for Arcane and Death's Embrace for Necromancy, which count only where a skill asks for them and uses the answer.
- Druidism's `Check Form Passives` adds 0 for Bestial Fury, while a separate helper adds 20% per rank and looks for a different first-rank name.
- Undead summons apply `base * (1 + sum)` of their own damage and health passives at spawn.
- Melee, ranged and some Vampirism skills read the equipped weapon's `WeaponInfo.Damage`, often with a multiplier, such as 3 for Heavy Sweep. Unarmed skills read the unarmed rating, `round(L + 5 + 3 * floor(L / 5))` for Unarmed level L. Heartwrench reads the Vampirism level.
- Bard's Bolt and Harmonic Burst deal the lute's damage plus 25. Mending Melody heals by the Lute level, and Lute's Embrace doubles the heal where its description says 50% more.
- No attribute multiplies a skill's damage, and no passive shortens cooldowns in general.
- For 16 of the 56 skills whose traced formula names its base, the base differs from the Base Damage the game's tooltip shows, which skill pages publish. Ember Flick's card says 45 and its hit multiplies 35, Meteor Form's card says 100 and its hit multiplies 300.

What the lab showed:
- Elemental damage ranks add before they multiply. Ice Shard hit for 25, 25.5, 26 and 27.5 at Frostbite ranks 0, 1, 2 and 5, and for 33.75 with Frozen Dominion 5 as well, which is `25 * (1 + 0.10 + 0.25)`. Ice Spike rose from 35 to 38.5 at Frostbite rank 5.
- Every damage passive applies with the coefficient the code names. Run `skill-lab-elemental-ranks` set each passive to rank 5 alone, for Ice Shard, Ember Flick, Snap Shard, Airblade and Thunderous Smite, and each hit rose by 10%, 10%, 15%, 15% and 25% in the passives' order.
- Four spells ignore the bonus. At rank 0 and at full ranks of their school's five damage passives, Spire Barrage's spears hit for 30, Earth Hammer for 25 and 100, Inferno Lance for 150 plus a 50 explosion, and Emberspark for its fixed 25. Earth Spikes' direct hit stays at 100, while a second hit of 45 rose to 78.75 in both sweeps.
- The tooltip's Base Damage is not the hit for 15 of the 16 skills the trace named: Ember Flick hit for 35 against a tooltip of 45, Meteor Form for 300 against 100, Lightning Fury for 75 against 35. Walking Capacitor did not channel headless and stays unmeasured.
- Lightning Fury and Thunderous Smite took 60 and 70 mana in one step, twice their costs of 30 and 35, and Ice Spike took none. Task 1.7 measures mana on purpose before this becomes a claim.

What the compendium has:
- Skill pages show the game card with base damage, cooldown and mana or stamina cost, from `S_SkillInfo`.
- Discipline pages show each passive with its game description, ranks and costs.
- The Character Planner holds a build's passive ranks, the discipline levels and the hotbar, so the elemental and older passive bonuses can be computed for a build now. A build has no weapon until `add-equipment-planner`.

## Goals / Non-Goals

**Goals:**

- A player can read on a skill page what makes its damage grow, and by how much at full ranks.
- A player can read on a passive which skills it affects.
- The planner shows the damage and mana cost a build's skills have.
- Every published coefficient rests on a lab measurement, and every lab-confirmed mismatch with a description is a known issue.

**Non-Goals:**

- Damage per second, rotations or combat simulation.
- Damage-over-time totals, which depend on the target's status handling. Pages state the tick and duration the code supplies, where the lab confirms them.
- Enemy armor and resistances, which arrive with creatures.

## Decisions

### Inputs, not formulas per skill

The catalog publishes each skill's damage as a base and a list of typed inputs, such as the Pyromancy damage passives, the equipped weapon times 3, or the unarmed rating, with each input's coefficients. Rules in `site/src/lib/rules/` turn base and inputs into a number with worded steps. A skill whose traced hit drops the bonus publishes the bonus only after the lab shows which number lands, and a skill whose hit path the trace did not resolve publishes its scaling as unknown. This keeps one rule per kind of input, as `site/game-rules` requires, instead of a formula per skill.

### Lab before publishing

Each kind of input ships only after `tools/runtime/checks/skill-damage.lua` measures it against a still enemy, as the trace's lab plan describes: Ice Shard as the control for elemental ranks at ranks 0, 1, 2 and 5 and with two passives, Spire Barrage, Earth Spikes and Inferno Lance as suspected drops, Heavy Sweep for weapon damage with two weapons, an unarmed skill at two Unarmed levels, an undead summon with and without its passives, Bard's Bolt with two lutes, Mending Melody with and without Lute's Embrace, and mana spent at ranks 0 and 5 of one mana passive.

### One sentence per skill, published only where measured

The five damage passives of a school apply to every skill alike, so a skill page says in one sentence that they raise its damage, links them, and gives the 75% they add up to, which Ice Shard confirmed at 43.75 from 25. A table of the five passives on every skill page would repeat the discipline page. Comparison 86 had chosen such a table while three passives were unmeasured.

The code alone does not tell which skills use the bonus, because a skill can compute it and hit with another number, as Spire Barrage does, and one skill can have a hit that scales beside hits that do not, as Ember Flick's impact and burn do. `tools/runtime/checks/skill-ranks.sh` therefore casts every skill whose code asks for the bonus at rank 0 and at full ranks, and the catalog publishes the sentence only where a hit rose by the full bonus. Each cast starts from the same place after the earlier casts' leftovers are destroyed, because burning ground and similar effects of one skill otherwise hit the next skill's enemy, and each hit is read from what the enemy received, because two hits in one frame otherwise read as one. A skill whose cast does not reach the enemy at the sweep's distance, or that deals no damage, gets no sentence.

Skill pages reuse the item pages' Bugs section without a link beside the game card, because a link there would change both page kinds for a case the Bugs heading already serves. The passives' own descriptions already state their bonus, so passive rows list no skills.

### Applied base, not the tooltip's

A skill's published damage base is the number its hit uses, as the lab confirms it. Where the game's tooltip shows another Base Damage, the game card keeps the applied number and the page names the tooltip's number in its Bugs section, as `site/item-page` already does for items.

### Mismatches are game issues

A difference between a passive's description and what the lab measures, such as 2.4% where the description says 2.5%, is the game's defect, decided with `.agent/skills/game-issues/SKILL.md`. It goes into `issues/known-issues.json` with its reproduction, and the page shows the applied number beside the issue, as items do.

### After the game's melee revisit

The developer said the melee trees will be revisited. The pages state melee scaling as this build has it, and the update workflow's rule check reports when a cited helper's fingerprint changes, which marks the scaling for review after that update.

## Risks / Trade-offs

- A skill can hit through several actors, ticks and branches, so one "damage" per skill can mislead. Pages list each scaled hit by what the player sees, such as the projectile and the burning ground, where the lab separates them.
- Casting a hotbar skill headless needs its input action or the controller's own use function. If neither works headless, the check runs in `launch render`, which needs no window.

## Open Questions

- Whether the four spells that ignore the damage bonus are defects or the developer's intent. Pages state the fixed numbers the lab measured either way.
- How older passives such as Holy Retribution and Death's Embrace reach the skills of their trees in play.
- Whether Bestial Fury raises shapeshift damage at all.
