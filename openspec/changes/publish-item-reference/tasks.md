# Tasks

## 1. Tooling foundation

- [x] 1.1 Add a Nix flake with one development shell for macOS and Linux (.NET SDK 10.0.403, Bun, Node 22), and verify `dotnet --version`, `bun --version` and `node --version` inside `nix develop` on macOS
- [x] 1.2 Add the .NET solution with `global.json`, central package management, locked restore, nullable analyzers with warnings as errors, CSharpier and an xUnit v3 test project, and verify that CUE4Parse restores under NuGet audit and that build, test and format checks pass
- [ ] 1.3 Add the SvelteKit site with the static adapter, TypeScript, ESLint, Prettier, svelte-check, Vitest and Playwright, and verify each check runs clean
- [ ] 1.4 Add the check that fails when a game file or extracted output is tracked by Git, and verify it fails on a planted file and passes otherwise
- [ ] 1.5 Add the GitHub Actions workflow that runs every check inside the Nix shell on invented fixtures, with SHA-pinned actions and no secrets, plus Renovate configuration with grouped updates and manual review for CUE4Parse, and verify the workflow passes on a pushed branch after approval to push

## 2. Extraction

- [x] 2.1 Add machine-local configuration (a Git-ignored file plus a committed example) for the Steam library and output locations, and verify a missing or invalid file produces an error naming the setting
- [ ] 2.2 Read build identity from Steam's app manifest and fail when the build changes during a run, and verify the output names build 24464547 and its depot manifest, contains no machine paths, and that a changed app manifest fails the run
- [ ] 2.3 Load packages with fatal serialization errors and record unreadable objects and packages as gaps, and verify with a test that a failing object leaves no record and that the 2.2 GB Moonhorn Cliffs level appears as a gap
- [ ] 2.4 Encode property values and merge inherited defaults, and verify with tests for inherited values, partial struct overrides, replaced arrays, explicit empty overrides, struct defaults and unknown native fields
- [ ] 2.5 Resolve user-defined enum display names and keep each text's namespace, key and source string, and verify `E_ItemType::NewEnumerator3` resolves to its display name
- [ ] 2.6 Write records for class defaults, DataTables, enums, placed actors per level and dialogues, and verify on build 24464547 that all 2,161 item classes and 48 DataTables appear and that placed vendors carry their stock
- [ ] 2.7 Decode item icons to PNG named by pixel hash, and verify sampled DXT1, DXT5 and uncompressed icons by viewing them
- [ ] 2.8 Verify that two extractions of build 24464547 produce identical output hashes

## 3. Item catalog

- [ ] 3.1 Select inventory items with identities and URLs, and verify that workbenches and containers are not items and that every URL is unique
- [ ] 3.2 Resolve item facts, categories and stats, marking unknown facts, and verify a weapon, an armour piece and a food item against the records
- [ ] 3.3 Add shop, currency shop, smithing, crafting and cooking sources, and verify sample items for each against the spike evidence
- [ ] 3.4 Add container loot, random loot, enemy drop and pickup sources, and verify sample items for each against the spike evidence
- [ ] 3.5 Add dialogue grant, fishing, gathering, farming, bounty note and settlement delivery sources, and verify sample items for each against the spike evidence
- [ ] 3.6 Add reviewed exclusions for the test levels, and verify that DevTestMap placements are not sources and that a stale entry fails the catalog
- [ ] 3.7 List coverage gaps, and verify that items without sources, missing icons, unknown facts and unread packages appear
- [ ] 3.8 Write site data with exported JSON Schema and generated TypeScript types, add invented site-data fixtures, and verify the fixtures match the schema and a check fails when generated files are stale

## 4. Item reference site

- [ ] 4.1 Add the layout with the Steam build label, and verify the label in prerendered HTML
- [ ] 4.2 Add the item list with category filter and sorting, and verify filtering and sorting with Playwright
- [ ] 4.3 Add item pages with facts, icons, sources and unknowns, and verify that a weapon page shows damage, speed and sources and that an item without sources links to the coverage page
- [ ] 4.4 Add Pagefind search, and verify that the start of a word finds the item in a built site
- [ ] 4.5 Add the coverage page, and verify it lists the fixture's gaps with links
- [ ] 4.6 Convert icons to WebP thumbnails at build time, and verify the item list loads thumbnails instead of full-size icons
- [ ] 4.7 Write the build information file and refuse production builds from a dirty tree, and verify the file's contents and the refusal
- [ ] 4.8 Add the file-count and file-size gate, and verify it fails on a fixture that exceeds the configured limits

## 5. Deployment

- [ ] 5.1 Add Wrangler configuration for an assets-only Worker with a prerendered 404 page, and verify `wrangler deploy --dry-run` accepts the build
- [ ] 5.2 Add the preview, production and rollback commands and document the update workflow in README.md, and verify a preview deployment serves an item page, after approval to deploy

## 6. End-to-end check

- [ ] 6.1 Run extract, catalog, site build and preview from a clean checkout on macOS, and verify the documented workflow produces a working preview
