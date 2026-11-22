## 1. Lab

- [x] 1.1 Observe whether learning a skill from a book or conversation costs a skill point
- [x] 1.2 Observe whether passive ranks can be refunded in the tree window
- [x] 1.3 Measure attributes at high point counts and below their starting value, and what the level up window's Health button does for a player
- [x] 1.4 Record character level and attribute vectors for the rule check

## 2. Rules and inputs

- [x] 2.1 Add rules for character level grants, attribute effects and regeneration with worded steps, citations and vectors, and extend `rules:check`
- [x] 2.2 Write the planner payload from the catalog and add its budget to the payload gate
- [ ] 2.3 Build the planner state, evaluator and diagnostics in `site/src/lib/planner/` with unit tests on invented inputs

## 3. Sharing and characters

- [ ] 3.1 Build the link codec with digests, size limits, version checks and unresolved keys, and fail publication on digest collisions
- [ ] 3.2 Build "Copy as text"
- [ ] 3.3 Build the browser store with explicit actions, previews and failure reporting

## 4. Interface

- [ ] 4.1 Compare the planner layout, attribute panel, hotbar, cues and text format in Plannotator
- [ ] 4.2 Build the planner route reusing the discipline rows, with keyboard use and both widths
- [ ] 4.3 Add active-character cues to discipline and skill pages
- [ ] 4.4 Add fixtures and browser tests for editing, invalid choices, links, text, characters and cues

## 5. Ship

- [ ] 5.1 Run the full check suite and the real-data build
- [ ] 5.2 Review the planner and the cued pages on real data at 1440 and 390 px and record the review in the commit bodies
- [ ] 5.3 Update the README for the planner and rule checks
- [ ] 5.4 Push, wait for CI, deploy to staging and production, and pass the smoke checks
- [ ] 5.5 Validate, archive and sync the specs
