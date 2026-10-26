# Spec Delta

## Purpose

Lets readers find disciplines, skills, passives and spellsongs and understand their progression requirements from current-build facts without opening the planner.

## ADDED Requirements

### Requirement: Progression reference pages
Each published discipline, skill, passive and spellsong SHALL have a static page with identity and game summary, acquisition or unlock requirements, uses or effects, established locations, governing rules and gaps in that order. Inapplicable sections SHALL be omitted. Unknown acquisition or effects SHALL be stated rather than hidden. Skill and passive pages SHALL link to their discipline and applicable planner context.

#### Scenario: Passive lookup
- **WHEN** a passive page is opened without JavaScript
- **THEN** its known required discipline level, point cost, maximum rank, related discipline and gaps are readable

### Requirement: Discipline tree presentation
A discipline page SHALL show its reachable level-based skill slots and passives with required levels and costs. Multiple slots for the same skill SHALL preserve their distinct requirements. Tree presentation SHALL have a readable keyboard-accessible list or table and SHALL NOT require spatial navigation to read a requirement.

#### Scenario: Phone tree view
- **WHEN** a discipline page is read on a narrow screen
- **THEN** skill names, levels, costs and passive rank limits remain readable without horizontal page scrolling

### Requirement: Browsing and discovery
The site SHALL provide counted browsing for all four progression kinds with name ordering and shareable filters for kind and established discipline or category. Initial results SHALL be prerendered. The kind registry SHALL supply search filters, routes, sitemap entries and factual metadata. Existing item routes and search results SHALL remain available.

#### Scenario: Skill search
- **WHEN** the start of a word in a skill name is searched
- **THEN** the result identifies its kind and leads to its page without loading the planner

### Requirement: Shared reference presentation
Progression pages SHALL use shared tokens, primitives, entity links, issue notes, stat vocabulary and number formatting. Tooltips SHALL work on hover and keyboard focus without loading target pages. Site-authored text SHALL pass the existing voice gate. Game text SHALL retain its wording and be marked for exclusion from that gate.

#### Scenario: Cooldown in several views
- **WHEN** a skill's cooldown appears in its tooltip, page and planner
- **THEN** all views use the same vocabulary entry, units and formatting

### Requirement: Static relation closure
All progression relation links, including rows inside disclosures, SHALL exist in static HTML and pass the existing graph and publication gates. Pages SHALL expose build context, coverage gaps and affected known issues using the existing reference contracts.

#### Scenario: Missing skill target
- **WHEN** a discipline page links to a skill page absent from the built site
- **THEN** publication fails and identifies the discipline page and missing target
