# Tasks

## 1. Tooling foundation

- [ ] 1.1 Add a Nix flake with one development shell for macOS and Linux (.NET SDK at least 10.0.402, Bun, Node for Node-hosted tools) and verify `nix develop -c dotnet --version` and `nix develop -c bun --version` on macOS
- [ ] 1.2 Add the .NET solution with `global.json`, locked NuGet restore, CSharpier and nullable analyzers, plus an xUnit v3 test project, and verify `dotnet test` passes on an empty test
- [ ] 1.3 Add the Bun workspace with ESLint, Prettier, svelte-check and Vitest, and verify each check runs clean on the empty workspace
- [ ] 1.4 Add a GitHub Actions workflow that runs every check inside the Nix shell with SHA-pinned actions, and Renovate configuration with grouped updates and manual review for CUE4Parse, and verify the workflow passes on a pushed branch once a remote exists

## 2. Pinned builds

- [ ] 2.1 Add machine-local configuration (Git-ignored file plus a committed example) for the Steam library and pinned-copy locations, and verify a missing or invalid file produces a clear error
- [ ] 2.2 Implement pinning with copy-on-write cloning where available and a full copy otherwise, and verify on macOS that pinning build 24464547 leaves the Steam installation unchanged and that re-pinning reports the existing copy
- [ ] 2.3 Write build records from the Steam app manifest and container hashes, and verify the record contains no machine paths and that a modified container fails verification
- [ ] 2.4 Implement the accepted-build record and the cleanup command with confirmation, and verify cleanup lists but keeps files until confirmed
- [ ] 2.5 Document pinning and cleanup in README.md and verify the documented commands run as written

## 3. Source records

- [ ] 3.1 Implement the package inventory with chunk hashes and parse status, and verify it lists all 42,277 packages of build 24464547
- [ ] 3.2 Enable fatal serialization errors and the end-of-script check for read functions, and verify with a test that a truncated export and truncated bytecode both fail extraction with the package named
- [ ] 3.3 Implement inherited-default merging with per-field ancestor provenance and unresolved native fields, and verify with tests for inherited values, partial struct overrides, replaced arrays and explicit empty overrides
- [ ] 3.4 Resolve user-defined enum display names and keep text namespace, key and source string, and verify `E_ItemType::NewEnumerator3` resolves to its display name
- [ ] 3.5 Write canonical records per item class and per DataTable under `data/builds/<build-id>/`, and verify two runs produce identical hashes
- [ ] 3.6 Export JSON Schema from the record types and generate TypeScript types from it, and verify a check fails when generated files are stale
- [ ] 3.7 Commit the records and icons for build 24464547 and verify a fresh extraction on the same build reproduces the committed files

## 4. Item catalog

- [ ] 4.1 Load records into the catalog with schema validation, and verify an invalid record fails the build with its file named
- [ ] 4.2 Build the reachability graph from shops, loot tables, recipes, quest rewards and playable-level placements, and verify a shop item is published with its shop as source
- [ ] 4.3 Write the review list of unreached items with their references, and verify demo-pack items appear on it and not in the catalog
- [ ] 4.4 Add reviewed overrides with reasons, and verify an override publishes an unreached item and shows its reason on the review list
- [ ] 4.5 Decode icons into content-addressed PNGs and list items without icons, and verify no empty image is substituted

## 5. Item reference site

- [ ] 5.1 Create the SvelteKit site with the static adapter and a layout that shows the game version and build ID, and verify the label in prerendered HTML
- [ ] 5.2 Add the item list with category filter and sorting, and verify filtering and sorting with Playwright
- [ ] 5.3 Add item pages at stable URLs with facts, icons and sources, and verify a weapon page shows damage, speed and source links
- [ ] 5.4 Add Pagefind search, and verify a partial name finds the item in a built site
- [ ] 5.5 Add the file-count and file-size gate, and verify it fails on a fixture that exceeds the configured limits

## 6. Deployment

- [ ] 6.1 Add Wrangler configuration for Workers static assets with prerendered 404 handling, and verify `wrangler deploy --dry-run` accepts the build
- [ ] 6.2 Add the deploy and rollback commands and document them in README.md, and verify a preview deployment serves an item page, after approval to deploy

## 7. End-to-end check

- [ ] 7.1 Run pin, extract, catalog, build and preview from a clean checkout on macOS, and verify the documented workflow produces the same records and a working preview
