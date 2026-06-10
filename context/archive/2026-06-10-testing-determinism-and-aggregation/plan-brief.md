# Determinism & Aggregation Tests — Plan Brief

> Full plan: `context/changes/testing-determinism-and-aggregation/plan.md`
> Research: `context/changes/testing-determinism-and-aggregation/research.md`

## What & Why

Close Phase 1 of the test plan for Risks #1 (macro day total diverges from real ingredient sum) and #2 (shopping list duplicates or wrong gram totals). Every new test uses an **inline independent oracle** — expected values are hand-computed from per-100g proportions and gram amounts, never lifted from the implementation formula.

## Starting Point

Domain calculators (`MacroCalculator`, `ShoppingListCalculator`) are pure `decimal` arithmetic, well-tested for happy paths. But existing tests suffer from the **tautology anti-pattern**: expected values mirror the implementation's own formula. Fractional grams and multi-recipe day totals are untested in the macro chain. Shopping list edge cases (same name/different ID, first-seen name) are undocumented.

## Desired End State

Every calculation chain level has at least one independent-oracle test. Fractional grams (3 portions), factor > 1 (entry > recipe portions), multi-recipe DayTotal, and aggregation edge cases are all covered. The per-whole-recipe gramature convention and aggregation-by-ProductId contract are visible in test names. Cookbook patterns in test-plan.md §6.1-6.2 document the oracle convention.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
|---|---|---|
| Oracle implementation | Inline arithmetic per test with `// Oracle:` comments | Must not share code with implementation; transparency > DRY. |
| Fractional gram edge cases | 3 portions + entry > recipe portions | Covers repeating decimal (factor < 1) and scale-up (factor > 1). |
| Same-ID/different-name test | Include (first-seen name wins) | Documents implicit behavior that's otherwise invisible. |
| Domain rounding assertion | Assert no rounding; leave to presentation | Explicit contract, not silent assumption. |

## Scope

**In scope:**
- 5 new test methods in `MacroCalculatorTests.cs`
- 3 new test methods in `ShoppingListCalculatorTests.cs`
- test-plan.md §3 status update and §6 cookbook patterns

**Out of scope:**
- Production code changes
- Application-layer test changes
- Rounding policy implementation
- New test projects or helpers

## Architecture / Approach

Pure unit-test additions to two existing test files. Each test follows the pattern: Arrange (build domain objects with known values) → Act (call calculator) → Assert (compare against inline arithmetic oracle). No mocks, no I/O, no shared oracle methods.

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Macro Oracle Tests | 5 new independent-oracle tests for the full macro chain | Getting the repeating-decimal assertion right (28-29 sig digit `decimal`) |
| 2. Shopping List Aggregation Tests | 3 new edge-case tests for aggregation boundaries | None — straightforward setup/assert |
| 3. Verification & Cookbook | Full suite green + cookbook patterns | None — mechanical |

**Prerequisites:** Research complete (✓), branch `feature/testing-determinism-and-aggregation` exists (✓)
**Estimated effort:** ~1 session across 3 phases

## Open Risks & Assumptions

- `decimal` repeating values (e.g., 100/3) produce trailing digits that depend on `decimal` division precision — tests must match the exact `decimal` result, not a rounded approximation.
- Assumption: no production code changes are needed to make the existing behavior testable — the domain calculators are already pure static functions.

## Success Criteria (Summary)

- All 8 new tests pass; all existing tests pass (zero regressions)
- Every `// Oracle:` comment traces arithmetic to per-100g values and gram amounts, not to implementation methods
- test-plan.md §6 cookbook is usable by a future contributor adding their first oracle test
