# Proposal

## Why

The Bloodline has about 2,200 items, and players have no current reference for them. The official wiki has 377 pages and no active editors. Item data can be read offline from the game's packages, so items are the cheapest place to prove the whole path from a pinned game build to a published page. Later content kinds, maps and mechanics guides depend on that path.

## What Changes

- Pin each supported Steam build as a local copy with a recorded identity, and keep only the current build's game files.
- Read the game's packages offline into canonical, build-scoped records. Every value names the package, export and property it came from, including the ancestor it was inherited from.
- Fail the extraction when a package or function does not parse completely, instead of publishing partial data.
- Store each build's extracted records and icons in a private data repository, so changes between builds can be compared after older game files are deleted. The public repository holds code, specs and invented test data. The published site is the only public form of game data.
- Build an item catalog from the records. Publish only items that the game can actually give the player, and hold back items that nothing reaches for review.
- Publish a static site with an item list, an item page per published item, search, and the game build on every page. Each deployment records the code commit, data commit and game build it was built from.
- Deploy the site to Cloudflare with checks on the number and size of files.

## Capabilities

### New Capabilities

- `builds/pinned-builds`: pinning Steam builds as local copies, recording their identity, and keeping only the current build's game files.
- `extraction/source-records`: reading packages into canonical records with per-value provenance, inherited defaults and completeness checks.
- `catalog/items`: item records, display names, categories, icons, and which items are reachable in the game.
- `site/item-reference`: item list, item pages, search and build labels on the published site.

### Modified Capabilities

None. This is the first change.

## Impact

- New: a .NET 10 extractor using CUE4Parse, a Bun/TypeScript catalog and site (SvelteKit, Pagefind), a Nix development shell, GitHub Actions checks on invented fixtures, and a Cloudflare deployment from a machine with both repositories.
- New private repository `bloodline-compendium-data` for extracted records, build records, icons, review lists and overrides. Build 24464547's records take about 38 MB raw, 1.7 MB gzipped.
- Runtime tooling, maps, quests, skills, creatures and mechanics guides are out of scope. They follow in later changes on the same path.
