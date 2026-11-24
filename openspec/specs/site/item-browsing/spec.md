# site/item-browsing Specification

## Purpose
Defines how players find and compare items in the item list and through search.

## Requirements

### Requirement: Filters by question
The item list SHALL offer separate filters for category, weapon type, armor slot, armor class, weapon part, effect, how to get, where to get and availability, in that order, each with values in game terms.

#### Scenario: Filter order
- WHEN a reader opens the item list
- THEN category precedes weapon type, armor slot, armor class, weapon part, effect, how to get, where to get and availability

### Requirement: Filter values follow other selections
A filter SHALL show only values that match items under the other active filters, keep selected values visible, and show counts that follow the other filters.

#### Scenario: Weapons selected
- **WHEN** a reader selects the Weapons category
- **THEN** the weapon type filter shows only weapon types with matching items and no armor slot, food or part values appear

### Requirement: Large filters offer narrowing
A filter SHALL offer a narrowing field only when it has more than ten values.

#### Scenario: Eleven filter values
- WHEN a filter has eleven values
- THEN it offers a narrowing field

### Requirement: Category filters appear when relevant
Filters that apply to one category SHALL appear when that category is selected or when they narrow the current list.

#### Scenario: Weapon filter applicability
- WHEN Weapons is selected
- THEN the applicable weapon filters appear

### Requirement: Columns for the category
The list SHALL show columns that suit the selected category: damage, speed and type for weapons, armor, class and slot for armor, healing and effects for food and potions, contributions and fitting weapons for parts, and the number of uses for materials.
A column that only some weapons have, such as Headshot, Projectiles per Shot or the shield columns, SHALL show only when every listed weapon has it, so all weapons show Damage and Speed and a Bow filter adds Headshot and Projectiles per Shot. A ranged weapon's Damage cell SHALL show its body hit, the number its game card calls Damage. Each name SHALL show its icon in a 36 px slot, because the game's thin weapon icons need an edge to read.

#### Scenario: All weapons
- **WHEN** a reader selects Weapons
- **THEN** the list shows one Damage column and no Headshot column

#### Scenario: Healing food
- **WHEN** a reader selects Food and sorts by healing
- **THEN** the food that heals most comes first and its healing shows in its row

### Requirement: Stat selections control columns
Selecting a stat filter SHALL add its column, and the stat picker SHALL offer the stats of the selected category.

#### Scenario: Selecting a healing stat
- WHEN a reader selects a healing stat filter for Food
- THEN the list adds its healing column and the picker offers Food stats

### Requirement: Stat filters offer all or any matching
Several stat filters SHALL match all or any of them as the reader chooses.

#### Scenario: Any selected stat
- WHEN a reader chooses to match any of several stat filters
- THEN an item matching one of them remains in the list

### Requirement: Columns sort by meaning
All columns SHALL sort by meaning, so speed words sort from slowest to fastest.

#### Scenario: Speed sort
- **WHEN** a reader sorts weapons by speed
- **THEN** Slow comes before Below Average, Average and Above Average, and Fast comes last

### Requirement: Columns identify units and missing values
A column SHALL name its unit, and a cell without a value SHALL say whether the value is unknown or does not apply.

#### Scenario: Unknown stat
- WHEN a stat value is unknown
- THEN its cell says it is unknown and its column names the unit

### Requirement: Narrow lists keep item names visible
On narrow screens the item name SHALL stay visible while the other columns scroll.

#### Scenario: Scrolling a narrow list
- WHEN a reader scrolls the columns on a narrow screen
- THEN the item name stays visible

### Requirement: Filters that agree
A place filter combined with a way-to-get filter SHALL match an item only through a source of a selected way in a selected place. Filter counts SHALL stay on one line, and a skip link SHALL lead past the filters to the results.

#### Scenario: Buying ingots in one place
- **WHEN** a reader selects Materials, Merchants and Elderglen Woods
- **THEN** only materials sold by a merchant in Elderglen Woods remain, and Copper Ingot, sold only in Squalls End, does not

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
