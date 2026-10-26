# Spec Delta

## Purpose

Publishes disciplines, skills, passives and spellsongs as connected reference entities and supplies the verified progression inputs used by the skill planner.

## ADDED Requirements

### Requirement: Progression identities
The catalog SHALL register disciplines, skills, passives and spellsongs in the entity graph with page routes, search labels and stable game identifiers. Skill keys SHALL use class paths. Other keys SHALL use their source identifiers, qualified by the owning asset where necessary, never display names. Repeated references to one game identity SHALL resolve to one entity.

#### Scenario: Same skill in several slots
- **WHEN** several tree slots reference the same skill class
- **THEN** they refer to one skill entity and retain distinct slot conditions

#### Scenario: Renamed passive
- **WHEN** a passive's display name changes without changing its source identity
- **THEN** its key stays unchanged and its readable slug follows the graph's qualification rules

### Requirement: Reachable level-based trees
Discipline trees SHALL follow the level-based trees reached from the skill window. Slots SHALL carry skill references and required discipline levels. Passive entries SHALL carry their required levels, point costs and maximum ranks. Unreachable older prerequisite trees SHALL NOT supply planner unlock rules. A menu label SHALL NOT establish an entity association contrary to its bound destination.

#### Scenario: Biomancy menu destination
- **WHEN** the Magic menu's Biomancy button opens the Arcane tree
- **THEN** the catalog records the Arcane destination rather than inventing a separate Biomancy tree

### Requirement: Skill and spellsong facts
Skills SHALL expose known descriptions, icons, mana costs, cooldowns, cast times and source tab classifications. Spellsongs SHALL expose their known facts and game-established relations. Missing sources, icons, requirements or effects SHALL remain explicit gaps. Neither a table tab nor the absence of a tree slot SHALL establish that a skill is obtainable or unobtainable.

#### Scenario: Skill outside reachable trees
- **WHEN** a named skill has no established tree unlock
- **THEN** its page remains available with an unknown acquisition gap and the planner does not invent an unlock

### Requirement: Shared progression relations
Discipline membership, skill unlocks, passive availability and established spellsong connections SHALL be graph relations computed once and projected in both directions. Each unlock SHALL carry its typed discipline-level condition and point cost. Unsupported conditions SHALL remain unknown and appear in coverage.

#### Scenario: Skill threshold
- **WHEN** a tree slot unlocks a skill at a discipline level
- **THEN** the discipline and skill documents expose the same relation, threshold and cost

### Requirement: Progression claims
Every progression fact and rule SHALL carry the existing claim type and build identity. Applied meanings SHALL cite fully parsed consuming functions by package, function and fingerprint. Unexplained disagreements SHALL appear in coverage. Only reproduced game defects SHALL enter the existing issue registry and reach affected pages through shared issue notes.

#### Scenario: Passive description without verified effect
- **WHEN** a passive description states an effect whose application has not been established
- **THEN** the description remains marked game text and the applied effect remains unknown rather than becoming a computed bonus

### Requirement: Separate planner inputs
The catalog SHALL publish keyed trees, slots, ranks, costs, conditions, level domains and rule inputs as a planner payload separate from reference page documents. The payload SHALL identify its game build and schema version. Unknown values SHALL remain distinguishable from zero and unlimited values.

#### Scenario: Unread maximum rank
- **WHEN** a passive's maximum rank cannot be established
- **THEN** the payload marks that maximum unknown and does not permit unlimited ranks
