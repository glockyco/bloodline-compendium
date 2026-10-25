# Spec Delta

## Purpose

The item catalog decides which items exist for players and what the site says about them, based only on source records of the accepted build.

## ADDED Requirements

### Requirement: Item identity
Each catalog item SHALL be identified by its Blueprint class path, which stays stable across builds while the class exists.

#### Scenario: Same item in two builds
- **WHEN** an item class exists in two builds
- **THEN** both catalogs give it the same identifier

### Requirement: Item facts
Each catalog item SHALL carry its display name, description, category, value and type-specific stats as resolved in the source records, with the provenance of each fact.

#### Scenario: Weapon facts
- **WHEN** an item is a weapon
- **THEN** its catalog entry includes its damage and attack speed with their sources

### Requirement: Reachable items only
The catalog SHALL publish only items that the game can give the player, as shown by references from shops, loot tables, recipes, quest rewards or placed objects in playable levels. Items that nothing reaches SHALL be listed for review and not published.

#### Scenario: Item sold in a shop
- **WHEN** a shop's stock references an item
- **THEN** the item is published and lists the shop as a source

#### Scenario: Demo content from an asset pack
- **WHEN** an item class is referenced only from demonstration levels of a marketplace asset pack
- **THEN** the item is not published and appears in the review list with the reason

### Requirement: Reviewed overrides
Corrections to catalog decisions SHALL be recorded as reviewed overrides with a reason, and never by editing generated output.

#### Scenario: Including an unreachable item
- **WHEN** a reviewed override publishes an item without a detected source
- **THEN** the item is published and the override's reason is shown in the review list

### Requirement: Item icons
Each published item SHALL have its icon decoded from the game's texture, or SHALL be listed as missing an icon.

#### Scenario: Missing texture
- **WHEN** an item's icon texture cannot be found or decoded
- **THEN** the build lists the item as missing an icon and does not substitute an empty image
