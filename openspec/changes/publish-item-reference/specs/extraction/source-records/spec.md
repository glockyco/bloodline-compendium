# Spec Delta

## Purpose

Source records turn the installed build's packages into local data that the catalog can trust and that anyone with the same build can regenerate.

## ADDED Requirements

### Requirement: Machine-local locations
The locations of the Steam library and of the output directory SHALL come from configuration local to each machine. The repository SHALL NOT contain machine-specific paths.

#### Scenario: Different library locations
- **WHEN** two machines with different Steam library locations extract the same Steam build with the same code
- **THEN** both produce identical records

#### Scenario: Missing configuration
- **WHEN** extraction runs without a valid configuration
- **THEN** it fails and names the missing or invalid setting

### Requirement: Build identity
Extraction SHALL record the Steam app ID, build ID and depot manifest IDs of the build it read, and SHALL NOT modify the Steam installation.

#### Scenario: Reading the installed build
- **WHEN** extraction reads the installed build
- **THEN** its output names the app ID, build ID and depot manifest IDs
- **AND** the installation's files are unchanged

#### Scenario: Update during extraction
- **WHEN** Steam changes the installed build while extraction runs
- **THEN** extraction fails and names the change

### Requirement: Complete objects only
Extraction SHALL NOT write records from partially parsed objects. An object or package that does not parse completely SHALL be left out and recorded as a gap with its package and error.

#### Scenario: Parse error in an object
- **WHEN** an object that extraction reads raises a deserialization error
- **THEN** no record contains data from that object
- **AND** the gaps name its package, object and error

#### Scenario: Package too large to load
- **WHEN** a level package exceeds what the parser can load
- **THEN** its placed actors are absent and the gaps name the package

### Requirement: Inherited defaults
Extraction SHALL resolve Blueprint class defaults by merging each class's stored overrides over its ancestors' defaults.

#### Scenario: Value inherited from a parent
- **WHEN** an item class stores no value for a field that its parent class sets
- **THEN** the item's record contains the parent's value

#### Scenario: Partial struct override
- **WHEN** a class overrides one field of a struct that its parent sets
- **THEN** the record contains the class's value for that field and the parent's values for the others

#### Scenario: Explicit override to an empty value
- **WHEN** a class explicitly overrides a field with zero, false, null or an empty array
- **THEN** the record keeps that explicit value and does not fall back to the ancestor

#### Scenario: Native default
- **WHEN** no Blueprint ancestor and no struct default sets a field that a native class declares
- **THEN** the record marks the field as unknown instead of guessing a value

### Requirement: Readable enum and text values
Extraction SHALL resolve user-defined enum values to their display names and SHALL keep each localized text's namespace, key and source string.

#### Scenario: User-defined enum
- **WHEN** a record contains a value such as `E_ItemType::NewEnumerator3`
- **THEN** the record also carries the enum's display name for that value

### Requirement: Regenerated output outside Git
Extraction SHALL write its output to a directory named by the Steam build ID inside the configured output location, which Git ignores. The repository's checks SHALL fail when a game file or extracted output is tracked.

#### Scenario: Output of an earlier build
- **WHEN** a newer build is extracted on a machine that extracted an earlier build
- **THEN** the earlier build's output remains for comparison

#### Scenario: Tracked game-derived file
- **WHEN** a commit adds a game file or a file from the output directory to the repository
- **THEN** the repository's checks fail and name the file

### Requirement: Deterministic output
Extraction SHALL produce byte-identical output for the same build and code on the same machine.

#### Scenario: Repeated extraction
- **WHEN** extraction runs twice on the same build with the same code
- **THEN** both outputs have identical hashes
