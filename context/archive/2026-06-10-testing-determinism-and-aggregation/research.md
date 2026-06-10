---
date: "2026-06-10T13:03:00+02:00"
researcher: Antigravity
git_commit: 0f1436bd59a3ab46a620319f29a8c59f60044e6a
branch: feature/testing-determinism-and-aggregation
repository: Jadlify
topic: "Macro calculation determinism and shopping list aggregation — test readiness research for Phase 1"
tags: [research, codebase, nutrition, macro-calculator, shopping-list, determinism, aggregation, testing]
status: complete
last_updated: "2026-06-10"
last_updated_by: Antigravity
---

# Research: Macro Calculation Determinism and Shopping List Aggregation

**Date**: 2026-06-10T13:03:00+02:00
**Researcher**: Antigravity
**Git Commit**: 0f1436bd59a3ab46a620319f29a8c59f60044e6a
**Branch**: feature/testing-determinism-and-aggregation
**Repository**: Jadlify (pWiklacz/Jadlify)

## Research Question

What is the precise calculation chain for daily macro totals and shopping list aggregation? What convention governs per-recipe vs per-portion gramatures? Where are the gaps in existing test coverage that Phase 1 (Risks #1 and #2) must close — specifically: independent oracle tests, fractional gram edge cases, multi-portion scaling, and cross-recipe product-id aggregation?

## Summary

The codebase implements a **complete, well-structured calculation chain** from per-100g product values through to daily macro totals and shopping list aggregation. The **per-whole-recipe gramature convention** is explicit (RecipeIngredient.WholeRecipeAmount is grams for the entire recipe, not per portion — confirmed by XML doc and naming). Both `MacroCalculator` and `ShoppingListCalculator` are **pure domain services** that operate on `decimal` arithmetic — no rounding, no float lossy conversions. Existing tests cover the happy path well but have **critical gaps** this phase must close:

1. **No independent oracle** — all expected values in tests are hand-derived from the implementation's own formula, not independently computed from first principles (the tautology anti-pattern from the test plan).
2. **Fractional grams untested in macro calculation** — `ShoppingListCalculatorTests.ForMealEntries_DoesNotRoundBeforeReturning` exercises a 100/3 division, but `MacroCalculatorTests` uses only whole-number grams (200g, 50g, 100g, 150g).
3. **Multi-recipe day with independent oracle** — `DayTotal_SumsMultipleMealEntries` exists but uses only one recipe; no test combines multiple recipes with different portion counts and validates against an independent oracle.
4. **Aggregation key is ProductId** — confirmed correct per test-plan Risk #2, but no test explicitly asserts that two products with the _same name but different IDs_ remain separate entries.

## Detailed Findings

### 1. Gramature Convention (PRD Open Question 4)

**Finding: per-whole-recipe, explicitly documented.**

The `RecipeIngredient.WholeRecipeAmount` property (file: `src/Jadlify.Domain/Recipes/RecipeIngredient.cs:37-40`) carries an XML doc:

> *"Gram amount of this product used across the whole recipe, not per serving."*

This means: when a user creates a recipe for 4 portions with "150g chicken", those 150g are for all 4 portions (37.5g/portion). The PRD recommendation in Open Question 4 was "gramatury per-cały-przepis (intuicyjne dla meal prep)" — the implementation follows this.

**Impact on testing**: The oracle formula is:
```
ingredient_macro = per100g_value × (wholeRecipeGrams / 100)
recipe_total = Σ ingredient_macro
per_serving = recipe_total / recipe.Portions
meal_entry_macro = per_serving × entry.Portions
day_total = Σ meal_entry_macro
```

This is exactly what `MacroCalculator` implements (file: `src/Jadlify.Domain/Nutrition/MacroCalculator.cs:9-59`).

### 2. Calculation Chain — MacroCalculator (Domain layer)

**Location**: `src/Jadlify.Domain/Nutrition/MacroCalculator.cs`

The calculator is a static class with five methods forming a chain:

| Method | Formula | Line |
|---|---|---|
| `ForProductAmount(product, amount)` | `product.Per100Grams.Scale(amount / 100)` | 11-17 |
| `RecipeTotal(recipe)` | `Σ ingredient.Per100Grams.Scale(ingredient.WholeRecipeAmount / 100)` | 19-31 |
| `RecipePerServing(recipe)` | `RecipeTotal(recipe).Scale(1 / recipe.Portions)` | 33-38 |
| `ForMealEntry(entry, recipe)` | `RecipePerServing(recipe).Scale(entry.Portions)` | 40-45 |
| `DayTotal(entries)` | `Σ ForMealEntry(entry, recipe)` | 47-59 |

**Key observations**:
- All arithmetic is `decimal` — no `double`/`float`, no rounding. This guarantees determinism for the same inputs.
- `MacroNutrients.Scale(factor)` multiplies all four fields (Calories, Protein, Fat, Carbs) by the factor (file: `src/Jadlify.Domain/Nutrition/MacroNutrients.cs:28-29`).
- `MacroNutrients` is a sealed record — value equality comes for free.
- `NutrientBasisGrams` is `100m` (constant, line 9).
- The `+` operator on `MacroNutrients` simply adds corresponding fields (line 31-36).

**Potential precision concern**: `RecipePerServing` divides by `recipe.Portions` (int), then `ForMealEntry` multiplies by `entry.Portions` (int). When `recipe.Portions` doesn't divide evenly into the total, the intermediate per-serving result has a repeating decimal. `decimal` handles this with 28-29 significant digits, so precision is effectively exact, but the important test case is: a recipe for 3 portions × entry for 2 portions. This exercises the non-trivial division.

### 3. Shopping List Aggregation — ShoppingListCalculator (Domain layer)

**Location**: `src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs`

The calculator operates on `(MealPlanEntry, Recipe)` tuples:

1. Iterates all entries and their recipe ingredients.
2. Scales each ingredient: `ingredient.WholeRecipeAmount.Value × (entry.Portions / recipe.Portions)`.
3. Aggregates by `ingredient.ProductId` (Guid) in a `Dictionary<Guid, ShoppingListItemAccumulator>`.
4. Returns items sorted by `ProductName` (case-insensitive), then by `ProductId`.

**Aggregation key**: `ProductId` (Guid), not name. This is correct per Risk #2.

**Portion factor**: `entry.Portions / (decimal)recipe.Portions` — note the cast to `decimal` for recipe portions. The `entry.Portions` is `int`, so the implicit conversion to `decimal` happens on the left operand of `/`. This is safe.

**ShoppingListItem**: a simple record `(Guid ProductId, string ProductName, decimal Grams)`.

### 4. Application Layer Orchestration

**DailyMacroSummary**: `src/Jadlify.Application/Planning/DailyMacroSummary/GetDailyMacroSummaryQueryHandler.cs`
- Fetches entries by date, resolves recipes by IDs, calls `MacroCalculator.DayTotal` and per-entry `ForMealEntry`.
- Missing recipes produce `MacroNutrients.Zero` (graceful degradation, not error).
- Tests in `DailyMacroSummaryHandlerTests.cs` cover: multi-entry, empty day, missing recipe, no goal.

**GetShoppingList**: `src/Jadlify.Application/Shopping/GetShoppingList/GetShoppingListQueryHandler.cs`
- Same fetch-resolve pattern; delegates to `ShoppingListCalculator.ForMealEntries`.
- Missing recipes produce a `ShoppingListWarningDto` (entry skipped, warning returned).
- Tests in `GetShoppingListQueryHandlerTests.cs` cover: empty day, aggregation, sorting, missing recipe, date filtering, snapshot names.

### 5. Existing Test Coverage — Gap Analysis

#### MacroCalculatorTests (Domain)

| Test name | What it proves | Gap |
|---|---|---|
| `ForProductAmount_ScalesValuesProportionallyToGrams` | 200 kcal×(150/100) = 300 ✓ | Expected value derived from implementation formula — no independent oracle |
| `ForProductAmount_AtHundredGrams_ReturnsPer100gValues` | Identity case (100g = per100g) | OK, trivial |
| `RecipeTotal_SumsWholeRecipeIngredients` | Sum of two ingredients | No fractional grams, no independent oracle |
| `RecipePerServing_DividesTotalByPortions` | Total / 4 | Clean division — no remainder case |
| `ForMealEntry_ScalesPerServingBySelectedPortions` | Per-serving × 2 | Only one recipe, one entry |
| `DayTotal_SumsMultipleMealEntries` | breakfast (1 portion) + lunch (2 portions) | Same recipe used twice — no cross-recipe test |
| `DayTotal_ReturnsZero_ForEmptyInput` | Empty = Zero | OK |
| `Calculation_IsRepeatable_ForTheSameInputs` | f(x) = f(x) | Necessary but insufficient for determinism — doesn't prove correctness |
| `RecipeTotal_UsesIngredientSnapshots_NotCurrentProductValues` | Snapshot isolation | Good — validates a real risk |

**Missing coverage**:
- **Independent oracle**: Test that computes expected values from first principles (manual arithmetic on per-100g values × grams), not from the same formula the code uses.
- **Fractional grams**: e.g., 33.3g of a product, or a recipe with 3 portions where total doesn't divide evenly.
- **Multiple different recipes in DayTotal**: Currently only tests the same recipe with different portion counts.
- **Per-whole-recipe convention explicit test**: No test explicitly names "this is whole-recipe grams, not per-portion".

#### ShoppingListCalculatorTests (Domain)

| Test name | What it proves | Gap |
|---|---|---|
| `ForMealEntries_ReturnsEmptyList_ForEmptyInput` | Empty = empty | OK |
| `ForMealEntries_ScalesWholeRecipeAmountByPlannedPortions` | 400g / 4 × 2 = 200g | Good, single-product |
| `ForMealEntries_SumsDuplicateProductsAcrossRecipesAndEntries` | Same ProductId across recipes aggregated | Good — covers Risk #2 core scenario |
| `ForMealEntries_DoesNotRoundBeforeReturning` | 100/3 × 2 = 66.666...m | Good fractional case |
| `ForMealEntries_ReturnsItemsSortedByNameThenProductId` | Sort order | OK |
| `ForMealEntries_UsesIngredientSnapshotName` | Snapshot isolation | Good |

**Missing coverage**:
- **Same name, different ProductId**: No test asserts that two products named identically but with different IDs remain separate entries.
- **Same ProductId but different snapshoted names**: First-seen name wins in current implementation — untested edge case.
- **Independent oracle for aggregation math**: Expected values (350m) are hand-computed from the formula, not from an independently-coded reference.

#### Application-Layer Tests

Both `DailyMacroSummaryHandlerTests` and `GetShoppingListQueryHandlerTests` exercise the full chain through fake repositories but share the same gap: expected values are not independently verified.

### 6. Type System and Precision

- **GramAmount**: `decimal` value, non-zero-positive guard (`ArgumentOutOfRangeException.ThrowIfNegativeOrZero`). Cannot represent 0g or negative grams.
- **MacroNutrients**: `decimal` fields, non-negative guards. Records (value equality).
- **MealPlanEntry.Portions**: `int`, positive. Cast to `decimal` for division.
- **Recipe.Portions**: `int`, positive.
- **ShoppingListItemAccumulator.Grams**: `decimal`, mutable accumulator.

All types use `decimal` — no `double`/`float` anywhere in the calculation chain. This eliminates floating-point non-determinism entirely. The only theoretical edge case is `decimal` overflow (28-29 digits), which is unreachable for realistic food quantities.

### 7. RecipeIngredient Snapshot Model

Recipes store **snapshots** of product data at creation/update time:
- `RecipeIngredient.ProductName` — snapshot of product name
- `RecipeIngredient.Per100Grams` — snapshot of product's MacroNutrients
- `RecipeIngredient.WholeRecipeAmount` — user-specified grams for the whole recipe

This means: if a product's macro values change after a recipe is created, the recipe keeps the old values until explicitly updated. This is by design (tested by `RecipeTotal_UsesIngredientSnapshots_NotCurrentProductValues`). The shopping list uses the same snapshots.

## Code References

- `src/Jadlify.Domain/Nutrition/MacroCalculator.cs:1-61` — Full macro calculation chain
- `src/Jadlify.Domain/Nutrition/MacroNutrients.cs:1-38` — Value object with Scale and + operator
- `src/Jadlify.Domain/Nutrition/GramAmount.cs:1-16` — Positive-decimal gram wrapper
- `src/Jadlify.Domain/Recipes/Recipe.cs:1-96` — Recipe entity with Portions and Ingredients
- `src/Jadlify.Domain/Recipes/RecipeIngredient.cs:37-40` — WholeRecipeAmount XML doc (gramature convention)
- `src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs:1-65` — Aggregation by ProductId
- `src/Jadlify.Domain/Shopping/ShoppingListItem.cs:1-4` — Simple output record
- `src/Jadlify.Domain/Planning/MealPlanEntry.cs:1-39` — Entry with int Portions
- `src/Jadlify.Application/Planning/DailyMacroSummary/GetDailyMacroSummaryQueryHandler.cs:1-68` — Application orchestrator
- `src/Jadlify.Application/Shopping/GetShoppingList/GetShoppingListQueryHandler.cs:1-59` — Shopping list orchestrator
- `tests/Jadlify.Domain.Tests/Nutrition/MacroCalculatorTests.cs:1-186` — Existing macro tests (12 tests)
- `tests/Jadlify.Domain.Tests/Shopping/ShoppingListCalculatorTests.cs:1-126` — Existing shopping tests (6 tests)
- `tests/Jadlify.Application.Tests/Planning/DailyMacroSummaryHandlerTests.cs:1-154` — Application macro tests (4 tests)
- `tests/Jadlify.Application.Tests/Shopping/GetShoppingListQueryHandlerTests.cs:1-173` — Application shopping tests (6 tests)

## Architecture Insights

1. **Clean separation**: Domain calculators are pure static functions; Application handlers orchestrate I/O (repo calls) and delegate calculation to domain. This makes unit-testing the domain oracle-style possible without any mocks.

2. **Snapshot model prevents temporal drift**: RecipeIngredient stores product values at recipe-creation time. Tests already verify this. This means the macro determinism guarantee is local to the recipe — it doesn't depend on the current state of the product catalog.

3. **Aggregation is domain-pure**: `ShoppingListCalculator` is a pure function from `(MealPlanEntry, Recipe)[]` → `ShoppingListItem[]`. The Application handler only adds the "missing recipe" warning concern.

4. **Portion convention is implicit but consistent**: The code uses `WholeRecipeAmount` everywhere (naming is explicit), but no test's _name_ or _comment_ explicitly calls out the per-whole-recipe convention. A well-named test would make this convention visible to future contributors.

## Historical Context (from prior changes)

- `context/archive/2026-06-04-daily-macro-summary/plan.md` — Introduced the `MacroCalculator.DayTotal` and `GetDailyMacroSummaryQueryHandler`. This is where the calculation chain was first assembled end-to-end.
- `context/archive/2026-06-06-shopping-list-from-day-plan/plan.md` — Introduced `ShoppingListCalculator` with the `ProductId` aggregation key and `ShoppingListItemAccumulator` pattern.
- `context/archive/2026-05-31-recipe-builder-with-macro-calculation/` — Where `RecipeIngredient.WholeRecipeAmount` and the per-whole-recipe convention were established.

## Open Questions

1. **Independent oracle implementation**: Should the oracle be a separate static method in the test project (e.g., `TestMacroOracle.ComputeExpected(...)`) or just inline arithmetic per test case? A separate oracle method risks becoming its own bug source; inline arithmetic per test case with comments is more transparent but repetitive. **Recommendation**: inline arithmetic with explicit comments naming the formula, because the whole point is that the oracle must not share code with the implementation.

2. **Rounding policy for presentation**: The Domain layer does not round at all — `decimal` carries full precision. The PRD doesn't specify a rounding rule for display. Should Phase 1 tests assert that the domain never rounds (current behavior) and leave rounding to the presentation layer? **Recommendation**: yes — test that domain outputs are unrounded, and add a note for Phase 3 (e2e) that the frontend may round for display.

3. **Name collision in shopping list**: When two products share the same `ProductId`, the first-seen `ProductName` wins (the accumulator doesn't update `ProductName` on `Add()`). This is fine because `ProductId` uniqueness guarantees the same product, but if the user updates a product name between recipe-creation events, two ingredients with the same `ProductId` might carry different snapshot names. The shopping list will show the first-encountered name. Is this worth testing? **Recommendation**: yes — a quick assertion that the first-seen name is used, so the behavior is documented, even if it's acceptable.

4. **MealPlanEntry.Portions vs Recipe.Portions types**: `MealPlanEntry.Portions` is `int`, `Recipe.Portions` is `int`. `ShoppingListCalculator` divides them as `entry.Portions / (decimal)recipe.Portions`. Should tests exercise the case where `entry.Portions > recipe.Portions` (user takes more portions than the recipe yields)? This is a valid scenario (cook the recipe twice). **Recommendation**: yes — it exercises the scaling factor > 1.
