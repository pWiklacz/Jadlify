# Shopping List From Day Plan Implementation Plan

## Overview

Implement roadmap slice S-06: a signed-in user can open the shopping-list screen, choose one date, and see a deduplicated list of products required by that day's meal plan. The list is a read-only projection computed from meal-plan entries and recipe ingredient snapshots; it is not persisted as a separate shopping-list record.

## Current State Analysis

Jadlify already has the planning and calculation foundation this slice needs:

- The roadmap identifies S-06 as the north-star slice that closes the daily meal-plan, macro-summary, and shopping-list flow (`context/foundation/roadmap.md:24`, `context/foundation/roadmap.md:38`).
- Meal-plan entries can be listed by selected date through the owner-scoped `IMealPlanRepository.ListByDateAsync` (`src/Jadlify.Application/Planning/IMealPlanRepository.cs:17`).
- Recipes can be batch-loaded with ingredients through the owner-scoped `IRecipeRepository.ListByIdsWithIngredientsAsync` (`src/Jadlify.Application/Recipes/IRecipeRepository.cs:34`).
- Recipe ingredients snapshot `ProductId`, `ProductName`, and `WholeRecipeAmount`; product edits/deletes after recipe creation must not change recipe-derived calculations (`docs/reference/contract-surfaces.md:38`).
- Quantities are grams-only for MVP, and `WholeRecipeAmount` is the whole-recipe quantity, not per serving (`docs/reference/contract-surfaces.md:20`, `src/Jadlify.Domain/Recipes/RecipeIngredient.cs:40`).
- S-05 already established the read-only projection pattern under planning: load entries, batch-load ingredient-rich recipes, compute results on read, and avoid persisted derived totals (`src/Jadlify.Application/Planning/DailyMacroSummary/GetDailyMacroSummaryQueryHandler.cs`, `docs/reference/contract-surfaces.md:56`).
- The protected SPA already routes `/shopping-list`, but the page is still a placeholder (`src/Jadlify.Web/src/App.tsx:27`, `src/Jadlify.Web/src/routes/sections/ShoppingListPage.tsx:3`).

### Key Discoveries

- Shopping-list aggregation should reuse recipe ingredient snapshots, not current product rows. This preserves the existing recipe snapshot contract and avoids new product reads.
- The correct shopping quantity is proportional to planned portions: `ingredient.WholeRecipeAmount * entry.Portions / recipe.Portions`. This mirrors `MacroCalculator.ForMealEntry`, which scales recipe-per-serving values by planned portions (`src/Jadlify.Domain/Nutrition/MacroCalculator.cs:40`).
- A missing recipe is not expected because recipe deletion is blocked while meal-plan entries reference it, but S-05 treats defensive missing recipes as zero contribution. S-06 should keep the list usable while surfacing a warning.
- No schema change is needed. Persisting generated lists would introduce stale-snapshot and refresh semantics that are out of MVP scope.

## Desired End State

On `/shopping-list`, the user selects a date and sees the shopping list for that day. Each product appears once, sorted A-Z by product name, with a summed gram amount formatted to one decimal place. If the selected day has no planned entries, the page shows an empty-state message and a link to `/meal-plan`.

The API exposes a read-only `GET /api/shopping-list?date=yyyy-MM-dd` endpoint. It returns the selected date, an array of aggregated items, and an optional warnings array for defensive data gaps such as a meal-plan entry whose recipe is not returned by the owner-scoped recipe read. Anonymous access returns `401`, and users only see shopping quantities derived from their own meal-plan entries and recipes.

## What We're NOT Doing

- Not persisting shopping lists, generated snapshots, checked-state, or shopping sessions.
- Not adding export to file, multi-day or weekly shopping lists, pantry/no-waste behavior, product categories, or store aisles.
- Not adding non-gram units; grams-only remains the MVP contract.
- Not showing per-recipe source breakdown for each item.
- Not changing meal-plan, recipe, product, or daily-macro-summary contracts beyond adding the new shopping-list API surface and contract registry entry.
- Not adding shopping-list output to `/meal-plan`; the accepted MVP surface is the existing `/shopping-list` route.

