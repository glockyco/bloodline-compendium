# Spec Delta

## Purpose

Defines how the site's own words present game facts, unknowns and explanations to players, so that every sentence helps a player decide something about the game.

## ADDED Requirements

### Requirement: Text about the game, not the reference
Item pages, lists, tooltips and tools SHALL describe the game: what an item does, how to get it and what it is for. Statements about the reference itself, its data or its methods, such as disclaimers that a source may be incomplete, SHALL appear only on About.

#### Scenario: Acquisition answer
- **WHEN** a reader opens an item with known sources
- **THEN** no sentence below the sources says that sources may be incomplete or that locations may be unreachable

### Requirement: No pipeline words or identifiers
Site text, labels, table headers, empty states and accessibility text SHALL NOT use pipeline words such as entity, relation, source as a noun for a game object, coverage, mechanism, record, weight, pick or relative chance, and SHALL NOT show identifiers such as package paths, class names, dialogue states, enum words or error messages. Game names and game text keep their own wording. The voice check SHALL fail on the listed words and on identifier patterns in site text.

#### Scenario: Dialogue state in a condition
- **WHEN** a condition is built from a dialogue state whose action is unknown
- **THEN** the page does not show the state's name

### Requirement: Uncertainty attached to its fact
An unknown or doubtful value SHALL be stated where the value appears, with what it means for the player, such as an unknown unlock requirement or a price no purchase has confirmed. The same caveat SHALL NOT be repeated under every row of a section. A missing value SHALL read Unknown with its reason on hover, focus and tap.

#### Scenario: Unknown drop chance
- **WHEN** an enemy's drop has no established chance
- **THEN** the chance cell reads Unknown and its reason is available on hover, focus and tap

### Requirement: Casing, numbers and plurals
Titles, headings, column headers and buttons SHALL use Title Case and sentences SHALL use sentence case. Counts SHALL agree with their nouns. Chances below 1 percent SHALL read as approximately 1 in N with their event. Every number SHALL pass through the shared formatter.

#### Scenario: One ingredient
- **WHEN** a recipe uses one Iron Ingot
- **THEN** the page reads 1 Iron Ingot, not 1 items

#### Scenario: Rare container loot
- **WHEN** an item's chance per container is 0.4 percent
- **THEN** the page reads about 1 in 250 containers
