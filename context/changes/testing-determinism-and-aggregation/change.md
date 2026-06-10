---
change_id: testing-determinism-and-aggregation
title: Test determinism and aggregation of macros and shopping lists
status: implemented
created: 2026-06-10
updated: 2026-06-10
archived_at: null
---

## Notes

Open a change folder for rollout Phase 1 of context/foundation/test-plan.md: "Determinizm obliczen i agregacji".
Risks covered: #1 (suma makro dnia rozjezdza sie z suma skladnikow), #2 (lista zakupow ma duplikaty lub zla sume gramatur).
Risk response intent:
- #1: prove the day macro total equals an INDEPENDENTLY computed sum (oracle derived from per-100g proportions, NOT lifted from the implementation) for fractional grams and multiple portions; the per-recipe-vs-per-portion gram convention (PRD Open Question 4) must be made explicit, not assumed.
- #2: prove two recipes sharing the same product yield exactly one shopping-list entry with summed grams and no duplicates; aggregation key must be product id, not name.
