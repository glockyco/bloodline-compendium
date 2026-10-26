# Spec Delta

## Purpose

Defines the ways to get items that the catalog must read beyond placed actors and data tables, the context a recipe needs, and how conditions and places are named for players.

## ADDED Requirements

### Requirement: Sources from hardcoded and runtime mechanisms
The catalog SHALL publish sources from the hardcoded crafting mappings in `Crafting:Get Correct Item`, the default stock of faction currency shops, stables, contract scroll pools, seasonal gift placements, archaeology rewards and level reward variables, each with the requirements the code establishes. A mapping whose recipe row does not exist SHALL be listed in the maintainer report, not published.

#### Scenario: Hardcoded weapon recipe
- **WHEN** `Crafting:Get Correct Item` maps Copper Claymore to a recipe row with positive quantities
- **THEN** Copper Claymore lists that recipe as a source with its ingredients

#### Scenario: Faction shop default stock
- **WHEN** the Lionsmane shop's default content sells the Lionsmane Cuirass for 350 Iuba
- **THEN** the cuirass lists that shop as a source with a price of 350 Iuba

#### Scenario: Missing recipe row
- **WHEN** the code maps Atacamite Ingot to a row name the recipe table lacks
- **THEN** no source is published and the maintainer report lists the mapping

### Requirement: Recipe context
A crafted source SHALL state its station, its discipline and required level, the experience it grants, its output quantity and how the recipe is learned, as far as the game's code and data establish them.

#### Scenario: Cooked food
- **WHEN** Apple Pie is cooked from 3 Apples and 1 Bread at a cooking fire for 35 cooking experience
- **THEN** its source states the cooking fire, the ingredients with quantities, the output and the experience

### Requirement: Conditions in player terms
A condition SHALL describe what a player does or waits for, in game terms, such as talking to a character, completing a quest, building a settlement building or the calendar month. A dialogue state, quest flag or other identifier SHALL be translated to the action that sets it, or left out of pages when its meaning is unknown. A condition that only says that further requirements may exist SHALL NOT be published per source. The maintainer report SHALL list the sources whose requirements are not fully read.

#### Scenario: Dialogue state
- **WHEN** a merchant's stock requires the dialogue state SpokeWithScarecrow, which the Scarecrow's greeting sets
- **THEN** the condition reads as talking to the Scarecrow first

#### Scenario: Unresolved dialogue predecessor
- **WHEN** earlier dialogue checks may gate a source and their paths are not read
- **THEN** the item page shows no warning for it and the maintainer report lists the source

### Requirement: Settlement and place names
A source in a settlement building SHALL name the building as part of the player's settlement instead of reporting an unknown location. Places SHALL take names from travel, entrance and containing-level relationships, and reusable event maps SHALL be named by the encounter or area that uses them. Places that no published source uses SHALL not need a name.

#### Scenario: Settlement Armorer
- **WHEN** the settlement Armorer sells Leather Pants
- **THEN** the source reads as the Armorer in the player's settlement, after it is built

#### Scenario: Interior reached through a door
- **WHEN** a level is entered through a door in the Mages College
- **THEN** the level takes a name derived from that entrance
