# Spec Delta

## Purpose

The item reference lets players find any item and see what it does and where it comes from for the current game build, including what is still unknown.

## ADDED Requirements

### Requirement: Item list
The site SHALL provide compact item rows filtered by category and Type, with sorting by name and value, matching facet counts and shareable URL state. Items without a known source SHALL be hidden by default with an explicit counted reveal. Filtering SHALL remain responsive on the complete item catalog.

#### Scenario: Revealing items without sources
- **WHEN** a reader selects the counted reveal for items without a known source
- **THEN** those items become available and the URL preserves the selected filters and sort

### Requirement: Item pages
The site SHALL provide one page per item at its readable qualified URL. Acquisition SHALL be the primary answer, with one game-like facts tooltip beside it on desktop and after it on phones. Reverse uses SHALL follow the answer. Missing facts, sources and icons SHALL remain explicit without blank decorative panels. Relation tables SHALL show at most two numeric columns with interpretable units. Tables of at most ten rows SHALL show all rows. Longer tables SHALL show eight rows and a native disclosure containing every remaining row in prerendered HTML.

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
Every page SHALL identify the reviewed game release and link to release information. The footer SHALL provide Coverage, About and Ko-fi links. The header SHALL keep search and support visible. The About page SHALL explain the method, release date, distinct data-update date, coverage limits, patch notes and unofficial status.

#### Scenario: Reading release information
- **WHEN** a reader opens About
- **THEN** the page distinguishes the game release from the data update and links to the reviewed patch notes

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

### Requirement: Reference hub
The home page SHALL introduce the item reference, provide search and category entry points, and expose release and coverage information without repeating the complete item list.

#### Scenario: Starting a lookup
- **WHEN** a reader opens the home page
- **THEN** search and category links offer direct routes to the items

### Requirement: Discoverable factual pages
Each indexable page SHALL have a unique factual title and description, canonical URL and appropriate social metadata. Structured data SHALL include WebSite without SearchAction and BreadcrumbList matching visible breadcrumbs. About SHALL be visible and included in the sitemap. Wiki MAY be used as a descriptive search term but SHALL NOT replace the site name or imply official status.

#### Scenario: Duplicate item names
- **WHEN** distinct items share a game display name
- **THEN** their metadata uses their readable qualifiers to distinguish the pages

### Requirement: Readable accessible presentation
Site-authored titles, navigation, noun headings and table columns SHALL use Title Case. Labels and hints SHALL use sentence case without changing game-provided names. Body text SHALL use rem units with a minimum of 1rem, no decorative letterspacing, and normal text contrast of at least 4.5:1. Desktop and phone layouts SHALL preserve readable names, units and controls without sideways page scrolling.

#### Scenario: Narrow item page
- **WHEN** a reader opens an item at phone width
- **THEN** acquisition precedes the facts tooltip and relation values remain legible

### Requirement: Independent presentation tokens
The site SHALL default to a neutral dark theme with no decoration, textures or webfonts. Every site-authored color, font, size, spacing, radius and shadow SHALL be defined by a semantic CSS custom property in one tokens file.

#### Scenario: Changing visual identity
- **WHEN** presentation tokens change
- **THEN** item facts, acquisition answers, relation tables, list controls and URL behavior remain independent of that visual change

### Requirement: Publication graph acceptance
Before deployment, the built-site gate SHALL verify internal links, item references, icon resources, sitemap membership and unique page metadata. Browser acceptance SHALL include automated accessibility checks and actual desktop and phone review. Fixture acceptance SHALL NOT be reported as real-data runtime acceptance.

#### Scenario: Broken item target
- **WHEN** a built item relation points to an absent page or icon
- **THEN** the predeployment gate fails and names the referring page and target
