# Spec Delta

## ADDED Requirements

### Requirement: Skill scaling on skill pages
A skill page SHALL state what its damage and mana cost scale with, each input with its effect at full ranks or with its multiplier, and SHALL say when its damage is fixed in this build or when its scaling is unknown. A coefficient that differs from the game's text SHALL show the applied value with its known issue.

#### Scenario: Elemental skill
- **WHEN** a reader opens Ice Shard
- **THEN** the page says its damage rises with the Cryomancy damage passives, up to 75% more at full ranks, and links them

#### Scenario: Fixed melee skill
- **WHEN** a reader opens a melee skill whose damage reads nothing
- **THEN** the page says its damage is fixed in this game version

### Requirement: Passives show the skills they affect
A scaling passive's anchor and preview SHALL list the skills it affects, linked to their pages.

#### Scenario: Fire Affinity
- **WHEN** a reader follows a link to Fire Affinity
- **THEN** its anchor lists the Pyromancy skills its ranks raise
