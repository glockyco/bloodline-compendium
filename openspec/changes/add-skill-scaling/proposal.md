# Proposal

## Why

Players ask what makes their skills hit harder. The game's developer answered on Discord that the magic trees were revisited with scaling and the melee trees were not yet, and will be in a later update. Skill pages show only the base damage, cooldown and cost from the game's tooltip. Nothing says which passives raise a skill's damage, how far, or what a melee skill's damage follows, and nobody has checked in the lab that the passives' percentages hold.

A trace of the game's code (`artifacts/24464547/runtime/skill-scaling.md`) found 192 of 592 skill actors with damage that changes:
- Aeromancy, Cryomancy, Electromancy, Geomancy and Pyromancy skills add their school's five damage passives, 2%, 2%, 3%, 3% and 5% per rank, up to +75%.
- Arcane, Blood Magic, Druidism and Necromancy skills use older named passives, and undead summons their own damage and health passives.
- Many melee, ranged and Vampirism skills read the equipped weapon's damage, unarmed skills the unarmed rating, which follows the Unarmed level, and two spellsongs the lute's damage.
- The five schools' mana passives cut mana costs by 2.4%, 2.4%, 3%, 3.6% and 3.6% per rank, while 20 of their descriptions say 2.5% and 3.5%.
- Some skills compute the bonus and then hit with a fixed number, and some passive descriptions promise more than the code does, such as Bestial Fury and Lute's Embrace.

## What Changes

- Measure the scaling in the lab: a school's damage passives against a skill that uses them and against one that seems to drop the bonus, weapon damage against a melee skill, the unarmed rating, summon passives, the lute, and the mana passives.
- Publish each skill's damage inputs from the catalog, as far as code and lab agree, and mark the rest unknown.
- Show on each skill page what its damage scales with, the passives that raise it with their effect at full ranks, and when its damage is fixed.
- Show on each scaling passive the skills it affects.
- Add the scaling rules to `site/src/lib/rules/`, so skill pages and the planner compute damage and mana cost the same way.
- Show each hotbar skill's damage and mana cost in the Character Planner from the build's passive ranks, and its weapon scaling once `add-equipment-planner` gives builds a weapon.
- Record each lab-confirmed difference between a passive's description and what the game applies as a known issue.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `catalog/skills`: skills carry their damage and mana cost inputs, and scaling passives the skills they affect.
- `site/skill-reference`: skill pages show their scaling, and passives the skills they affect.
- `site/skill-planner`: builds show their skills' damage and mana cost from the build's passive ranks.

## Impact

- Catalog: `SkillProducer` reads the scaling helpers the skills call, and site data and planner inputs gain each skill's scaling and each passive's coefficients.
- Site: skill pages, passive tooltips and anchors on discipline pages, the planner's skill bars and summary, and scaling rules in `site/src/lib/rules/` with their rule check.
- Lab: a check under `tools/runtime/checks/` that casts skills at a still enemy and reads its Health at different passive ranks, weapons and levels.
- `issues/known-issues.json`: passives whose descriptions differ from what the lab measures.
