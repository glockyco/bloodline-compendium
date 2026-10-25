# Proposal

## Why

The Bloodline has about 2,100 inventory items, and players have no current reference for them. The official wiki has 377 pages and no active editors. Item data can be read offline from the game's packages, so items are the cheapest place to prove the whole path from the installed game to a published page. Later content kinds, maps and mechanics guides reuse that path.

## What Changes

- Read the installed Steam build offline into local records, identified by its Steam build and depot manifest IDs. Records are regenerated from the game files whenever they are needed and never stored in Git.
- Leave out any object that does not parse completely and list it as a gap, instead of publishing partial data.
- Build an item catalog from the records: every named inventory item with its facts, its icon and the sources that the game data shows.
- Show uncertain and missing information on the site instead of withholding items. Items without a known source stay published, and a coverage page lists every gap.
- Publish a static site with an item list, a page per item, search, the coverage page and the Steam build on every page. Each deployment records the code commit and the Steam build it was built from.
- Deploy the site to Cloudflare with checks on the number and size of files.

## Capabilities

### New Capabilities

- `extraction/source-records`: reading the installed build into local records, with build identity, inherited defaults, readable enums and texts, and gaps for anything that does not parse.
- `catalog/items`: inventory items with their facts, icons and sources, reviewed exclusions, and coverage gaps.
- `site/item-reference`: item list, item pages, search, coverage page and build labels on the published site.

### Modified Capabilities

None. This is the first change.

## Impact

- New: a .NET 10 program using CUE4Parse that extracts records and builds the catalog, a SvelteKit site built with Bun and searched with Pagefind, a Nix development shell, GitHub Actions checks on invented fixtures, and a Cloudflare deployment from a machine with the game installed.
- Extracted records, icons and site data live in a Git-ignored local directory. The repository holds code, specs, invented fixtures and a short list of reviewed exclusions.
- Runtime tooling, level reachability, sources that exist only in Blueprint bytecode, maps, quests, skills, creatures and mechanics guides are out of scope.
