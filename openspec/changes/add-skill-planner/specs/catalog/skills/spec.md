# Spec Delta

## ADDED Requirements

### Requirement: Planner inputs
The catalog SHALL write the planner's inputs as one payload separate from page documents: disciplines with their rows, skill slots with required levels and the discipline each checks, passives with ranks and costs, skills with names and icons, and rule inputs, naming its game build and format version. An unknown value SHALL stay distinguishable from zero and from no limit.

#### Scenario: Unknown maximum rank
- **WHEN** a passive's maximum rank cannot be read
- **THEN** the payload marks it unknown and the planner allows no ranks beyond what is known
