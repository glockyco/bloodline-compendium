# site/known-issues Specification

## Purpose
Defines how reproduced defects in the game itself are recorded, shown to readers where they meet them, and prepared for the game's developer.

## Requirements

### Requirement: Issue registry
Reproduced game defects SHALL live in one typed registry. Each entry SHALL state the effect on players, any workaround, steps a player can follow without mods, expected and observed behaviour, the cause with code citations, which parts were observed and which derived, the builds it was confirmed on, and its report status. A page note or check SHALL fail to build when it names an entry that does not exist.

#### Scenario: Removed entry
- **WHEN** an entry is removed but a page note still names it
- **THEN** the build fails and names the note

### Requirement: Notes where readers meet the issue
Pages showing a value or rule an issue affects SHALL show one note in a shared style, with the issue's summary and a link to its entry.

#### Scenario: Affected item
- **WHEN** a reader opens an item whose tooltip shows a value the game does not apply
- **THEN** the page states the applied value and notes the issue with a link

### Requirement: Known Issues page
The site SHALL list every entry on one page, player facts first and developer facts below, linked from About and the footer and included in search and the sitemap.

Issue headings and their index labels SHALL use Title Case. When a later action restores an effect, name the action that fails. When affected items behave differently, name both outcomes. When items in a disappearing group have different names, do not imply that they match.

Summaries with distinct outcomes, places or groups SHALL start each part on a new line with an inline label in sentence case. Keep a group that spans places together under one label so the reader sees what belongs to it.

Every numbered step SHALL show the action first and its result on a separate line within the same step so a reader can see what to check before moving on.

#### Scenario: Reading an entry
- **WHEN** a reader opens an entry
- **THEN** they see the effect and workaround before steps, cause and evidence

#### Scenario: Knee guards
- **WHEN** the knee guards issue is listed
- **THEN** its heading and index label read "Equipping Knee Guards Does Not Update Armor"
- **AND** one numbered step reads "Equip Broad Knee Guards." with "Their tooltip shows ARMOR: 7, but Armor stays at 0." on the next line

#### Scenario: Potions with different outcomes
- **WHEN** the affected potions are listed
- **THEN** the heading and index label read "Some Potions Do Nothing or Have the Wrong Effect"
- **AND** one summary line reads "No effect: Health Potion, Mana Potion and Castleside Cider." with "No effect:" as its inline label

#### Scenario: Disappearing items
- **WHEN** the disappearing items issue is listed
- **THEN** its heading and index label read "Some Items Disappear After You Take One of Them"

### Requirement: Recheck after each update
After a game update, every entry SHALL be rechecked. Entries that no longer reproduce SHALL be removed with their notes, and entries that still reproduce SHALL record the new build.

#### Scenario: Fixed by a patch
- **WHEN** a build fixes a recorded defect
- **THEN** the entry and its notes are removed in the update
