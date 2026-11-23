# Spec Delta

## ADDED Requirements

### Requirement: Widget trees
A widget Blueprint's class record SHALL list the widgets of its widget tree whose class is a Blueprint class, each with the values the tree sets on it, its parent panel and its position in that panel. Text widgets SHALL be recorded with their text. Other native widgets SHALL be left out.

#### Scenario: Skill tree slot
- **WHEN** a skill tree places a skill slot in a row panel
- **THEN** the tree's record lists the slot with its skill, its required level, the row panel and its position in the row
