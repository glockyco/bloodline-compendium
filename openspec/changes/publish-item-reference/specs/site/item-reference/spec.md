# Spec Delta

## Purpose

The item reference lets players find any item and see what it does and where it comes from for the current game build, including what is still unknown.

## ADDED Requirements

### Requirement: Item list
The site SHALL provide a list of all items that can be filtered by category and sorted by name and value.

#### Scenario: Filtering by category
- **WHEN** a reader selects the weapon category
- **THEN** the list shows only weapons

### Requirement: Item pages
The site SHALL provide one page per item, with its icon, name, description, category, value, stats and sources, at a stable URL. The page SHALL state each fact marked unknown and a missing source or icon.

#### Scenario: Opening an item page
- **WHEN** a reader opens an item's page
- **THEN** the page shows the item's facts and sources

#### Scenario: Item without a known source
- **WHEN** a reader opens the page of an item without a known source
- **THEN** the page states that no source is known and links to the coverage page

### Requirement: Search
The site SHALL let readers find items by name from every page, without a server.

#### Scenario: Searching by the start of a word
- **WHEN** a reader types the beginning of a word in an item's name
- **THEN** matching items are offered with links to their pages

### Requirement: Coverage page
The site SHALL provide a coverage page that explains each kind of gap and lists the items, sources and packages it affects, with links to item pages.

#### Scenario: Reading the coverage page
- **WHEN** a reader opens the coverage page
- **THEN** it lists the items without a known source with links to their pages

### Requirement: Build label
Every page SHALL name the Steam build ID that its data comes from.

#### Scenario: Reading the build
- **WHEN** a reader opens any page
- **THEN** the page names the Steam build ID of its data

### Requirement: Publication provenance
Each built site SHALL include a build information file naming the code commit and the Steam app ID, build ID and depot manifest IDs it was built from. The file SHALL NOT contain record data. A production build SHALL refuse a working tree with uncommitted changes.

#### Scenario: Tracing a deployment
- **WHEN** the deployed site's build information file is read
- **THEN** it names the code commit, Steam build ID and depot manifest IDs used for that deployment

#### Scenario: Uncommitted changes
- **WHEN** a production build runs on a working tree with uncommitted changes
- **THEN** the build fails and names the changed files

### Requirement: Static delivery within limits
The site SHALL be fully prerendered static files. The build SHALL fail when the file count or any file size exceeds the hosting limits.

#### Scenario: Too many files
- **WHEN** a build would produce more files than the configured limit
- **THEN** the build fails and reports the count by directory
