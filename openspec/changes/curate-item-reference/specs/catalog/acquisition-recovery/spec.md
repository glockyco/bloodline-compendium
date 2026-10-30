# Spec Delta

## Purpose

Defines the context a recipe needs and how conditions and places are named for players.

## ADDED Requirements

### Requirement: Recipe context
A crafted source SHALL state its station, its discipline and required level, the experience it grants and its output quantity, as far as the game's code and data establish them.

#### Scenario: Cooked food
- **WHEN** Apple Pie is cooked from 3 Apples and 1 Bread at a cooking fire for 35 cooking experience
- **THEN** its source states the cooking fire, the ingredients with quantities, the output and the experience

### Requirement: Conditions in player terms
A condition SHALL describe what a player does or waits for, in game terms, such as talking to a character, completing a quest, building a settlement building or the calendar month.

#### Scenario: Calendar condition
- WHEN a source is available only in October
- THEN its condition names October

### Requirement: Condition identifiers describe their actions
A dialogue state, quest flag or other identifier SHALL be translated to the action that sets it, or left out of pages when its meaning is unknown.

#### Scenario: Dialogue state set by another conversation
- **WHEN** a source requires a dialogue state that another character's conversation sets
- **THEN** the condition reads as talking to that character first

#### Scenario: Dialogue state set on the way
- **WHEN** the Hallowed Eve Merchant's stock requires the dialogue state that asking what he sells sets in the same conversation
- **THEN** the source carries no condition for it

### Requirement: Unresolved source conditions stay in the maintainer report
A condition that only says that further requirements may exist SHALL NOT be published per source. The maintainer report SHALL list the sources whose requirements are not fully read.

#### Scenario: Unresolved dialogue predecessor
- **WHEN** earlier dialogue checks may gate a source and their paths are not read
- **THEN** the item page shows no warning for it and the maintainer report lists the source

### Requirement: Settlement and place names
A source in a settlement building SHALL name the building as part of the player's settlement instead of reporting an unknown location. Places SHALL take names from travel, entrance and containing-level relationships, and reusable event maps SHALL be named by the encounter or area that uses them. Places that no published source uses SHALL not need a name.

#### Scenario: Settlement Armorer
- **WHEN** the settlement Armorer sells Leather Boots
- **THEN** the source reads as the Armorer in the player's settlement, after it is built

#### Scenario: Interior reached through a door
- **WHEN** a level is entered through a door in the Mages College
- **THEN** the level takes a name derived from that entrance
