# Daily Macro Summary (S-05) — Plan Brief

> Full plan: `context/changes/daily-macro-summary/plan.md`

## What & Why

Build roadmap slice S-05: a signed-in user sees the kcal and macro totals for a selected day plus the numeric difference against their daily goal (FR-013). This closes the calibration part of the planning flow — if the day's numbers aren't trusted, the S-06 shopping list won't be either.

## Starting Point

S-04 (commit `71c41fe`) shipped the planning storage/display layer: `GET /api/meal-plan?date=` lists a day's entries (no macros), `GET /api/daily-goal` returns the singleton goal or `null`, and `MacroCalculator.ForMealEntry(entry, recipe)` already computes one entry's macros from recipe ingredient snapshots. The `/meal-plan` page lists entries but shows no totals. The contract registry explicitly directs S-05 to compute day totals on read via `MacroCalculator`, never persisted.

## Desired End State

On `/meal-plan`, selecting a date shows a day-total panel, a per-macro "remaining" line (`goal − consumed`, with a subtle over/under color), and each entry card annotated with its own macros. With no goal set, totals still show, remaining hides, and a prompt links to `/goals`. A dedicated `GET /api/meal-plan/summary?date=` serves per-entry contributions (keyed by entry id) + day total + goal echo + signed remaining. The S-04 entry-list contract is untouched.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Compute vs persist | Compute on read via `MacroCalculator` | Contract registry mandates it; no parallel macro path | Contract registry |
| API surface | Dedicated `GET /api/meal-plan/summary?date=` | Clean contract, leaves S-04 list untouched, S-06-friendly | Plan |
| Summary payload | Per-entry macros keyed by id + total + goal + remaining | Single source for entry identity (list); no duplicated entry data | Plan |
| Delta direction | "Remaining" = `goal − consumed` (signed) | Natural for planning ("how much left"); negative when over | Plan |
| Granularity | Per-entry macros **and** day total | User asked to see each entry's contribution | Plan |
| No goal | Show totals, hide remaining, prompt to set goal | Summary stays useful; consistent with "plan works without a goal" | Plan |
| Over goal | Numbers + subtle color cue | FR-013 stays numeric; light readability aid; no progress bars | Plan |
| Placement | On the `/meal-plan` page | Same selected-day + date selector; one coherent screen | Plan |
| Precision | 1 decimal, reuse `formatMacro` | Consistent with recipe macro display | Plan |
| Ingredient load | New owner-scoped `ListByIdsWithIngredientsAsync` | Existing `ListByIdsAsync` is display-only (no ingredients) | Plan |

## Scope

**In scope:** day-total + per-entry macros + signed remaining, computed on read; new summary query/handler/DTOs; ingredient-loaded recipe batch read; `GET /api/meal-plan/summary`; React summary panel + per-entry card macros on `/meal-plan`; tests at every layer; S-05 contract-registry entry.

**Out of scope:** persisting totals; changing S-04/goal/recipe contracts; meal-type subtotals, progress bars, charts, percentages; weekly/multi-day summaries; goal history; export; new route or schema/migration.

## Architecture / Approach

Vertical slice: `MacroCalculator.DayTotal` (domain) → `GetDailyMacroSummaryQuery`/handler + DTOs (application, composing entries + recipes-with-ingredients + goal; signed remaining as a plain-`decimal` read-model since `MacroNutrients` is non-negative) → `GET /api/meal-plan/summary` (API) → `useDailyMacroSummary(date)` + `DailyMacroSummaryPanel` merged onto the existing `MealPlanPage` by entry id (React).

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Domain + Application summary | `DayTotal`, ingredient-loaded read, summary query/handler/DTOs + unit tests | Reusing display-only `ListByIdsAsync` would silently yield zero macros |
| 2. Summary API + integration tests | `GET /api/meal-plan/summary?date=`, owner-scoped/goal/over-goal tests | Cross-user isolation; route ordering vs `/{id:guid}` |
| 3. React UI on `/meal-plan` | Summary panel + remaining color + per-entry card macros + tests | Adding totals without disturbing S-04 list/CRUD |
| 4. Verification + handoff | `verify-min`, frontend gates, S-05 contract entry | Stable handoff for S-06 shopping list |

**Prerequisites:** S-04 complete (`/api/meal-plan`, `/api/daily-goal`, `/meal-plan` page all live). 
**Estimated effort:** ~3–4 focused sessions across 4 phases.

## Open Risks & Assumptions

- Display rounding is per value; displayed per-entry macros may not visually sum to the displayed total. Aggregation stays precise server-side; round only for display.
- A referenced recipe always exists (deletion blocked while in use); a missing recipe is a defensive zero-contribution case.
- Reusing `formatMacro` couples planning to `recipes/macroMath`; lifting it to a shared util is an acceptable alternative to keep one formatter.

## Success Criteria (Summary)

- A planned day shows correct totals, per-entry macros, and `goal − consumed` remaining, matching `MacroCalculator`.
- Over-goal macros render negative + red; with no goal, totals show and remaining is replaced by a set-goal prompt.
- User data stays isolated; no day totals are persisted and no S-04 contract changes.
