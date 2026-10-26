# Spec Delta

## Purpose

Defines the shared presentation every page is built from, so spacing, type, colour, alignment and wording stay consistent as the site grows.

## ADDED Requirements

### Requirement: Closed scales
Site styles SHALL take colours, spacing, type sizes, weights, line heights, radii and layers from one tokens file with small scales. A style outside the tokens file that uses a raw value SHALL fail the lint check.

#### Scenario: Raw spacing value
- **WHEN** a component sets padding to a raw length
- **THEN** stylelint fails and names the file and declaration

### Requirement: Shared primitives
Repeated elements such as cards, icon slots, section headings, counts, buttons, tables, entity links and issue notes SHALL come from shared components. Cards in a row SHALL align their inner rows regardless of text length, and content SHALL stay inside its padding.

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

### Requirement: Voice rules
Site-authored text SHALL use the game's own terms for labels and state facts plainly. A check SHALL fail on the banned constructions listed in the voice rules.

#### Scenario: Banned phrase
- **WHEN** a page contains a banned construction
- **THEN** the voice check fails and names the page and phrase
