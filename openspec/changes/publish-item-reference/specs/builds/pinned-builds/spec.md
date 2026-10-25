# Spec Delta

## Purpose

Pinned builds give every extraction and runtime check a fixed, identified copy of the game, independent of Steam's automatic updates and of the machine that holds the copy.

## ADDED Requirements

### Requirement: Machine-local locations
The locations of the Steam library, of pinned copies and of the private data repository's checkout SHALL come from configuration local to each machine. Neither repository SHALL contain machine-specific paths.

#### Scenario: Different library locations
- **WHEN** two machines with different Steam library locations pin the same Steam build
- **THEN** both produce identical build records

### Requirement: Pin a Steam build
The system SHALL pin an installed Steam build of The Bloodline as a separate local copy, without modifying the Steam installation.

#### Scenario: Pinning the installed build
- **WHEN** the installed Steam build is pinned
- **THEN** a copy of the game files exists under a directory named by the Steam build ID
- **AND** the Steam installation's files are unchanged

#### Scenario: Pinning an already pinned build
- **WHEN** a build that is already pinned on this machine is pinned again
- **THEN** the existing copy is kept unchanged and the command reports that the build is already pinned

### Requirement: Record build identity
The system SHALL record, for each pinned build, the Steam app ID, build ID, depot manifest ID, and a content hash of every game container file.

#### Scenario: Identifying a pinned build
- **WHEN** a build is pinned
- **THEN** a build record lists its app ID, build ID, depot manifest ID and per-container hashes

#### Scenario: Detecting a changed copy
- **WHEN** a pinned copy's container file no longer matches its recorded hash
- **THEN** extraction from that copy fails and names the changed file

### Requirement: Accepted build
The private data repository SHALL record which build is accepted. Published data SHALL come only from the accepted build's extracted records.

#### Scenario: Accepting a candidate build
- **WHEN** a candidate build's records are accepted
- **THEN** the data repository's accepted-build record names the candidate
- **AND** the site's data comes from the candidate's records

### Requirement: Keep only current game files
Each machine SHALL keep the game files of at most the accepted build and one candidate build. Cleanup SHALL delete other pinned copies only after confirmation.

#### Scenario: Cleanup after an update
- **WHEN** cleanup runs on a machine that still holds a build older than the accepted build
- **THEN** cleanup lists that build's game files and deletes them only after confirmation

#### Scenario: Build records outlive game files
- **WHEN** a build's game files are deleted
- **THEN** its build record and its extracted records remain in the private data repository
