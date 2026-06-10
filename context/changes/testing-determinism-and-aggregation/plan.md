# Determinism & Aggregation Tests — Implementation Plan

## Overview

Close Phase 1 of the test plan (Risks #1 and #2) by adding independent-oracle tests for macro calculation determinism and shopping list aggregation. No production code changes — this is a test-only phase. Every expected value is computed inline from first-principles arithmetic (per-100g × grams ÷ 100), never lifted from the implementation's formula.

## Current State Analysis

The domain layer is pure `decimal` arithmetic end-to-end — no `float`/`double`, no rounding. `MacroCalculator` and `ShoppingListCalculator` are static classes operating on immutable value objects. Existing tests cover the happy path well but suffer from the **tautology anti-pattern**: expected values mirror the implementation formula rather than an independent computation.

### Key Discoveries:

- Per-whole-recipe gramature convention is explicit: [RecipeIngredient.cs:37-40](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Recipes/RecipeIngredient.cs#L37-L40) XML doc says *"Gram amount of this product used across the whole recipe, not per serving."*
- `MacroNutrients.Scale(factor)` multiplies all four fields by `factor` — [MacroNutrients.cs:28-29](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Nutrition/MacroNutrients.cs#L28-L29)
- Shopping list aggregation key is `ProductId` (Guid), not name — [ShoppingListCalculator.cs:26](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs#L26)
- First-seen `ProductName` wins when same `ProductId` appears with different snapshot names — [ShoppingListCalculator.cs:32-34](file:///c:/Users/wikla/source/repos/Jadlify/src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs#L32-L34)
- Existing fractional-gram test exists in shopping list (`100/3`) but NOT in macro calculator — [ShoppingListCalculatorTests.cs:62-73](file:///c:/Users/wikla/source/repos/Jadlify/tests/Jadlify.Domain.Tests/Shopping/ShoppingListCalculatorTests.cs#L62-L73)
- `DayTotal_SumsMultipleMealEntries` uses the same recipe twice — no cross-recipe test — [MacroCalculatorTests.cs:62-82](file:///c:/Users/wikla/source/repos/Jadlify/tests/Jadlify.Domain.Tests/Nutrition/MacroCalculatorTests.cs#L62-L82)

## Desired End State

After this plan is complete:

1. Every macro calculation level (`ForProductAmount`, `RecipeTotal`, `RecipePerServing`, `ForMealEntry`, `DayTotal`) has at least one test whose expected values are computed inline from first principles — **not** from the implementation's formula.
2. Fractional grams (3-portion recipe) and entry.Portions > recipe.Portions are explicitly tested in the macro chain.
3. Shopping list aggregation has tests proving: same-name/different-ID → separate entries; same-ID/different-snapshot-name → one entry with first-seen name; independent-oracle math for aggregated grams.
4. All tests explicitly assert full `decimal` precision (no rounding), documenting the domain contract.
5. `pwsh ./.scripts/test-min.ps1 -Project Jadlify.Domain.Tests` passes with all new tests green.
6. test-plan.md §3 Phase 1 status is `planned`, §6 cookbook patterns document the oracle convention.

## What We're NOT Doing

- No production code changes — `MacroCalculator`, `ShoppingListCalculator`, and all domain types remain untouched.
- No Application-layer test changes — the handler tests already exercise the domain calculators transitively; independent oracle at that level is Phase 3 (e2e) territory.
- No rounding policy implementation — we assert the domain does NOT round; presentation-layer rounding is out of scope (Phase 3 / frontend).
- No changes to test infrastructure (no new test project, no new helper classes).

## Implementation Approach

Add new test methods to the two existing domain test classes. Each test uses **inline arithmetic with `// Oracle:` comments** explaining the first-principles calculation. No shared oracle helper method — the whole point is that each test's expected value is independently verifiable by reading just that test.

The oracle formula for macro calculations:
```
ingredient_macro_field = per100g_field × (wholeRecipeGrams / 100)
recipe_total = Σ ingredient_macro_field
per_serving = recipe_total / recipe.Portions
meal_entry_macro = per_serving × entry.Portions
day_total = Σ meal_entry_macro
```

The oracle formula for shopping list:
```
scaled_grams = wholeRecipeAmount × (entry.Portions / recipe.Portions)
aggregated = Σ scaled_grams  (grouped by ProductId)
```

---

## Phase 1: Macro Oracle Tests

### Overview

Add 5 new test methods to [MacroCalculatorTests.cs](file:///c:/Users/wikla/source/repos/Jadlify/tests/Jadlify.Domain.Tests/Nutrition/MacroCalculatorTests.cs) covering the independent-oracle, fractional-gram, factor > 1, multi-recipe DayTotal, and no-rounding gaps.

### Changes Required:

#### 1. Independent Oracle Tests

**File**: `tests/Jadlify.Domain.Tests/Nutrition/MacroCalculatorTests.cs`

**Intent**: Add test methods that compute expected macro values from first-principles inline arithmetic (per-100g × grams ÷ 100), not from the implementation's formula. Cover all calculation chain levels including the currently-missing fractional grams and multi-recipe scenarios.

**Contract**: Five new `[Fact]` methods:

1. `RecipeTotal_WithFractionalGrams_MatchesIndependentOracle` — 3-portion recipe with ingredients using non-round gram amounts (e.g., 33.3g). Oracle comment shows `per100g × (33.3 / 100)` inline.

2. `RecipePerServing_WithThreePortions_ProducesRepeatingDecimal` — Recipe with 3 portions; oracle computes total then divides by 3. Asserts the full `decimal` repeating value (28-29 sig digits), proving no rounding.

3. `ForMealEntry_WhenEntryPortionsExceedRecipePortions_ScalesUp` — Entry with 5 portions of a 3-portion recipe (factor > 1). Oracle: `(total / 3) × 5`.

4. `DayTotal_WithMultipleDifferentRecipes_MatchesIndependentOracle` — Two different recipes with different ingredients and portion counts, each used with different entry portions. Oracle sums each entry's contribution inline.

5. `WholeRecipeConvention_GramsAreForEntireRecipe_NotPerServing` — Explicitly-named test that creates a recipe with known grams and 4 portions, then asserts `RecipePerServing` equals `total / 4` (not raw grams). The test name and comments make the per-whole-recipe convention visible to future contributors.

### Success Criteria:

#### Automated Verification:

- All 5 new tests pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.Domain.Tests -Class MacroCalculatorTests`
- All existing 12 tests in `MacroCalculatorTests` still pass (no regressions)
- Build succeeds: `pwsh ./.scripts/build-min.ps1 -Project Jadlify.Domain.Tests`

#### Manual Verification:

- Verify each `// Oracle:` comment shows arithmetic derived from per-100g values and grams, NOT from calling `MacroCalculator` methods

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Shopping List Aggregation Tests

### Overview

Add 3 new test methods to [ShoppingListCalculatorTests.cs](file:///c:/Users/wikla/source/repos/Jadlify/tests/Jadlify.Domain.Tests/Shopping/ShoppingListCalculatorTests.cs) covering the same-name/different-ID, first-seen-name, and independent-oracle gaps.

### Changes Required:

#### 1. Aggregation Edge Case Tests

**File**: `tests/Jadlify.Domain.Tests/Shopping/ShoppingListCalculatorTests.cs`

**Intent**: Add tests that prove the shopping list aggregates by `ProductId` (not name), documents the first-seen name behavior, and uses inline independent-oracle arithmetic for the gram totals.

**Contract**: Three new `[Fact]` methods:

1. `ForMealEntries_SameProductNameDifferentIds_RemainSeparateEntries` — Two ingredients with identical `ProductName` but different `ProductId` guids. Assert two separate `ShoppingListItem` entries in the result.

2. `ForMealEntries_SameProductIdDifferentSnapshotNames_UsesFirstSeenName` — Two recipe entries sharing a `ProductId` but with different `ProductName` snapshots. Assert one aggregated entry using the first-encountered name.

3. `ForMealEntries_AggregatedGrams_MatchIndependentOracle` — Multiple recipes sharing products, with varied portions. Oracle computes `wholeRecipeAmount × (entryPortions / recipePortions)` per ingredient inline with `// Oracle:` comments, then sums per ProductId. Asserts the totals match.

### Success Criteria:

#### Automated Verification:

- All 3 new tests pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.Domain.Tests -Class ShoppingListCalculatorTests`
- All existing 6 tests in `ShoppingListCalculatorTests` still pass (no regressions)
- Build succeeds: `pwsh ./.scripts/build-min.ps1 -Project Jadlify.Domain.Tests`

#### Manual Verification:

- Verify each `// Oracle:` comment shows arithmetic derived from gram amounts and portion ratios, NOT from calling `ShoppingListCalculator` methods

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 3: Verification & Cookbook Update

### Overview

Run the full test suite to confirm no cross-project regressions, update test-plan.md with the Phase 1 status and cookbook patterns.

### Changes Required:

#### 1. Full Test Suite Run

**Intent**: Confirm all backend tests pass (Domain + Application + API + Infrastructure), not just the two changed test classes.

**Contract**: `pwsh ./.scripts/test-min.ps1` (no `-Project` filter)

#### 2. Test Plan Status Update

**File**: `context/foundation/test-plan.md`

**Intent**: Update §3 Phase 1 row status from `researched` to `planned`, and fill §6.1 and §6.2 cookbook patterns with the inline-oracle and aggregation-edge-case conventions established in this phase.

**Contract**: §3 Phase 1 status column, §6.1 `### 6.1 Adding a unit test`, §6.2 `### 6.2 Adding an integration test`.

### Success Criteria:

#### Automated Verification:

- Full test suite passes: `pwsh ./.scripts/test-min.ps1`
- Build succeeds: `pwsh ./.scripts/build-min.ps1`

#### Manual Verification:

- test-plan.md §6.1 and §6.2 entries are clear enough for a future contributor to follow the oracle pattern

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding.

---

## Testing Strategy

### Unit Tests:

- Independent-oracle macro tests (5 new in `MacroCalculatorTests`)
- Aggregation edge-case tests (3 new in `ShoppingListCalculatorTests`)

### Integration Tests:

- None in this phase — Application-layer tests already exercise domain calculators transitively. Independent oracles at Application level are Phase 3 (e2e) scope.

### Manual Testing Steps:

1. Review each new test's `// Oracle:` comment to verify arithmetic is from first principles
2. Verify test names clearly communicate the per-whole-recipe convention and aggregation-by-ProductId contract
3. Verify that cookbook pattern entries in test-plan.md §6.1-6.2 are actionable

## References

- Research: `context/changes/testing-determinism-and-aggregation/research.md`
- Test plan: `context/foundation/test-plan.md`
- Macro calculator: `src/Jadlify.Domain/Nutrition/MacroCalculator.cs`
- Shopping list calculator: `src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs`
- Existing macro tests: `tests/Jadlify.Domain.Tests/Nutrition/MacroCalculatorTests.cs`
- Existing shopping tests: `tests/Jadlify.Domain.Tests/Shopping/ShoppingListCalculatorTests.cs`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Macro Oracle Tests

#### Automated

- [x] 1.1 All 5 new macro oracle tests pass — d01eaff
- [x] 1.2 All 12 existing MacroCalculatorTests pass (no regressions) — d01eaff
- [x] 1.3 Build succeeds for Jadlify.Domain.Tests — d01eaff

#### Manual

- [x] 1.4 Each Oracle comment shows first-principles arithmetic, not implementation formula — d01eaff

### Phase 2: Shopping List Aggregation Tests

#### Automated

- [x] 2.1 All 3 new aggregation edge-case tests pass — 3325345
- [x] 2.2 All 6 existing ShoppingListCalculatorTests pass (no regressions) — 3325345
- [x] 2.3 Build succeeds for Jadlify.Domain.Tests — 3325345

#### Manual

- [x] 2.4 Each Oracle comment shows first-principles arithmetic, not implementation formula — 3325345

### Phase 3: Verification & Cookbook Update

#### Automated

- [x] 3.1 Full test suite passes (all backend test projects) — c73565a
- [x] 3.2 Full build succeeds — c73565a

#### Manual

- [x] 3.3 test-plan.md §6.1 and §6.2 cookbook entries are actionable for future contributors — c73565a
