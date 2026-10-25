# Spec Delta

## Purpose

Source records turn a pinned build's packages into canonical data that later stages can trust, trace back to the game, and compare across builds.

## ADDED Requirements

### Requirement: Package inventory
Extraction SHALL write an inventory of every package in the pinned build, with its path, chunk hash and parse status.

#### Scenario: Complete inventory
- **WHEN** extraction runs on a pinned build
- **THEN** the inventory has one entry per package in the game's containers

#### Scenario: Comparing inventories
- **WHEN** inventories of two builds are compared
- **THEN** the comparison lists added, removed and changed packages by chunk hash

### Requirement: Complete parsing or failure
Extraction SHALL fail when a package, export or Blueprint function it reads does not parse completely. It SHALL NOT write records built from partially parsed data.

#### Scenario: Parse error in a read package
- **WHEN** an export that extraction reads raises a deserialization error
- **THEN** extraction exits with failure and names the package and export

#### Scenario: Truncated bytecode
- **WHEN** a Blueprint function's bytecode does not parse to its end
- **THEN** records that depend on that function are not written and the failure names the function

### Requirement: Value provenance
Every extracted value SHALL record the package, export and property path it came from.

#### Scenario: Tracing a value
- **WHEN** a reader of the records looks up an item's value
- **THEN** the record names the source package, export and property path

### Requirement: Inherited defaults
Extraction SHALL resolve Blueprint class defaults by merging each class's stored overrides over its ancestors' defaults, and SHALL record which ancestor supplied each value.

#### Scenario: Value inherited from a parent
- **WHEN** an item class stores no value for a field that its parent class sets
- **THEN** the item's record contains the parent's value and names the parent as its source

#### Scenario: Explicit override to an empty value
- **WHEN** a class explicitly overrides a field with zero, false, null or an empty array
- **THEN** the record keeps that explicit value and does not fall back to the ancestor

### Requirement: Readable enum and text values
Extraction SHALL resolve user-defined enum values to their display names and SHALL keep each localized text's namespace, key and source string.

#### Scenario: User-defined enum
- **WHEN** a record contains a value such as `E_ItemType::NewEnumerator3`
- **THEN** the record also carries the enum's display name for that value

### Requirement: Deterministic output
Extraction SHALL produce byte-identical records for the same build and extractor version, on any machine and operating system.

#### Scenario: Repeated extraction
- **WHEN** extraction runs twice on the same build
- **THEN** both outputs have identical hashes

#### Scenario: Checking committed records on another machine
- **WHEN** a machine that pinned the same build extracts it with the recorded extractor version
- **THEN** its output is identical to the records committed in the repository

### Requirement: Build-scoped records in the repository
Extracted records SHALL be stored in the repository per build, together with the build record and the extractor version that produced them.

#### Scenario: Records after the game files are gone
- **WHEN** a build's game files have been deleted
- **THEN** its extracted records can still be compared with a newer build's records
