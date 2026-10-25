# Spec Delta

## Purpose

The item reference lets players find any obtainable item and see what it does and where it comes from, for the current game build.

## ADDED Requirements

### Requirement: Item list
The site SHALL provide a list of all published items that can be filtered by category and sorted by name and value.

#### Scenario: Filtering by category
- **WHEN** a reader selects the weapon category
- **THEN** the list shows only weapons

### Requirement: Item pages
The site SHALL provide one page per published item, with its icon, name, description, category, value, stats and sources, at a stable URL derived from the item's identity.

#### Scenario: Opening an item page
- **WHEN** a reader opens an item's page
- **THEN** the page shows the item's facts and links to its sources

### Requirement: Search
The site SHALL let readers find items by name from every page, without a server.

#### Scenario: Searching by partial name
- **WHEN** a reader types part of an item's name
- **THEN** matching items are offered with links to their pages

### Requirement: Build label
Every page SHALL state the game build its data comes from.

#### Scenario: Reading the build
- **WHEN** a reader opens any page
- **THEN** the page names the game version and Steam build ID of its data

### Requirement: Static delivery within limits
The site SHALL be fully prerendered static files. The build SHALL fail when the file count or any file size exceeds the hosting limits.

#### Scenario: Too many files
- **WHEN** a build would produce more files than the configured limit
- **THEN** the build fails and reports the count by directory
