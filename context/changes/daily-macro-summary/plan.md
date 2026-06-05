# Daily Macro Summary (S-05) Implementation Plan

## Overview

Give a signed-in user a per-day macro summary on the meal-plan screen: the macro contribution of each meal entry, the day's total kcal/protein/fat/carbohydrates, and the numeric "remaining" against their daily goal (FR-013). Everything is computed on read through `MacroCalculator` from the existing meal entries and their current recipes' ingredient snapshots — no day totals are persisted, and no parallel macro path is created.

## Current State Analysis

S-04 (commit `71c41fe`) landed the planning storage and display layer this slice builds on:

- `GET /api/meal-plan?date=` lists a selected day's entries (`MealPlanEntryResponse`: id, date, recipeId, recipeName, mealType, portions) — display data only, **no macros** ([MealPlanEndpoints.cs](src/Jadlify.API/Planning/MealPlanEndpoints.cs), [MealPlanEntryDto.cs](src/Jadlify.Application/Planning/MealPlanEntryDto.cs)).
- `GET /api/daily-goal` returns the singleton `DailyGoalResponse` or a literal JSON `null` when unset ([DailyGoalEndpoints.cs](src/Jadlify.API/Planning/DailyGoalEndpoints.cs)).
- `MacroCalculator.ForMealEntry(MealPlanEntry, Recipe)` already computes one entry's macros (per-serving × portions) from recipe ingredient snapshots ([MacroCalculator.cs:40](src/Jadlify.Domain/Nutrition/MacroCalculator.cs)). `MacroNutrients` supports `+` and `Scale`, and is **non-negative by construction** ([MacroNutrients.cs:5](src/Jadlify.Domain/Nutrition/MacroNutrients.cs)).
- The frontend `/meal-plan` page (`MealPlanPage`) lists entries via `useMealPlan(date)`; `/goals` edits the goal via `useDailyGoal` ([MealPlanPage.tsx](src/Jadlify.Web/src/planning/MealPlanPage.tsx), [useDailyGoal.ts](src/Jadlify.Web/src/planning/useDailyGoal.ts)). Recipe macros are displayed at 1 decimal via `formatMacro` ([macroMath.ts:36](src/Jadlify.Web/src/recipes/macroMath.ts)).
- The contract registry pre-decides the architecture: *"S-05 must compute day totals from each meal entry plus its current recipe's ingredient snapshots through `MacroCalculator.ForMealEntry(...)` rather than persisting precomputed day totals"* ([contract-surfaces.md:52](docs/reference/contract-surfaces.md)).

### Key Discoveries

- **Ingredient-loading gap**: [`IRecipeRepository.ListByIdsAsync`](src/Jadlify.Application/Recipes/IRecipeRepository.cs:25) deliberately omits `Ingredients` (S-04 only needs names) — see the explicit comment at [RecipeRepository.cs:74](src/Jadlify.Infrastructure/Persistence/Repositories/RecipeRepository.cs). Macro math needs `recipe.Ingredients`, so S-05 needs an ingredient-loaded owner-scoped batch read; reusing `ListByIdsAsync` as-is would silently yield zero macros.
- **`MacroNutrients` cannot hold a negative**: the "remaining" value (`goal − consumed`) goes negative when the user is over goal. It therefore cannot be a `MacroNutrients`; it must be a signed read-model with plain `decimal` fields, computed outside the `MacroNutrients` type.
- **Recipe deletion is blocked while referenced** by a meal entry (`Recipe.InUse`, [RecipeRepository.cs:117](src/Jadlify.Infrastructure/Persistence/Repositories/RecipeRepository.cs)), so an entry's recipe is guaranteed to exist; a missing recipe is a defensive zero-contribution case, not an expected state.
- **Established vertical-slice cadence** (Domain → Application → API → React, tests at each layer) mirrors S-04 exactly; tests use in-memory fakes (`FakeMealPlanRepository`, `FakeRecipeRepository`) whose `ListByIds*` return seeded recipes with their ingredients intact.

