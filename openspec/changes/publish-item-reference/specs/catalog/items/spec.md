# Spec Delta

## Purpose

The item catalog decides what the site says about each item, based only on the records of one build.

## ADDED Requirements

### Requirement: Inventory items
The catalog SHALL contain every Blueprint class under the game's item base class that a player can hold in the inventory and that has a display name. Placed interactables that share the base class, such as workbenches and containers, SHALL NOT be items.

#### Scenario: Workbench
- **WHEN** a class under the item base class is a workbench
- **THEN** the catalog has no item for it

#### Scenario: Named inventory item
- **WHEN** a class under the item base class is an inventory item with a display name
- **THEN** the catalog has an item for it

### Requirement: Item identity
Each item SHALL be identified by its Blueprint class path. Its URL SHALL derive from that identity and stay stable across builds while the class exists.

#### Scenario: Same item in two builds
- **WHEN** an item class exists in two builds
- **THEN** both catalogs give it the same identifier and URL

### Requirement: Item facts
Each item SHALL carry its display name, description, category, value and type-specific stats as the records state them. A fact that the records do not determine SHALL be marked unknown and not guessed.

#### Scenario: Weapon facts
- **WHEN** an item is a weapon
- **THEN** its entry includes its damage and attack speed

#### Scenario: Unknown fact
- **WHEN** a fact depends on a field that the records mark unknown
- **THEN** the entry marks the fact unknown

### Requirement: Item sources
Each item SHALL list the sources that the records show for it, with the place, quantity, chance or price as the data states them. Consumed inputs SHALL be kept apart from the item given.

#### Scenario: Item sold by a vendor
- **WHEN** a vendor placed in a level stocks an item
- **THEN** the item lists the vendor, the level and the price as a source

#### Scenario: Crafted item
- **WHEN** a recipe produces an item
- **THEN** the item lists the recipe with its ingredients, their quantities and the output count

#### Scenario: Weighted loot
- **WHEN** a loot pool gives an item with a weight
- **THEN** the source shows the raw weight and does not claim a percentage that the data does not state

### Requirement: Items without a known source
An item SHALL be published whether or not a source is known. An item without a known source SHALL say so.

#### Scenario: No known source
- **WHEN** no source in the records gives an item
- **THEN** the item is published, states that no source is known, and appears among the coverage gaps

### Requirement: Reviewed exclusions
Content that is not part of the game SHALL be excluded only through a reviewed entry with a reason and evidence. Placements in an excluded level SHALL NOT be sources.

#### Scenario: Test level
- **WHEN** an item is placed in an excluded test level
- **THEN** the item lists no source in that level

#### Scenario: Stale exclusion
- **WHEN** an exclusion names a level or class that the build lacks
- **THEN** the catalog fails and names the entry

### Requirement: Item icons
Each item SHALL have its icon decoded from the game's texture, or SHALL be marked as having no icon.

#### Scenario: Missing texture
- **WHEN** an item's icon texture cannot be found or decoded
- **THEN** the item is marked as having no icon, no other image stands in for it, and it appears among the coverage gaps

### Requirement: Coverage gaps
The catalog SHALL list every known gap: items without a known source, sources without a known place, items without an icon, facts marked unknown, and objects or packages that could not be read.

#### Scenario: Unread package
- **WHEN** extraction records a package as a gap
- **THEN** the coverage gaps name the package and the error
