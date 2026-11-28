## ADDED Requirements

### Requirement: Pages for the kinds readers meet
Creatures, places, people and merchants, quests, effects, containers, settlement buildings and recipes SHALL have pages, each listing what the catalog relates to it, so that every name an item or skill page shows leads somewhere. Effects SHALL cover debuffs on creatures, weapon enchantments and armor attunements. Each kind SHALL be compared with screenshots of its built pages on real data before it is published, and SHALL be published with its preview.

#### Scenario: A creature page
- **WHEN** creature pages are published
- **THEN** one page per creature name lists what it drops with each drop's own areas, where it appears and, once effect pages exist, the debuffs it can carry, and every item page that names the creature links to it

#### Scenario: A creature placed nowhere
- **WHEN** no level, spawner or event places a creature
- **THEN** its page says so once under its name, and its drop rows do not repeat it

#### Scenario: A creature preview
- **WHEN** a reader hovers a creature link
- **THEN** the preview describes the creature, by its checked stats once the lab has confirmed them, and never by its drops

#### Scenario: Divine Bond
- **WHEN** effect pages are published
- **THEN** the Divine Favor row "Applies Divine Bond to creatures it hits" links Divine Bond to its page, which lists the ways to apply it and what creatures give when they die with it
