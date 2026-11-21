## ADDED Requirements

### Requirement: The item's render
An item page with a published render SHALL show it above the game card, at most 18rem tall, at the density the screen needs. Where an item has a render on each body, the page SHALL show the male body first and offer a choice of Male and Female that switches the render without leaving the page. An item without a render SHALL show its game card alone.

#### Scenario: A torso piece
- **WHEN** a reader opens a torso armor item with renders on both bodies and chooses Female
- **THEN** the render shows the female body's mesh and the Female choice is marked as chosen

#### Scenario: A sword
- **WHEN** a reader opens a sword with one render
- **THEN** the render shows above the game card without a choice of body

#### Scenario: A ring
- **WHEN** a reader opens an item that has no render
- **THEN** the side column shows only the game card
