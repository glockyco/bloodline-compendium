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
Each item SHALL be identified internally by its Blueprint class path. Its public URL SHALL use a readable display-name slug. Duplicate names SHALL use readable type, category or distinguishing stats as qualifiers, with an ordinal only as a last resort. Internal class names and hashes SHALL NOT appear in public slugs. The cutover SHALL NOT retain legacy aliases.

#### Scenario: Duplicate display names
- **WHEN** two items have the same display name but different types
- **THEN** their public names and slugs distinguish them with readable type qualifiers

### Requirement: Item facts
Each item SHALL carry its display name, description, category, value and type-specific stats as the records state them. A fact that the records do not determine SHALL be marked unknown and not guessed. Weapon components SHALL expose their part type, damage and speed, with the part as their displayed Type. Iuba and Tithes SHALL be classified as Currency.

#### Scenario: Weapon facts
- **WHEN** an item is a weapon
- **THEN** its entry includes its damage and attack speed

#### Scenario: Unknown fact
- **WHEN** a fact depends on a field that the records leave out
- **THEN** the entry marks the fact unknown

### Requirement: Item sources
Each item SHALL list the sources that the records show for it, with the place, quantity, chance or price as the data states them. Consumed inputs SHALL be kept apart from the item given.

#### Scenario: Item sold by a vendor
- **WHEN** a vendor placed in a level stocks an item
- **THEN** the item lists the vendor, the level and the price as a source

#### Scenario: Crafted item
- **WHEN** a recipe produces an item
- **THEN** the item lists the recipe with its ingredients, their quantities and the output count

#### Scenario: Verified weighted selection
- **WHEN** consumer evidence establishes a pool's selection rule and all necessary inputs
- **THEN** the source shows the computed chance with its per-kill, per-catch, per-harvest or per-roll basis
- **AND** null outcomes remain in the probability denominator

#### Scenario: Incomplete selection evidence
- **WHEN** a weight is known but the complete selection rule is not
- **THEN** the source labels it Relative chance and does not invent an event probability

#### Scenario: Container picks
- **WHEN** a container chooses several entries from a pool
- **THEN** the source distinguishes chance per pick from the number of picks instead of claiming a per-open chance

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

### Requirement: Reverse item uses
The catalog SHALL derive reverse uses from every recipe, cooking, smithing, farming and currency-shop input. Each use SHALL identify its output or purchase, input quantity and applicable conditions without presenting consumed items as acquisition outputs.

#### Scenario: Currency purchase
- **WHEN** a shop consumes an inventory currency item to buy another item
- **THEN** the currency item's uses identify the purchased item and its price

### Requirement: Reviewed release identity
The catalog SHALL resolve each Steam build through a reviewed release mapping, carrying the release name, release date, patch-note URL and separate data-update date. Cataloging an unmapped build SHALL fail rather than guess release information.

#### Scenario: Unknown release
- **WHEN** the extracted Steam build has no reviewed release mapping
- **THEN** cataloging fails and identifies the missing build
