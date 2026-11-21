# site/presentation Specification

## Purpose
Defines the shared presentation every page is built from, so spacing, type, colour, alignment and wording stay consistent as the site grows.

## Requirements

### Requirement: Closed scales
Site styles SHALL take colours, spacing, type sizes, weights, line heights, radii and layers from one tokens file with small scales. A style outside the tokens file that uses a raw value SHALL fail the lint check.

#### Scenario: Raw spacing value
- **WHEN** a component sets padding to a raw length
- **THEN** stylelint fails and names the file and declaration

### Requirement: Shared primitives
Repeated elements such as cards, icon slots, section headings, counts, buttons, tables and entity links SHALL come from shared components. Cards in a row SHALL align their inner rows regardless of text length, and content SHALL stay inside its padding.

#### Scenario: Descriptions of different length
- **WHEN** one card's description wraps to two lines and its neighbour's fits on one
- **THEN** the icon rows of both cards start at the same height

### Requirement: Presentation checks
A fixture page SHALL show every primitive with extreme content. Checks SHALL fail on horizontal overflow, misaligned rows across cards, text below 1rem, contrast below 4.5:1 and accessibility violations at phone and desktop widths.

#### Scenario: Overflowing slot row
- **WHEN** an icon row extends past its card's padding
- **THEN** the layout check fails and names the element

### Requirement: Game-familiar item facts
Item facts SHALL appear in the order and wording of the game's own tooltip, with the game's typeface for item names, while showing the catalog's verified values rather than any interface value the catalog has found wrong.

#### Scenario: Weapon tooltip
- **WHEN** a reader opens a weapon page
- **THEN** its tooltip shows the name, icon, speed word, damage and description in the game's order

### Requirement: Entity tooltips
Every link to an entity of a kind with pages SHALL show that entity's tooltip on hover and on keyboard focus, in its kind's tooltip style, without loading the target page. Without JavaScript the link SHALL still lead to the page.

#### Scenario: Item linked from a recipe
- **WHEN** a reader hovers or focuses an ingredient link on a recipe
- **THEN** the ingredient's item tooltip appears beside the link and the link still opens its page

### Requirement: One stat vocabulary
Every stat label, unit and number format SHALL come from one stat vocabulary that uses the game's own terms, and every number SHALL pass through one formatting function. Tooltips, tables, filters, formulas, calculators and the planner SHALL use the same entries.

#### Scenario: Armor in two places
- **WHEN** an item tooltip and a list column both show armor
- **THEN** both use the same label, unit and number format

### Requirement: Voice rules
Site-authored text SHALL use the game's own terms for labels and state facts plainly: no filler that claims ease, no marketing phrases, no cute labels, no defensive asides about what the site lacks, no exclamation marks, no semicolons between clauses, no em or en dashes, ellipsis characters or arrows, and straight quotes only.

#### Scenario: Site-authored label
- WHEN the site labels a game fact
- THEN it uses the game's term without filler

### Requirement: Game quotations retain their wording
Text from the game keeps its own wording and SHALL be marked as game text so the rules skip it.

#### Scenario: Quoted game punctuation
- WHEN marked game text contains punctuation the voice rules forbid in site-authored text
- THEN the wording is retained and the voice rules skip it

### Requirement: Built pages enforce the voice rules
A check over every built page SHALL fail on the constructions listed in the repository's voice rules file and name the page and the passage.

#### Scenario: Banned phrase
- **WHEN** a page contains a banned construction
- **THEN** the voice check fails and names the page and phrase

### Requirement: Spacing by role
Padding, margins, gaps and insets outside the tokens file SHALL use named spacing roles, not steps of the spacing scale. Every surface and every table cell SHALL use the same inset, so text inside stacked sections starts at one edge.

#### Scenario: Scale step in a component
- **WHEN** a component sets padding to a step of the spacing scale instead of a role
- **THEN** stylelint fails and names the file and declaration

#### Scenario: Summary above a table
- **WHEN** an item page shows the acquisition summary above a source table
- **THEN** the text of both starts at the same distance from the edge of its surface

### Requirement: One section anatomy
Every content section SHALL place its heading, count and controls above one surface. Dividers between entries SHALL span the surface's full width. A group row in a table SHALL read as the heading of the rows below it and SHALL NOT repeat in those rows.

#### Scenario: Containers grouped by place
- **WHEN** an item's containers are grouped by place
- **THEN** each place appears once as a subheading and its rows do not list it again

### Requirement: Content-sized columns
Table columns SHALL size to their content and wrap at spaces. A numeric column SHALL right-align its heading, values and secondary lines, and SHALL name a unit shared by every row in its heading instead of each row.

#### Scenario: Long chance
- **WHEN** a container chance reads "about 1 in 1,400"
- **THEN** it stays inside the table, right-aligned under a heading that names its event

### Requirement: Layout check on real data
A layout check SHALL load the fixture pages and a set of extreme real-data pages at 1440, 1024, 768 and 390 px. It SHALL fail on horizontal overflow, a surface or cell off its inset, numeric cells that are not right-aligned and list separators without a space, naming the page, width and element. CI SHALL run it on fixtures, and a UI change SHALL pass it on the real-data preview before it is committed.

#### Scenario: Separator without a space
- **WHEN** a place list renders "Ancient Mines,Elderglen"
- **THEN** the layout check fails and names the page and the list

### Requirement: One preview presenter
Entity links SHALL show previews through one presenter that picks each kind's tooltip, and a kind SHALL get previews only by registering its tooltip and its preview data, so that pages that already link the kind need no change.

#### Scenario: A kind without previews
- **WHEN** a page links an entity whose kind has pages but no registered tooltip
- **THEN** the link leads to the page and shows no preview

### Requirement: Search for every kind with pages
Every page of a kind with pages SHALL be indexed for search with its kind's filter from the kind registry, and publication SHALL fail on an entity page without it.

#### Scenario: Entity page without a search body
- **WHEN** a built entity page lacks the search body or carries another kind's filter
- **THEN** the publication gate fails and names the page and the filter it needs
