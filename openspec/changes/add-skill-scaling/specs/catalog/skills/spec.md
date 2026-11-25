# Spec Delta

## ADDED Requirements

### Requirement: Skill damage inputs
Each skill SHALL be published with the damage base its hit uses and the inputs that change its damage, each typed and with its coefficients, such as a school's damage passives, the equipped weapon's damage with a multiplier, the unarmed rating or a discipline level, as far as the game's code and a lab run agree. A skill whose damage changes with nothing SHALL be marked fixed, and a skill whose landing hit the lab has not confirmed SHALL mark its scaling unknown.

#### Scenario: Elemental skill
- **WHEN** a lab run shows Ice Shard's hit growing with the Cryomancy damage passives
- **THEN** Ice Shard is published with its base and those five passives with their per-rank coefficients

#### Scenario: Weapon skill
- **WHEN** a lab run shows Heavy Sweep dealing three times the equipped weapon's damage
- **THEN** Heavy Sweep is published with the weapon damage input and multiplier 3

#### Scenario: Bonus the hit drops
- **WHEN** the code computes Spire Barrage's bonus but the lab shows a fixed hit landing
- **THEN** Spire Barrage is published as fixed, and the difference is a known issue if the game's text promises the bonus

#### Scenario: Tooltip base differs
- **WHEN** a lab run shows Ember Flick's hit using 35 while its tooltip says Base Damage 45
- **THEN** Ember Flick is published with base 35 and the tooltip's 45 as a known issue

### Requirement: Mana cost inputs
Each skill SHALL be published with the passives that reduce its mana cost and their per-rank coefficients, as a lab run measures them.

#### Scenario: Pyromancy mana passive
- **WHEN** a lab run measures Fire Efficiency at rank 5
- **THEN** its published coefficient reproduces the mana the cast spent

### Requirement: Passives name the skills they affect
Each passive that changes a skill's damage or mana cost SHALL be published with the skills it affects.

#### Scenario: Fire Affinity
- **WHEN** Fire Affinity raises every Pyromancy skill that uses the school's damage passives
- **THEN** Fire Affinity is published with those skills
