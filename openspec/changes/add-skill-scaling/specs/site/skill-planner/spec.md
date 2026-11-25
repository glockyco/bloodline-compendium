# Spec Delta

## ADDED Requirements

### Requirement: Skill damage in builds
The planner SHALL show each hotbar skill's damage and mana cost from the build's passive ranks and levels with the shared rules, and SHALL show an input the build does not hold, such as the weapon before builds have Equipment, as unknown.

#### Scenario: Ranked damage passive
- **WHEN** a build with Frostbite at rank 5 puts Ice Shard on its hotbar
- **THEN** the planner shows Ice Shard's damage 10% above its base

#### Scenario: Weapon skill before Equipment
- **WHEN** a build puts Heavy Sweep on its hotbar
- **THEN** the planner shows its damage as unknown until the build holds a weapon