## Implementation Approach

Follow the existing vertical-slice style. Add a deterministic domain/application aggregation surface that scales recipe ingredient snapshots by planned portions, groups by ingredient `ProductId`, keeps the snapshot product name, and returns sorted read models. Add a query handler that composes owner-scoped meal-plan entries and recipes with ingredients. Expose the read model through `GET /api/shopping-list?date=`. Replace the placeholder React page with a date selector and read-only shopping-list view using the authenticated API client and React Query.

## Critical Implementation Details

### Shopping Quantity Scaling

The aggregation must scale the whole-recipe ingredient amount by the planned portion fraction: `WholeRecipeAmount * entry.Portions / recipe.Portions`. Do not use the full recipe amount for every entry, and do not round before summing across entries.

### Snapshot Identity

Aggregate by `RecipeIngredient.ProductId` and display the snapshot `ProductName`. This intentionally avoids loading current product rows, so product edits after a recipe snapshot do not retroactively change the list.

### Defensive Missing Recipes

Missing recipes should not make the endpoint fail. They should contribute no items and produce a warning in the response so the UI can tell the user that the list may be incomplete.

## Phase 1: Domain and Application Shopping Projection

### Overview

Add the deterministic shopping-list aggregation and application query read model. No HTTP endpoint or React UI yet.

### Changes Required

#### 1. Shopping aggregation surface

**File**: `src/Jadlify.Domain/Shopping/ShoppingListCalculator.cs` (new) or an equivalent domain-owned calculator file

**Intent**: Keep the quantity scaling and product aggregation rule in one deterministic place, close to existing domain calculation logic.

**Contract**: Accept planned `(MealPlanEntry entry, Recipe recipe)` pairs and return aggregated items keyed by ingredient product id. Quantity is calculated from `WholeRecipeAmount * entry.Portions / recipe.Portions`, summed as `decimal`, and never rounded before returning. Empty input returns an empty list.

#### 2. Shopping read models

**File**: `src/Jadlify.Application/Shopping/ShoppingListDto.cs` (new), `src/Jadlify.Application/Shopping/ShoppingListItemDto.cs` (new), `src/Jadlify.Application/Shopping/ShoppingListWarningDto.cs` (new)

**Intent**: Define the application read model for the generated day list.

**Contract**:
- `ShoppingListDto(DateOnly Date, IReadOnlyList<ShoppingListItemDto> Items, IReadOnlyList<ShoppingListWarningDto> Warnings)`.
- `ShoppingListItemDto(Guid ProductId, string ProductName, decimal Grams)`.
- `ShoppingListWarningDto(Guid EntryId, Guid RecipeId, string Message)` for defensive missing-recipe cases.

#### 3. Query and handler

**File**: `src/Jadlify.Application/Shopping/GetShoppingList/GetShoppingListQuery.cs` (new), `src/Jadlify.Application/Shopping/GetShoppingList/GetShoppingListQueryHandler.cs` (new)

**Intent**: Compose owner-scoped meal-plan entries and ingredient-rich recipes into the shopping-list read model.

**Contract**: `GetShoppingListQuery(DateOnly Date) : IQuery<ShoppingListDto>`. Handler dependencies are `IMealPlanRepository` and `IRecipeRepository`. Steps: list entries by date; batch-load distinct recipe ids with ingredients; aggregate entries whose recipe is present; add warnings for missing recipes; sort items A-Z by product name, then by product id for deterministic tie-breaking; return `Result.Ok(dto)`. Empty day returns success with no items and no warnings.

#### 4. Unit tests and fakes

**File**: `tests/Jadlify.Domain.Tests/Shopping/ShoppingListCalculatorTests.cs` (new), `tests/Jadlify.Application.Tests/Shopping/GetShoppingListQueryHandlerTests.cs` (new), plus existing fake repositories if needed

**Intent**: Prove scaling, aggregation, empty-day behavior, sorting, and warning semantics without HTTP or UI.

**Contract**: Tests cover duplicate products across recipes, duplicate entries for the same recipe, portion scaling against multi-portion recipes, empty input, deterministic A-Z sorting, and missing recipe warning. Existing `FakeMealPlanRepository` and `FakeRecipeRepository` can be reused because they already model the relevant ports.

