# Spec Delta

## Purpose

Defines how the catalog publishes game content as one graph of entities that refer to each other, so every page can link to every related thing the game connects it to.

## ADDED Requirements

### Requirement: Stable entity identity
Every published entity SHALL have a kind, a stable key derived from the game object it represents, and a display name. Entities of kinds that have pages SHALL also have a slug derived from the display name, unique within the kind. A key SHALL NOT change when the display name, slug or page layout changes, and SHALL change only when the game object it derives from changes.

#### Scenario: Renamed item
- **WHEN** a game update changes an item's display name but not its class
- **THEN** the item keeps its key and its slug follows the new name

#### Scenario: Shared display name
- **WHEN** two entities of one kind share a display name
- **THEN** both keep distinct keys and receive slugs qualified by readable facts

### Requirement: Typed references
Wherever published data names another entity, it SHALL carry a reference with that entity's kind and key instead of display text. A reference to a kind without published pages SHALL still carry its kind and key and SHALL render as unlinked text. A reference the catalog cannot resolve SHALL carry a label and no key, and SHALL appear in coverage.

#### Scenario: Merchant source before NPC pages exist
- **WHEN** an item is sold by a merchant and NPC pages are not yet published
- **THEN** the item page names the merchant without a link and the site data holds the merchant's kind and key

#### Scenario: Publishing a new kind
- **WHEN** NPC pages are published
- **THEN** every existing reference to an NPC becomes a link without changes to the pages that hold them

### Requirement: Reverse relations
The catalog SHALL compute each relation once and publish both directions, so a page lists the entities that refer to it, such as the items a merchant sells or the recipes that use an item. Each relation SHALL have a named kind with a reader label for each direction.

#### Scenario: Item sold by a merchant
- **WHEN** a merchant's stock includes an item
- **THEN** the item lists the merchant as a source and the merchant lists the item as stock, from the same relation

### Requirement: Graph integrity
Before publication, the catalog and the built site SHALL reject a reference to a key that does not exist, a link to a kind without pages, a duplicate key or slug, a relation published in one direction only, and a page whose relations are missing from its static HTML.

#### Scenario: Dangling reference
- **WHEN** a source refers to a key that no entity has
- **THEN** the catalog fails and names the referring entity and the missing key
