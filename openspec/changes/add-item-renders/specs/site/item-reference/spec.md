## ADDED Requirements

### Requirement: Shared item links preview the render
A shared link to an item page SHALL preview the item's render, large and centred on the site's night colour, where the item has one. An item without a render SHALL preview its icon as a small square, and other pages the emblem banner.

#### Scenario: A sword shared in a chat
- **WHEN** a reader shares the address of a sword that has a render
- **THEN** the link preview shows the sword's render as a large image

#### Scenario: A food without a model
- **WHEN** a reader shares the address of a food the game draws no model for
- **THEN** the link preview shows the food's icon beside the text
