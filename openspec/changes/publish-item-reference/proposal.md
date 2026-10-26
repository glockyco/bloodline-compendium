# Proposal

## Why

The Bloodline needs a current searchable item reference alongside its official wiki. Item data can be read offline from the game's packages, so items are the starting point for the whole path from the installed game to a published page. Later content kinds, maps and mechanics guides reuse that path.

## What Changes

- Read the installed Steam build offline into local records, identified by its Steam build and depot manifest IDs. Records are regenerated from the game files whenever they are needed and never stored in Git.
- Leave out any object that does not parse completely and list it as a gap, instead of publishing partial data.
- Build an item catalog from the records with readable qualified public identities, facts, icons, acquisition sources and reverse uses. Compute chances only where consumer evidence establishes the complete rule.
- Publish every named inventory item, including items without a known source. Keep those pages accessible while the default list offers a counted reveal. Coverage explains every known gap.
- Publish an answer-first static item reference with Type and category filtering, shareable list state, search, a home hub, About, reviewed release metadata and factual discovery metadata.
- Deploy to Cloudflare with provenance, file limits, built-site graph integrity and accessible desktop and phone presentation.

## Capabilities

### New Capabilities

- `extraction/source-records`: reading the installed build into local records, with build identity, inherited defaults, readable enums and texts, and gaps for anything that does not parse.
- `catalog/items`: inventory items with readable public identities, facts, icons, sources, reverse uses, reviewed releases and exclusions, and coverage gaps.
- `site/item-reference`: answer-first item pages, faceted list, home hub, search, About, coverage, release labels, discovery metadata and publication acceptance.

### Modified Capabilities

None. This is the first change.

## Impact

- New: a .NET 10 program using CUE4Parse that extracts records and builds the catalog, a SvelteKit site built with Bun and searched with Pagefind, a Nix development shell, GitHub Actions checks on invented fixtures, and a Cloudflare deployment from a machine with the game installed.
- Extracted records, icons and site data live in a Git-ignored local directory. The repository holds code, specs, invented fixtures and a short list of reviewed exclusions.
- Runtime tooling, level reachability, sources that exist only in Blueprint bytecode, maps, quests, skills, creatures and mechanics guides are out of scope.