## Desired End State

On `/meal-plan`, after selecting a date, the user sees:

- A **day-total panel** with kcal/protein/fat/carbohydrates for that day.
- A **remaining line** per macro (`goal − consumed`) when a goal is configured, with a subtle color cue (over goal → red, within → neutral/positive). When no goal is configured, the totals still show, the remaining line is hidden, and a short prompt links to set a goal.
- **Per-entry macros** shown on each meal-entry card.

All numbers render at 1 decimal place (reusing the recipe `formatMacro` convention). The day's macro data is served by a dedicated `GET /api/meal-plan/summary?date=` endpoint that returns per-entry contributions keyed by entry id, the day total, the goal echo, and the signed remaining. The S-04 `GET /api/meal-plan?date=` entry-list contract is unchanged.

Verify: with a goal set and recipes planned, totals and per-entry macros equal `MacroCalculator` output; remaining equals `goal − total`; an over-goal macro shows negative + red; clearing the goal hides remaining and shows the prompt; another user's entries/recipes never appear.

## What We're NOT Doing

- Not persisting any precomputed day or entry totals (compute on read only).
- Not changing the S-04 `GET /api/meal-plan?date=`, `/api/daily-goal`, or recipe contracts.
- Not adding per-meal-type subtotals, progress bars, charts, percentages, weekly/multi-day summaries, goal history, or export.
- Not adding a new route or page — the summary lives on the existing `/meal-plan` screen.
- Not changing recipe macro math, schema, or migrations.

## Implementation Approach

Follow the existing vertical slice. Add one deterministic day-total surface to the domain `MacroCalculator`. Add an owner-scoped ingredient-loaded recipe batch read. Add a read-only `DailyMacroSummaryQuery`/handler that composes entries + recipes + goal into a read model exposing per-entry macros, day total, goal echo, and signed remaining. Expose it as a new `GET` under the existing `/api/meal-plan` group. On the frontend, add a `useDailyMacroSummary(date)` query, a summary panel component, and merge per-entry macros onto the existing entry cards by entry id; widen the meal-plan mutation invalidations to refresh the summary.

## Critical Implementation Details

- **Signed remaining vs non-negative totals.** Per-entry macros and the day total are non-negative and use the existing `RecipeMacroSummaryDto` shape. The remaining (`goal − consumed`) is a separate signed read-model with plain `decimal` fields — do not route it through `MacroNutrients` (its constructor throws on negatives).
- **Display rounding is per value, not summed.** The server returns full-precision `decimal`; the client rounds each value independently to 1 dp. Displayed per-entry values are therefore not guaranteed to visually sum to the displayed total. Keep all aggregation in precise decimals server-side; round only for display. This is acceptable for MVP and should not be "fixed" by rounding before summing.
- **Empty/missing recipe is defensive only.** A referenced recipe always exists (deletion is blocked while in use). If a recipe id is nonetheless absent from the batch read, treat its entries as zero contribution rather than failing the whole summary.

## Phase 1: Domain Day-Total + Application Summary Read Model

### Overview

Add the deterministic day-total surface, the ingredient-loaded recipe read, and the summary query/handler/DTOs with unit tests. No API or UI yet.

### Changes Required

#### 1. Domain day-total math

**File**: `src/Jadlify.Domain/Nutrition/MacroCalculator.cs`