### Success Criteria

#### Automated Verification

- Build passes: `pwsh ./.scripts/build-min.ps1`
- Domain tests pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.Domain.Tests -Class ShoppingListCalculatorTests`
- Application tests pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.Application.Tests -FullyQualifiedNameContains Shopping`
- Format passes: `pwsh ./.scripts/format-min.ps1`

#### Manual Verification

- A hand calculation for a recipe with 4 portions and a 2-portion meal-plan entry matches the handler test result.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation before proceeding to Phase 2.

---

## Phase 2: Shopping List API Endpoint and Integration Tests

### Overview

Expose the application projection through an authenticated API endpoint and test the HTTP behavior, including user isolation.

### Changes Required

#### 1. API response contracts

**File**: `src/Jadlify.API/Shopping/ShoppingListContracts.cs` (new)

**Intent**: Map application DTOs to the wire response consumed by React.

**Contract**: `ShoppingListResponse(DateOnly Date, IReadOnlyList<ShoppingListItemResponse> Items, IReadOnlyList<ShoppingListWarningResponse> Warnings)` with `FromDto`. Item response exposes `productId`, `productName`, and `grams`. Warning response exposes `entryId`, `recipeId`, and `message`.

#### 2. API endpoint group

**File**: `src/Jadlify.API/Shopping/ShoppingListEndpoints.cs` (new), `src/Jadlify.API/Program.cs`

**Intent**: Add a protected read-only endpoint for the shopping-list route.

**Contract**: Map `GET /api/shopping-list?date=yyyy-MM-dd` through the mediator to `GetShoppingListQuery`. Return `200 OK` with `ShoppingListResponse` on success or `result.ToProblem()` on failure. The route inherits the global authenticated fallback policy; do not use `AllowAnonymous`.

#### 3. API integration tests

**File**: `tests/Jadlify.API.Tests/Shopping/ShoppingListEndpointsTests.cs` (new)

**Intent**: Prove the full API stack returns the right shape and enforces owner scope.

**Contract**: Tests cover anonymous `401`, empty day success, duplicate product aggregation across multiple recipes and entries, proportional scaling for recipe portions, A-Z sorting, and cross-user isolation. Use the existing API test factory and existing product/recipe/meal-plan setup helpers or local equivalents.

### Success Criteria

#### Automated Verification

