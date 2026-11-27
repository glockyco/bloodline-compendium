# site/reader-text Specification

## Purpose
Defines how the site's own words present game facts, unknowns and explanations to players, so that every sentence helps a player decide something about the game.

## Requirements

### Requirement: Text about the game, not the reference
Item pages, lists, tooltips and tools SHALL describe the game: what an item does, how to get it and what it is for. Statements about the reference itself, its data or its methods, such as disclaimers that a source may be incomplete, SHALL appear only on About.

#### Scenario: Acquisition answer
- **WHEN** a reader opens an item with known sources
- **THEN** no sentence below the sources says that sources may be incomplete or that locations may be unreachable

### Requirement: No pipeline words or identifiers
Site text, labels, table headers, empty states and accessibility text SHALL NOT use pipeline words such as entity, relation, source as a noun for a game object, coverage, mechanism, record, weight, pick or relative chance, and SHALL NOT show identifiers such as package paths, class names, dialogue states, enum words or error messages. Game names and game text keep their own wording. The voice check SHALL fail on the listed words and on identifier patterns in site text. A console command SHALL show the name a player types, as the only exception.

#### Scenario: Dialogue state in a condition
- **WHEN** a condition is built from a dialogue state whose action is unknown
- **THEN** the page does not show the state's name

### Requirement: Uncertainty attached to its fact
An unknown or doubtful value SHALL be stated where the value appears, with what it means for the player, such as an unknown unlock requirement or a price no purchase has confirmed. The same caveat SHALL NOT be repeated under every row of a section. A missing value SHALL read Unknown with its reason on hover, focus and tap.

#### Scenario: Unknown drop chance
- **WHEN** an enemy's drop has no established chance
- **THEN** the chance cell reads Unknown and its reason is available on hover, focus and tap

#### Scenario: Recipe its station does not list
- **WHEN** the game files hold a recipe that its station's list leaves out
- **THEN** Only in the Game Files shows its ingredients and a reason that names the station, such as "The recipe exists, but the Engineer's Workbench does not offer it."

#### Scenario: Reason in a player's words
- **WHEN** a way that only the game files hold gives its reason
- **THEN** the reason says what a player would tell another, with its detail, such as "This kind of spot can give it, but none of those placed in the world do.", and not how the site knows, as "is not known to" or "the game files" would

#### Scenario: Ways without a place
- **WHEN** several creatures that drop an item have no place
- **THEN** their rows stand at the end of the table under one heading that reads Location unknown and "No fixed spawn in any area.", and a single such row reads "Location unknown. No fixed spawn in any area." beside it

### Requirement: Casing, numbers and plurals
Titles, headings, column headers and buttons SHALL use Title Case and sentences SHALL use sentence case. Counts SHALL agree with their nouns. In a sentence, a chance below 1 percent SHALL read as approximately 1 in N with its event. In a column of chances, every chance SHALL read as a percent, and every chance short of certain SHALL give its reading as about 1 in N on hover, focus and tap. Every number SHALL pass through the shared formatter.

#### Scenario: One ingredient
- **WHEN** a recipe uses one Iron Ingot
- **THEN** the page reads 1 Iron Ingot, not 1 items

#### Scenario: Rare container loot in a sentence
- **WHEN** a sentence states an item's chance per container of 0.4 percent
- **THEN** it reads about 1 in 250 per container

#### Scenario: Chance column
- **WHEN** a column lists chances of 0.59 percent and 82.5 percent
- **THEN** the cells read 0.59% and 82.5%, and their hints read about 1 in 170 and about 1 in 1.2
