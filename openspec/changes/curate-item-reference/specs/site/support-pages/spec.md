# Spec Delta

## Purpose

Defines the pages around the item reference: About, Known Issues, the home page, missing pages, and the maintainer report that replaces Coverage.

## ADDED Requirements

### Requirement: Maintainer report instead of Coverage
The site SHALL NOT publish a Coverage page. The catalog SHALL write the gaps it finds, the unread objects, the unresolved conditions, the unexplained tooltip differences and the evidence for item statuses to a maintainer report beside the site data. About SHALL explain once, in player terms, what the reference reads from the game and what it cannot know.

#### Scenario: Old Coverage link
- **WHEN** a reader follows a link to /coverage/
- **THEN** they reach the missing-page view, and no page of the site links there

### Requirement: Known Issues for players first
The Known Issues page SHALL open with a list of its issues, and each issue SHALL show its title, its effect on players, its workaround and the affected items before its steps. The cause, functions and report status SHALL sit in a collapsed section for developers.

#### Scenario: Phone reader
- **WHEN** a reader opens Known Issues on a phone
- **THEN** every issue title is reachable from the list at the top without scrolling through another issue's evidence

### Requirement: Home and missing pages
The home page SHALL lead to the item list, the tools and the most asked-for item groups, with counts that match the list. A missing URL SHALL show the site's missing-page view with search and a link to the item list.

#### Scenario: Unknown URL
- **WHEN** a reader opens a URL that does not exist
- **THEN** the page says the page does not exist and offers search and the item list