- Build passes: `pwsh ./.scripts/build-min.ps1`
- API tests pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.API.Tests -FullyQualifiedNameContains ShoppingList`
- Existing planning API tests still pass: `pwsh ./.scripts/test-min.ps1 -Project Jadlify.API.Tests -FullyQualifiedNameContains MealPlan`
- Format passes: `pwsh ./.scripts/format-min.ps1`

#### Manual Verification

- `GET /api/shopping-list?date=<planned-date>` returns one row per product with summed grams and no duplicated products.
- A request without authentication returns `401`.

**Implementation Note**: Pause for manual confirmation before Phase 3.

---

## Phase 3: React Shopping List Page

### Overview

Replace the `/shopping-list` placeholder with a usable read-only shopping-list page: date selector, loading/error states, empty state, warnings, and A-Z item list.

### Changes Required

#### 1. Shopping-list types and hook

**File**: `src/Jadlify.Web/src/shopping/types.ts` (new), `src/Jadlify.Web/src/shopping/useShoppingList.ts` (new)

**Intent**: Type and fetch the new shopping-list API response.

**Contract**: Types mirror the API response: `ShoppingList`, `ShoppingListItem`, `ShoppingListWarning`. Hook uses query key `['shopping-list', date]`, calls `GET /api/shopping-list?date=...`, and is enabled only when a session and date are present.

#### 2. Real shopping-list page

**File**: `src/Jadlify.Web/src/shopping/ShoppingListPage.tsx` (new or move from route section), `src/Jadlify.Web/src/routes/sections/ShoppingListPage.tsx`

**Intent**: Render the selected day's generated list.

**Contract**: Page owns a date state defaulting to today's ISO date, renders a date input, fetches the list, shows loading and error messages consistent with existing pages, shows warnings when present, shows an empty state with a link to `/meal-plan`, and renders items sorted as returned by the API. Display each row as product name plus `grams` formatted to one decimal place and suffixed with `g`.

#### 3. Refresh behavior after navigation

**File**: `src/Jadlify.Web/src/shopping/useShoppingList.ts`, optionally `src/Jadlify.Web/src/planning/useMealPlanMutations.ts`

**Intent**: Keep the page simple and correct with compute-on-read semantics.

**Contract**: React Query refetches the shopping-list page when the selected date changes or the page remounts. If the implementation shares invalidation from meal-plan mutations, invalidate the shopping-list key for the affected date as an additive improvement, but do not add cross-page state coupling that makes `/meal-plan` own shopping-list behavior.

#### 4. Frontend tests

**File**: `src/Jadlify.Web/src/shopping/ShoppingListPage.test.tsx` (new)

**Intent**: Prove the page calls the API and renders the expected states.

**Contract**: Tests cover date-based fetch, item list rendering with one-decimal gram formatting, empty state linking to `/meal-plan`, warning rendering, loading/error states, and A-Z display order from the mocked response.

### Success Criteria

#### Automated Verification

- Frontend lint passes: `npm run lint` from `src/Jadlify.Web`
- Frontend tests pass: `npm test` from `src/Jadlify.Web`
- Frontend build passes: `npm run build` from `src/Jadlify.Web`

#### Manual Verification

- `/shopping-list` shows generated items for a date with a plan.
- The same product used in multiple recipes appears once with summed grams.
- A day without entries shows the empty state and link to `/meal-plan`.
- The page remains readable on a narrow mobile viewport.

**Implementation Note**: Pause for manual confirmation before Phase 4.

---

## Phase 4: Contract Registry and Final Verification

### Overview

Record the new S-06 contract and run the narrow end-to-end verification gates.

### Changes Required

#### 1. Contract registry update

**File**: `docs/reference/contract-surfaces.md`

**Intent**: Make the shopping-list read model load-bearing for future slices and prevent duplicate aggregation paths.

**Contract**: Add "Shopping List Contracts (S-06)" documenting `GET /api/shopping-list?date=yyyy-MM-dd`, compute-on-read behavior, grams-only output, aggregation by ingredient snapshot `ProductId`, proportional scaling by `entry.Portions / recipe.Portions`, no persistence, warning semantics for defensive missing recipes, and the dedicated `/shopping-list` page.

#### 2. Roadmap status note

**File**: `context/foundation/roadmap.md`

**Intent**: Leave a small status/handoff note only if implementation completion policy for this repo expects roadmap updates during the change.

**Contract**: Do not mark S-06 done until implementation is actually complete and verified. If touched, keep the update scoped to S-06.

#### 3. Final verification

**File**: no source file

**Intent**: Verify the backend and frontend surfaces that changed.

**Contract**: Run the narrowest relevant backend script first, then frontend lint/test/build. Use `verify-min.ps1` only when the implementation has touched enough backend layers that the full backend gate is justified.

### Success Criteria

#### Automated Verification

- Backend verify passes: `pwsh ./.scripts/verify-min.ps1`
- Frontend lint passes: `npm run lint` from `src/Jadlify.Web`
- Frontend tests pass: `npm test` from `src/Jadlify.Web`
- Frontend build passes: `npm run build` from `src/Jadlify.Web`

#### Manual Verification

- End-to-end local smoke: create products, create recipes sharing at least one product, plan them for one day with different portions, open `/shopping-list`, and confirm the list matches a hand calculation.
- Confirm another signed-in user does not see the first user's planned ingredients.
- Confirm `docs/reference/contract-surfaces.md` describes the shipped S-06 endpoint and aggregation semantics.

---

## Testing Strategy

### Unit Tests

- Domain aggregation: duplicate product aggregation, proportional portion scaling, empty input, deterministic sort.
- Application handler: empty day, duplicate products across entries, missing recipe warning, owner-scoped repository behavior through existing fakes.

### Integration Tests

- API endpoint: `401` anonymous, `200` empty day, correct aggregation, sorting, warning serialization, cross-user isolation.

### Frontend Tests

- `/shopping-list`: date query, loading/error/empty states, item rendering with one-decimal grams, warning display, link to `/meal-plan`.

### Manual Testing Steps

1. Sign in and create two products.
2. Create two recipes that share one product and have more than one recipe portion.
3. Add both recipes to the same meal-plan date with different planned portions.
4. Open `/shopping-list` for that date.
5. Verify the shared product appears once and its grams equal the sum of scaled whole-recipe ingredient amounts.
6. Switch to an empty date and verify the empty state plus `/meal-plan` link.

## Performance Considerations

The endpoint performs the same bounded reads as S-05: one owner-scoped meal-plan entry read for a date and one owner-scoped recipe-with-ingredients batch read for distinct recipe ids. Aggregation is in-memory over a single day's entries and recipe ingredients, which is within the PRD's small MVP scale and `< 800 ms p95` target.

## Migration Notes

No database migration is planned. The list is computed on read from existing meal-plan entries and recipe ingredient snapshots.

## References

- Roadmap S-06: `context/foundation/roadmap.md:178`
- PRD FR-015/FR-016: `context/foundation/prd.md`
- Contract registry grams/snapshot rules: `docs/reference/contract-surfaces.md:20`, `docs/reference/contract-surfaces.md:38`, `docs/reference/contract-surfaces.md:52`
- Existing S-05 read projection: `src/Jadlify.Application/Planning/DailyMacroSummary/GetDailyMacroSummaryQueryHandler.cs`
- Existing shopping route placeholder: `src/Jadlify.Web/src/routes/sections/ShoppingListPage.tsx:3`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` - <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Domain and Application Shopping Projection

