# Spec Delta

## MODIFIED Requirements

### Requirement: Search
The site SHALL let readers find every entity with a page by name from every page, without a server, with each result marked by its kind.

#### Scenario: Searching by the start of a word
- **WHEN** a reader types the beginning of a word in an item's or a skill's name
- **THEN** matching entities are offered with their kind and links to their pages

### Requirement: Build label
Every page SHALL identify the reviewed game release and link to release information. The header SHALL offer a Browse menu with every kind's list, read from the kind registry, and SHALL keep search and support visible. The footer SHALL provide every kind's list, Known Issues, About, wrong-fact reporting and Ko-fi links. The About page SHALL explain how the site is made, the release date, the distinct data-update date, what it does not list yet, patch notes and unofficial status.

#### Scenario: Reading release information
- **WHEN** a reader opens About
- **THEN** the page distinguishes the game release from the data update and links to the reviewed patch notes

#### Scenario: New kind in the menu
- **WHEN** a kind with pages is registered
- **THEN** the Browse menu and the footer list it without other changes to the layout

### Requirement: Reference hub
The home page SHALL say what a visitor can look up, provide search and an entry point for every kind with pages, and show release information without repeating any complete list.

#### Scenario: Starting a lookup
- **WHEN** a reader opens the home page
- **THEN** search and links to the item, discipline and skill lists offer direct routes
