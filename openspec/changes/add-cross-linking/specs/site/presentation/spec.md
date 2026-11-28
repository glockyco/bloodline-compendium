## MODIFIED Requirements

### Requirement: One preview presenter
Entity links SHALL show previews through one presenter that picks each kind's tooltip, and a kind SHALL get previews only by registering its tooltip and its preview data, so that pages that already link the kind need no change. Every kind with pages SHALL register a preview, and publication SHALL fail for a kind with pages but no preview.

#### Scenario: A kind without previews
- **WHEN** a kind is registered with pages but without a tooltip and preview data
- **THEN** publication fails and names the kind

#### Scenario: Resource linked from an item
- **WHEN** a reader hovers or focuses the Iron Deposit link on Iron Ore
- **THEN** a card framed as item previews are shows "Needs Mining level 10, can give 2 different items", the line the resource list card shows

#### Scenario: Profession and mechanic previews
- **WHEN** a reader hovers a profession or mechanic link
- **THEN** the card shows a profession's station, levels and "Makes" with its item count, or a mechanic's introduction, inside the frame

## ADDED Requirements

### Requirement: One style for entity names
An entity name that links SHALL show in the link colour and underlined, with or without an icon, at the weight of its place in the row. An entity name without a page SHALL show in the text colour of its place and never in the link colour. A layout check SHALL read the computed style of every entity name on the review pages and fail on any style outside these, naming the page and the name.

#### Scenario: A linked name with an icon
- **WHEN** a Used For row shows Lightborn Wand Core with its icon
- **THEN** the name is underlined in the link colour, as an inline link without an icon is

#### Scenario: A person without a page
- **WHEN** a dialogue row names The Mad Monk and people have no pages
- **THEN** the name shows in the row's text colour, and a name in the link colour fails the layout check
