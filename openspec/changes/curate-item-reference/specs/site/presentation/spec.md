# Spec Delta

## Purpose

Defines how pages keep one spacing and one section structure, and how composed pages are checked on real data, so presentation stays consistent as sections are added.

## ADDED Requirements

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