**Intent**: Provide a single deterministic "sum of a day's entries" surface so day aggregation lives beside the other macro math (per the contract registry's "single macro-math surface" rule), instead of being open-coded in a handler.

**Contract**: New `static MacroNutrients DayTotal(IEnumerable<(MealPlanEntry entry, Recipe recipe)> entries)` that folds `ForMealEntry(entry, recipe)` over the pairs starting from `MacroNutrients.Zero`. Null-guards the argument; an empty sequence returns `MacroNutrients.Zero`.

#### 2. Owner-scoped recipe read with ingredients

**File**: `src/Jadlify.Application/Recipes/IRecipeRepository.cs`, `src/Jadlify.Infrastructure/Persistence/Repositories/RecipeRepository.cs`

**Intent**: Load the current user's recipes for a set of ids **including ingredients**, so the summary can run deterministic macro math. Keep the existing display-only `ListByIdsAsync` lean and unchanged.

**Contract**: New `Task<IReadOnlyList<Recipe>> ListByIdsWithIngredientsAsync(IReadOnlyCollection<Guid> ids, CancellationToken)` on the port; implementation mirrors `ListByIdsAsync` (owner-scoped `user_id` filter, empty-input short-circuit, requested-ids `Contains` filter) but adds `.Include(recipe => recipe.Ingredients)`. Must never return another user's recipes. Add the same method to the test `FakeRecipeRepository` (returns seeded recipes, which already carry their ingredients).

#### 3. Summary read-model DTOs

**File**: `src/Jadlify.Application/Planning/DailyMacroSummaryDto.cs` (new), `src/Jadlify.Application/Planning/MealEntryMacroDto.cs` (new), `src/Jadlify.Application/Planning/MacroRemainingDto.cs` (new)

**Intent**: Read models for the day summary: per-entry contribution (keyed by entry id for client-side merge), the day total, the goal echo, and the signed remaining.

**Contract**:
- `MealEntryMacroDto(Guid EntryId, RecipeMacroSummaryDto Macros)` — reuses the non-negative `RecipeMacroSummaryDto`.
- `MacroRemainingDto(decimal Calories, decimal Protein, decimal Fat, decimal Carbohydrates)` — signed, **no** non-negative guard; a static factory `FromGoalAndTotal(MacroNutrients goal, MacroNutrients total)` computes `goal − total` per field.
- `DailyMacroSummaryDto(DateOnly Date, IReadOnlyList<MealEntryMacroDto> Entries, RecipeMacroSummaryDto Total, PlanningMacroGoalDto? Goal, MacroRemainingDto? Remaining)`. `Goal` and `Remaining` are both null exactly when no goal is configured.

#### 4. Summary query + handler

**File**: `src/Jadlify.Application/Planning/DailyMacroSummary/GetDailyMacroSummaryQuery.cs` (new), `.../GetDailyMacroSummaryQueryHandler.cs` (new)

**Intent**: Compose entries + recipes + goal into the summary read model, all owner-scoped, on read.

**Contract**: `GetDailyMacroSummaryQuery(DateOnly Date) : IQuery<DailyMacroSummaryDto>`. Handler depends on `IMealPlanRepository`, `IRecipeRepository`, `IDailyMacroGoalRepository`. Steps: list entries for the date; batch-load recipes-with-ingredients for the distinct recipe ids; per entry compute `MacroCalculator.ForMealEntry` (missing recipe → `MacroNutrients.Zero`); compute total via `MacroCalculator.DayTotal`; load the current goal; build `Remaining` only when a goal exists. Returns `Result.Ok(dto)` — an empty day and a missing goal are both normal success states (mirrors `GetDailyGoalQueryHandler`). Register the handler with DI following the existing planning handler registration.

### Success Criteria

#### Automated Verification

- Build passes: `pwsh ./.scripts/build-min.ps1`
- Domain tests pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.Domain.Tests` — `DayTotal` sums multiple `(entry, recipe)` pairs and returns `Zero` for empty input.
- Application tests pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.Application.Tests` covering: per-entry macros + total match `MacroCalculator`; remaining = `goal − total` (incl. a negative/over-goal field); `Goal`/`Remaining` null when no goal; empty day returns zero total; cross-user/missing recipe contributes zero.
- Format passes: `pwsh ./.scripts/format-min.ps1`

#### Manual Verification

- Numbers in the handler tests match a hand calculation for a known recipe/portion case.

**Implementation Note**: After automated verification passes, pause for manual confirmation before Phase 2. Phase blocks use plain bullets; the `## Progress` section owns the checkboxes.

---

## Phase 2: Summary API Endpoint + Integration Tests

### Overview

Expose the summary read model over HTTP and prove it is owner-scoped and correct across the goal/no-goal and over/under cases.

### Changes Required

#### 1. Summary response contracts

**File**: `src/Jadlify.API/Planning/MealPlanContracts.cs` (extend) or `src/Jadlify.API/Planning/DailyMacroSummaryContracts.cs` (new sibling)

**Intent**: Wire response records that mirror the DTOs and serialize the signed remaining and nullable goal cleanly.

**Contract**: `MealEntryMacroResponse(Guid EntryId, MacroSummaryResponse Macros)`, a four-field `MacroSummaryResponse` (decimal calories/protein/fat/carbohydrates, reused for per-entry, total, goal echo, and signed remaining), and `DailyMacroSummaryResponse(DateOnly Date, IReadOnlyList<MealEntryMacroResponse> Entries, MacroSummaryResponse Total, MacroSummaryResponse? Goal, MacroSummaryResponse? Remaining)` with a `FromDto` factory. (Reusing the existing `RecipeMacroSummaryResponse` shape is acceptable if preferred; keep one consistent four-field macro shape.)

#### 2. Summary endpoint

**File**: `src/Jadlify.API/Planning/MealPlanEndpoints.cs`

**Intent**: Add the read route to the existing `/api/meal-plan` group through the same Minimal API + mediator + `ToProblem` pattern.

**Contract**: `mealPlan.MapGet("/summary", ...)` taking `DateOnly date`, dispatching `GetDailyMacroSummaryQuery(date)`, returning `Results.Ok(DailyMacroSummaryResponse.FromDto(...))` on success or `result.ToProblem()` on failure. Inherits the global authenticated fallback policy (no `AllowAnonymous`). Route ordering: `"/summary"` must not collide with the existing `"/{id:guid}"` routes — a literal segment is unambiguous against a `:guid` constraint, but confirm registration order keeps both reachable.

### Success Criteria

#### Automated Verification

- Build passes: `pwsh ./.scripts/build-min.ps1`
- API tests pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.API.Tests` covering: `200` with correct totals/per-entry macros for a planned day with a goal; remaining = `goal − total` including an over-goal negative; `Goal`/`Remaining` serialized as `null` when no goal; empty day returns zero total and empty entries; anonymous → `401`; a second user sees only their own day (cross-user isolation).
- Format/lint passes: `pwsh ./.scripts/format-min.ps1`

#### Manual Verification

- GET /api/meal-plan/summary?date=<today> returns the expected JSON shape against a locally seeded day.

**Implementation Note**: Pause for manual confirmation before Phase 3.

---

## Phase 3: React Summary UI On `/meal-plan`

### Overview

Render the day-total panel, the remaining line with subtle color, the empty-goal prompt, and per-entry macros on the cards — without disturbing the S-04 entry list/CRUD behavior.

### Changes Required

#### 1. Summary types + query hook

**File**: `src/Jadlify.Web/src/planning/types.ts`, `src/Jadlify.Web/src/planning/useDailyMacroSummary.ts` (new)

**Intent**: Type the summary response and fetch it for the selected date.

**Contract**: Add `MacroSummary` (four numbers), `MealEntryMacro` (`entryId`, `macros`), and `DailyMacroSummary` (`date`, `entries`, `total`, `goal: MacroSummary | null`, `remaining: MacroSummary | null`) types. New `useDailyMacroSummary(date)` mirroring `useMealPlan`: query key `['planning','meal-plan-summary',date]`, calls `GET /api/meal-plan/summary?date=`, `enabled: Boolean(session && date)`. Export the key for invalidation.

#### 2. Refresh summary on mutations

**File**: `src/Jadlify.Web/src/planning/useMealPlanMutations.ts`

**Intent**: Keep the summary in sync after add/update/delete.

**Contract**: Each meal-plan mutation's `onSuccess` additionally invalidates `['planning','meal-plan-summary', date]` for the affected date (alongside the existing `mealPlanQueryKey(date)` invalidation).

#### 3. Summary panel component

**File**: `src/Jadlify.Web/src/planning/DailyMacroSummaryPanel.tsx` (new)

**Intent**: Show the day total and, when a goal exists, the remaining per macro with a subtle over/under color; otherwise show the totals plus a prompt linking to `/goals`.

**Contract**: Props `{ summary: DailyMacroSummary | undefined; isLoading; isError }`. Renders four macro rows (kcal/protein/fat/carbs) using the recipe `formatMacro` (import from `../recipes/macroMath`, or lift it to a shared `formatMacro` util — keep one formatter so precision stays consistent). Remaining rows apply a neutral/positive class when `>= 0` and a red class when `< 0`. No goal → hide remaining rows, render a short message with a link/CTA to set a goal. Has its own loading/error text consistent with the page's existing states.

#### 4. Wire panel + per-entry macros into the page

**File**: `src/Jadlify.Web/src/planning/MealPlanPage.tsx`

**Intent**: Place the panel on the selected-day view and annotate each entry card with its macros.

**Contract**: Call `useDailyMacroSummary(date)`; build a `Map<entryId, MacroSummary>` from `summary.entries`; render `DailyMacroSummaryPanel` near the day's entry list; on each existing entry card add a compact macro line read from the map by `entry.id` (fallback to dashes if not yet loaded). The entry list itself still comes from `useMealPlan(date)` (S-04 unchanged) — the summary is additive.

### Success Criteria

#### Automated Verification

- Lint passes: `npm run lint` (in `src/Jadlify.Web`)
- Frontend tests pass: `npm test` (in `src/Jadlify.Web`) — Vitest + RTL covering: totals + remaining render for a goal day; over-goal macro shows the negative/over styling; no goal hides remaining and shows the set-goal prompt; per-entry macros appear on cards.
- Build passes: `npm run build` (in `src/Jadlify.Web`)

#### Manual Verification

- On `/meal-plan`, selecting a day with planned recipes shows correct totals, per-entry macros, and remaining; an over-goal macro is visibly red.
- Clearing the goal on `/goals` and returning shows totals with the remaining hidden and a prompt to set a goal.
- Adding/editing/deleting an entry updates the summary without a manual refresh.
- Layout is reasonable on a narrow (mobile) width.

**Implementation Note**: Pause for manual confirmation before Phase 4.

---

## Phase 4: Verification + Contract Handoff

### Overview

Run the narrow end-to-end verification and record the S-05 contract for S-06 (shopping list), which reuses the same entries and recipe snapshots.

### Changes Required

#### 1. Contract registry update

**File**: `docs/reference/contract-surfaces.md`

**Intent**: Add an S-05 section documenting the summary endpoint and the compute-on-read decision so S-06 reuses it instead of building a parallel macro/aggregation path.

**Contract**: New "Daily Macro Summary Contracts (S-05)" section stating: `GET /api/meal-plan/summary?date=yyyy-MM-dd` returns per-entry macro contributions (keyed by entry id), the day total, the goal echo, and the signed remaining (`goal − consumed`, negative when over); totals are computed on read via `MacroCalculator.DayTotal` / `ForMealEntry` against recipe ingredient snapshots and never persisted; `IRecipeRepository.ListByIdsWithIngredientsAsync` is the owner-scoped ingredient-loaded batch read for macro math (vs the display-only `ListByIdsAsync`); the S-04 `GET /api/meal-plan?date=` list contract is unchanged.

### Success Criteria

#### Automated Verification

- Full backend verify passes: `pwsh ./.scripts/verify-min.ps1`
- Frontend lint + test + build pass: `npm run lint`, `npm test`, `npm run build` (in `src/Jadlify.Web`)

#### Manual Verification

- End-to-end on a fresh local stack: set a goal, plan recipes across meal types, confirm totals, per-entry macros, and remaining match a hand calculation; confirm over-goal coloring and the no-goal prompt.
- `contract-surfaces.md` S-05 section accurately describes the shipped endpoint.

---

## Testing Strategy

### Unit Tests

- Domain: `MacroCalculator.DayTotal` — multi-pair sum, empty → `Zero`.
- Application: summary handler — totals/per-entry macros vs `MacroCalculator`; remaining incl. negative; null goal → null goal/remaining; empty day; missing/cross-user recipe → zero contribution.

### Integration Tests

- API: `GET /api/meal-plan/summary` — correct shape and values with a goal; over-goal negative remaining; null goal/remaining serialization; empty day; `401` anonymous; cross-user isolation.

### Manual Testing Steps

1. Sign in, set a daily goal on `/goals`.
2. On `/meal-plan`, add several recipes across meal types and portions for one date.
3. Confirm the day total, per-entry macros, and remaining match a hand calculation.
4. Push one macro over its goal and confirm the negative value renders red.
5. Clear the goal and confirm totals remain, remaining hides, and the set-goal prompt appears.
6. Edit/delete an entry and confirm the summary updates without manual refresh.

## Performance Considerations

The summary issues at most three owner-scoped reads per day (entries, recipes-with-ingredients batch, goal) at MVP scale (≤ ~200 recipes / day's entries), well within the PRD's `< 800 ms p95` budget for the day summary. Recipe lookup is a single batched `IN` query, not per entry.

## Migration Notes

None. No schema changes — the summary is computed on read from existing tables.

## References

- Roadmap slice: `context/foundation/roadmap.md` (S-05)
- PRD: FR-013 (`context/foundation/prd.md`)
- S-04 contract handoff: `docs/reference/contract-surfaces.md:52`
- S-04 plan: `context/changes/daily-goals-and-meal-plan/plan.md`
- Macro math: `src/Jadlify.Domain/Nutrition/MacroCalculator.cs:40`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Domain Day-Total + Application Summary Read Model

#### Automated

- [x] 1.1 Build passes: `pwsh ./.scripts/build-min.ps1` — 56b8418
- [x] 1.2 Domain tests pass (`DayTotal` sum + empty → Zero) — 56b8418
- [x] 1.3 Application tests pass (per-entry/total vs MacroCalculator; remaining incl. negative; null goal; empty day; missing/cross-user recipe → zero) — 56b8418
- [x] 1.4 Format passes: `pwsh ./.scripts/format-min.ps1` — 56b8418

#### Manual

- [x] 1.5 Handler test numbers match a hand calculation — 56b8418

### Phase 2: Summary API Endpoint + Integration Tests

#### Automated

- [x] 2.1 Build passes:
  `pwsh ./.scripts/build-min.ps1` — 8016cc1
- [x] 2.2 API tests pass (totals/per-entry; over-goal negative; null goal/remaining; empty day; 401 anonymous; cross-user isolation)
- [x] 2.3 Format/lint
  passes: `pwsh ./.scripts/format-min.ps1` — 8016cc1

#### Manual

- [x] 2.4
  `GET /api/meal-plan/summary?date=` returns expected JSON locally — 8016cc1

### Phase 3: React Summary UI On `/meal-plan`

#### Automated

- [x] 3.1 Lint passes: `npm run lint`
- [x] 3.2 Frontend tests pass (totals+remaining; over-goal styling; no-goal prompt; per-entry macros) — 533db33
- [x] 3.3 Build passes: `npm run build` — 533db33

#### Manual

- [x] 3.4 Day with recipes shows correct totals/per-entry/remaining; over-goal is red — 533db33
- [x] 3.5 No goal → totals shown, remaining hidden, set-goal prompt appears — 533db33
- [x] 3.6 Add/edit/delete refreshes the summary automatically — 533db33
- [x] 3.7 Layout reasonable on narrow width — 533db33

### Phase 4: Verification + Contract Handoff

#### Automated

- [x] 4.1 Backend verify passes: `pwsh ./.scripts/verify-min.ps1`
- [x] 4.2 Frontend lint + test + build pass

#### Manual

- [x] 4.3 End-to-end hand-calculation check on a fresh local stack
- [x] 4.4 `contract-surfaces.md` S-05 section matches the shipped endpoint
