# Spec Delta

## Purpose

Defines how players find and compare items in the item list and through search.

## ADDED Requirements

### Requirement: Filters by question
The item list SHALL offer separate filters for category, weapon type, armor slot, armor class, weapon part, effect, how to get, where to get and availability, in that order, each with values in game terms. A filter SHALL show only values that match items under the other active filters, keep selected values visible, show counts that follow the other filters, and offer a narrowing field only when it has more than ten values. Filters that apply to one category SHALL appear when that category is selected or when they narrow the current list.

#### Scenario: Weapons selected
- **WHEN** a reader selects the Weapons category
- **THEN** the weapon type filter shows only weapon types with matching items and no armor slot, food or part values appear

### Requirement: Columns for the category
The list SHALL show columns that suit the selected category: damage, speed and type for weapons, armor, class and slot for armor, healing and effects for food and potions, contributions and fitting weapons for parts, and the number of uses for materials. Selecting a stat filter SHALL add its column. Several stat filters SHALL match all or any of them as the reader chooses. All columns SHALL sort.

#### Scenario: Healing food
- **WHEN** a reader selects Food and sorts by healing
- **THEN** the food that heals most comes first and its healing shows in its row

### Requirement: Every item findable
Name search SHALL match every item, including items without a known source, which SHALL show that status in their row. Browsing MAY leave items without a known source out by default only with a visible count and one action that shows them. Category counts on the home page and the list SHALL agree.

#### Scenario: Coconut
- **WHEN** a reader searches the list for Coconut
- **THEN** Coconut appears with a note that no way to get it is known

### Requirement: Search opens lists
Site search SHALL offer filtered lists for category and type words, such as swords, boots or food, alongside individual items.

#### Scenario: Searching swords
- **WHEN** a reader searches for swords
- **THEN** the results include the item list filtered to one-handed and two-handed swords