#### Automated

- [x] 1.1 Build passes: `pwsh ./.scripts/build-min.ps1` — 753be5a
- [x] 1.2 Domain shopping calculator tests pass — 753be5a
- [x] 1.3 Application shopping query handler tests pass — 753be5a
- [x] 1.4 Format passes: `pwsh ./.scripts/format-min.ps1` — 753be5a

#### Manual

- [x] 1.5 Handler test numbers match a hand calculation for multi-portion recipes — 753be5a

### Phase 2: Shopping List API Endpoint and Integration Tests

#### Automated

- [x] 2.1 Build passes: `pwsh ./.scripts/build-min.ps1` — d040df0
- [x] 2.2 Shopping-list API tests pass — d040df0
- [x] 2.3 Existing meal-plan API tests still pass — d040df0
- [x] 2.4 Format passes: `pwsh ./.scripts/format-min.ps1` — d040df0

#### Manual

- [x] 2.5 `GET /api/shopping-list?date=` returns expected JSON locally — d040df0
- [x] 2.6 Anonymous request returns `401` — d040df0

### Phase 3: React Shopping List Page

#### Automated

- [x] 3.1 Frontend lint passes: `npm run lint` — d88f8f3
- [x] 3.2 Frontend tests pass: `npm test` — d88f8f3
- [x] 3.3 Frontend build passes: `npm run build` — d88f8f3

#### Manual

- [x] 3.4 `/shopping-list` shows generated items for a planned date — d88f8f3
- [x] 3.5 Duplicate products appear once with summed grams — d88f8f3
- [x] 3.6 Empty date shows the empty state and `/meal-plan` link — d88f8f3
- [x] 3.7 Layout is readable on a narrow mobile viewport — d88f8f3

### Phase 4: Contract Registry and Final Verification

#### Automated

- [x] 4.1 Backend verify passes: `pwsh ./.scripts/verify-min.ps1` — e99164a
- [x] 4.2 Frontend lint passes: `npm run lint` — e99164a
- [x] 4.3 Frontend tests pass: `npm test` — e99164a
- [x] 4.4 Frontend build passes: `npm run build` — e99164a

#### Manual

- [x] 4.5 End-to-end local smoke matches a hand calculation — e99164a
- [x] 4.6 Cross-user shopping-list isolation is confirmed — e99164a
- [x] 4.7 Contract registry S-06 section matches the shipped endpoint — e99164a
