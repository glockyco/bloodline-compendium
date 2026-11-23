# Spec Delta

## MODIFIED Requirements

### Requirement: One registry of kinds
Each kind SHALL be registered once with whether it has pages, its route, its search filter label and its structured-data type, or with the kind whose pages show its entities as anchored entries. Routes, search filters, the sitemap, link targets, tooltips and page metadata SHALL read the registry, so publishing a kind needs no edits elsewhere to make it searchable, listed and linked.

#### Scenario: Promoting a kind to pages
- **WHEN** a kind's registry entry changes to have pages
- **THEN** its pages appear in search with their kind filter and in the sitemap

#### Scenario: Kind shown on another kind's page
- **WHEN** a kind is registered as shown on its discipline's page
- **THEN** every reference to one of its entities links to that page's anchor for it and shows its tooltip

### Requirement: Graph integrity
Before publication, the catalog and the built site SHALL reject a reference to a key that does not exist, a link to a kind without pages, a link to an anchor its host page lacks, a duplicate key or slug, a relation published in one direction only, and a page whose relations are missing from its static HTML.

#### Scenario: Dangling reference
- **WHEN** a source refers to a key that no entity has
- **THEN** the catalog fails and names the referring entity and the missing key

#### Scenario: Missing anchor
- **WHEN** a link leads to a passive whose discipline page has no anchor for it
- **THEN** publication fails and names the link and the missing anchor
