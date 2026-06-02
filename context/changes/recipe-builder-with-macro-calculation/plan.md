# Recipe Builder With Macro Calculation Implementation Plan

## Overview

Implement roadmap slice S-03: a signed-in user can create, review, edit, and delete recipes made from their own products, with deterministic macro totals for the whole recipe and per serving. The recipe builder uses grams-only ingredient quantities across the whole recipe, validates every selected product against the current user, and supports adding a missing product from the recipe flow.

## Current State Analysis

The repo already has the foundations this slice should build on rather than replace. Authentication is enforced by the API fallback policy, product CRUD is implemented end-to-end, EF Core persistence exists for recipes, and the domain has preliminary recipe and macro-calculation types. What is missing is the user-visible recipe vertical: application use-cases, recipe HTTP contracts, product search for the picker, a real `/recipes` page, and tests that prove recipe macro totals and per-user isolation.

The current product-delete policy from S-02 intentionally allows deleting products and expects S-03 to snapshot product data into recipe ingredients. That contract is not yet implemented in `RecipeIngredient`, so this plan must close that gap before exposing recipes.

## Desired End State

After this plan, `/recipes` is a real protected page where a user can list recipes, open a builder, search their products, add ingredients with whole-recipe gram amounts, see live whole-recipe and per-serving macro totals, save a complete recipe, edit it later, and delete it unless it is already used by a meal-plan entry. Backend responses return deterministic totals computed from the recipe ingredient snapshots, so existing recipes remain calculable even if an original product is edited or deleted later.

### Key Discoveries:

- `MacroCalculator` is the existing deterministic macro surface and already supports product amount, recipe total, per-serving, and meal-entry calculations in one place (`src/Jadlify.Domain/Nutrition/MacroCalculator.cs:7`).
- `Recipe` and `RecipeIngredient` already model portions and whole-recipe ingredient grams, but ingredients currently only carry `ProductId` and `WholeRecipeAmount` (`src/Jadlify.Domain/Recipes/Recipe.cs:3`, `src/Jadlify.Domain/Recipes/RecipeIngredient.cs:5`).
- Product repository access is owner-scoped and already has `ListByIdsAsync`, which is the right boundary for validating selected ingredients (`src/Jadlify.Application/Products/IProductRepository.cs:11`, `src/Jadlify.Application/Products/IProductRepository.cs:19`).
- Recipe repository operations are owner-scoped, but update currently documents metadata-only reconciliation, which is too narrow for this slice (`src/Jadlify.Application/Recipes/IRecipeRepository.cs:11`, `src/Jadlify.Application/Recipes/IRecipeRepository.cs:24`).
- Product Minimal API endpoints and frontend product hooks provide the pattern for recipe endpoints, `Result` mapping, react-query invalidation, and authenticated API calls (`src/Jadlify.API/Products/ProductEndpoints.cs:22`, `src/Jadlify.Web/src/products/useProducts.ts:13`).
- `/recipes` currently renders only a placeholder, so the frontend work is a replacement of that route surface (`src/Jadlify.Web/src/routes/sections/RecipesPage.tsx:3`).

## What We're NOT Doing

- No meal-plan, daily-goal, daily-summary, or shopping-list behavior in this change.
- No non-gram units, unit conversions, ml, pieces, or package-based ingredient quantities.
- No public recipes, sharing, tags, images, instructions, cooking steps, or AI generation.
- No barcode lookup changes beyond reusing the existing product add flow from inside the recipe builder.
- No persisted precomputed recipe totals; totals are derived deterministically from ingredient snapshots and grams.
- No direct browser-to-database access; all recipe and product behavior stays behind the ASP.NET Core API.

## Implementation Approach

Build the vertical slice bottom-up. First close the domain/application contract gap by making recipe ingredients carry the product snapshot needed for historical macro stability. Then expose owner-scoped commands and queries through Minimal API endpoints. Finally replace the recipes placeholder with a focused React builder that reuses the existing product catalog contracts and add-product modal where practical.

The selected behavior from planning is:

- recipes use existing user products as the source at ingredient-add time;
- each ingredient quantity is grams for the whole recipe;
- recipe `portions` drives per-serving totals;
- the UI recalculates macro preview live;
- only complete recipes can be saved;
- invalid, missing, or cross-user products reject the save;
- the picker supports advanced product search and lets the user add a missing product without leaving the recipe flow;
- tests cover the full vertical slice and per-user isolation at every layer.

## Critical Implementation Details

### Historical Product Snapshot

S-02 changed product deletion to "keep historical" and left comments in persistence expecting S-03 to snapshot product name and per-100g macros into `RecipeIngredient`. This is now load-bearing: recipe macro totals must not require the original product row to still exist, and editing a product later must not silently change an existing recipe's totals.

### Quantity Semantics

`RecipeIngredient.WholeRecipeAmount` is the amount used across the whole recipe, not per serving. UI copy and API contracts must keep this explicit so "150g chicken in a 4-portion recipe" means 37.5g per serving, not 600g total.

## Phase 1: Domain And Application Recipe Contracts

### Overview

This phase turns the existing recipe skeleton into a complete use-case contract: snapshot-capable ingredients, deterministic recipe totals, recipe DTOs, validators, and application handlers for list/get/create/update/delete.

### Changes Required:

#### 1. Recipe Ingredient Snapshot Model

**File**: `src/Jadlify.Domain/Recipes/RecipeIngredient.cs`

**Intent**: Extend recipe ingredients so each ingredient stores the selected product id, product display name, product per-100g macro snapshot, and whole-recipe gram amount. This satisfies the S-02 deletion policy and makes recipe totals stable after product edits/deletes.

**Contract**: `RecipeIngredient` must expose `ProductId`, `ProductName`, `Per100Grams`, and `WholeRecipeAmount`; construction rejects blank product names, missing macros, and non-positive gram amounts.

#### 2. Recipe Aggregate Editing

**File**: `src/Jadlify.Domain/Recipes/Recipe.cs`

**Intent**: Support creating and replacing a complete recipe composition so edit can change name, portions, and the full ingredient set in one save.

**Contract**: `Recipe` keeps unique product ids within `Ingredients`, validates positive `Portions`, and provides an aggregate method to replace metadata and ingredients without exposing a mutable collection.

#### 3. Macro Calculator Recipe Overloads

**File**: `src/Jadlify.Domain/Nutrition/MacroCalculator.cs`

**Intent**: Calculate recipe totals directly from ingredient snapshots while preserving existing meal-plan calculation compatibility for later slices.

**Contract**: `RecipeTotal(Recipe)` and `RecipePerServing(Recipe)` become the recipe-builder surface, completely removing the `IReadOnlyDictionary<Guid, Product> productsById` dictionary parameter since ingredient values are retrieved directly from the snapshotted ingredients. Existing product-dictionary methods are removed/cleaned up, and `ForMealEntry(MealPlanEntry, Recipe)` is simplified to avoid carrying the product dictionary. All math remains `decimal` and proportional to per-100g values.

#### 4. Recipe Application DTOs

**Files**:

- `src/Jadlify.Application/Recipes/RecipeDto.cs`
- `src/Jadlify.Application/Recipes/RecipeIngredientDto.cs`
- `src/Jadlify.Application/Recipes/RecipeMacroSummaryDto.cs`

**Intent**: Define read models returned by recipe queries and API responses without leaking domain objects or EF-owned types.

**Contract**: Recipe DTOs include id, name, portions, ingredient rows, total macros, and per-serving macros. Ingredient DTOs include product id, product name snapshot, whole-recipe grams, and per-100g macro snapshot.

#### 5. Recipe Commands, Queries, Handlers, And Validators

**Files**:

- `src/Jadlify.Application/Recipes/CreateRecipe/*`
- `src/Jadlify.Application/Recipes/UpdateRecipe/*`
- `src/Jadlify.Application/Recipes/DeleteRecipe/*`
- `src/Jadlify.Application/Recipes/GetRecipe/*`
- `src/Jadlify.Application/Recipes/ListRecipes/*`
- `src/Jadlify.Application/Recipes/RecipeValidationBounds.cs`

**Intent**: Add the application use-cases that API endpoints will call through the existing mediator and validation pipeline.

**Contract**: Create/update commands accept `Name`, `Portions`, and ingredient inputs `{ ProductId, WholeRecipeGrams }`; validators reject blank names, non-positive portions, empty ingredients, duplicate product ids, and non-positive grams. Handlers use owner-scoped `IProductRepository.ListByIdsAsync` to load products, reject missing/cross-user products with validation or not-found style failure, snapshot product values into ingredients, and return/list DTOs with computed totals.

#### 6. Recipe Repository Port Update

**File**: `src/Jadlify.Application/Recipes/IRecipeRepository.cs`

**Intent**: Make the persistence contract match the actual S-03 edit behavior.

**Contract**: `UpdateAsync(Recipe recipe)` updates the complete recipe aggregate, including ingredient composition, for the current user. Delete keeps the existing conflict contract when a recipe is used by a meal-plan entry.

### Success Criteria:

#### Automated Verification:

- Domain recipe snapshot and macro tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Domain.Tests -FullyQualifiedNameContains Recipe`
- Application recipe handler and validator tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Application.Tests -FullyQualifiedNameContains Recipes`
- Narrow build passes for touched backend projects: `pwsh ./.scripts/build-min.ps1 -Project src/Jadlify.Application/Jadlify.Application.csproj`

#### Manual Verification:

- Review the application contracts and confirm they describe whole-recipe grams, not per-serving grams.
- Confirm no new application code depends on ASP.NET Core, EF Core, Supabase SDKs, or raw claims.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Persistence, Product Search, And Recipe API

### Overview

This phase persists the new snapshot fields, implements complete recipe repository update semantics, exposes recipe endpoints, and extends product listing with search parameters for the recipe picker.

### Changes Required:

#### 1. Recipe EF Mapping And Migration

**Files**:

- `src/Jadlify.Infrastructure/Persistence/Configurations/RecipeConfiguration.cs`
- `src/Jadlify.Infrastructure/Persistence/Migrations/*`
- `src/Jadlify.Infrastructure/Persistence/Migrations/JadlifyDbContextModelSnapshot.cs`

**Intent**: Persist recipe ingredient snapshots alongside existing recipe ingredient product id and grams.

**Contract**: `recipe_ingredients` gains required product-name and per-100g macro snapshot columns, with decimal precision matching product macro columns. The migration must preserve existing columns and keep the owner-scoped `recipes.user_id` model intact.

#### 2. Recipe Repository Complete Update

**File**: `src/Jadlify.Infrastructure/Persistence/Repositories/RecipeRepository.cs`

**Intent**: Make repository persistence match the complete edit contract from Phase 1.

**Contract**: `GetByIdAsync` and `ListAsync` load ingredients (using `.Include(r => r.Ingredients)`); `UpdateAsync` owner-scopes the target recipe, loads the existing recipe with its ingredients, and reconciles the owned ingredient collection (matching on `ProductId`, updating existing entries' amounts/snapshots, adding new ones, and removing missing ones) to avoid EF Core tracking exceptions. It returns `Recipe.NotFound` for missing/cross-user targets and never mutates another user's recipe.

#### 3. Product Search Contract For Picker

**Files**:

- `src/Jadlify.Application/Products/ListProducts/ListProductsQuery.cs`
- `src/Jadlify.Application/Products/ListProducts/ListProductsQueryHandler.cs`
- `src/Jadlify.Application/Products/IProductRepository.cs`
- `src/Jadlify.Infrastructure/Persistence/Repositories/ProductRepository.cs`
- `src/Jadlify.API/Products/ProductEndpoints.cs`

**Intent**: Support the recipe product picker without loading an unbounded catalog as the product table grows, and align repository documentation.

**Contract**: `GET /api/products` remains backward-compatible when called without query parameters, and accepts optional `search`, `skip`, and `take` parameters for name/barcode filtering. All filtering stays owner-scoped; default and maximum `take` values prevent unbounded picker queries. Also, update `DeleteAsync` doc comments in `IProductRepository.cs` to reflect that product deletion always succeeds (keep-historical policy) and does not block on recipe ingredient references.

#### 4. Recipe API Contracts And Endpoints

**Files**:

- `src/Jadlify.API/Recipes/RecipeContracts.cs`
- `src/Jadlify.API/Recipes/RecipeEndpoints.cs`
- `src/Jadlify.API/Program.cs`

**Intent**: Expose recipe CRUD under `/api/recipes` using the existing Minimal API + mediator + `ResultExtensions.ToProblem` pattern.

**Contract**: Endpoints:

- `GET /api/recipes` returns the current user's recipe summaries with totals.
- `GET /api/recipes/{id:guid}` returns one recipe with ingredients and totals.
- `POST /api/recipes` creates a complete recipe and returns `201` with `Location`.
- `PUT /api/recipes/{id:guid}` replaces metadata and ingredient composition and returns `204`.
- `DELETE /api/recipes/{id:guid}` returns `204`, `404`, or `409 Recipe.InUse`.

All routes inherit the existing authenticated fallback policy and must not call `AllowAnonymous`.

#### 5. Contract Surface Registry

**File**: `docs/reference/contract-surfaces.md`

**Intent**: Record the recipe contracts future meal-plan and shopping-list slices must reuse.

**Contract**: Add a short S-03 section naming `RecipeIngredient` snapshot semantics, recipe endpoint routes, recipe DTO totals, and the rule that meal-plan/shopping-list calculations must use recipe ingredient snapshots rather than reloading deleted products. Also, update the existing 'Resource Persistence and Calculation' section in `docs/reference/contract-surfaces.md` to remove the outdated references to `Product.InUse` conflict checks since product deletion always succeeds.

### Success Criteria:

#### Automated Verification:

- Infrastructure recipe repository and model tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Infrastructure.Tests -FullyQualifiedNameContains Recipe`
- API recipe endpoint tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests -FullyQualifiedNameContains Recipe`
- Product endpoint regression tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests -FullyQualifiedNameContains Product`
- Backend verification for touched projects passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API/Jadlify.API.csproj -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj`

#### Manual Verification:

- Inspect generated migration for expected `recipe_ingredients` snapshot columns and no unrelated schema churn.
- Confirm unauthenticated requests to `/api/recipes` are still blocked by the global auth policy.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 3: React Recipe Builder UI

### Overview

This phase replaces the recipes placeholder with the actual recipe list and builder experience, including live macro preview, product search, and add-missing-product flow.

### Changes Required:

#### 1. Frontend Recipe Wire Types And API Hooks

**Files**:

- `src/Jadlify.Web/src/recipes/types.ts`
- `src/Jadlify.Web/src/recipes/useRecipes.ts`
- `src/Jadlify.Web/src/recipes/useRecipeMutations.ts`
- `src/Jadlify.Web/src/recipes/useProductSearch.ts`

**Intent**: Mirror the recipe API contracts and keep fetching/mutation patterns consistent with the products feature.

**Contract**: React-query keys separate recipe list/detail from product search. Create/update/delete mutations invalidate recipe queries; product search calls `/api/products?search=...&take=...` and is enabled only with a signed-in session.

#### 2. Shared Macro Calculation Helpers

**File**: `src/Jadlify.Web/src/recipes/macroMath.ts`

**Intent**: Give the UI a deterministic live preview that matches backend math before save.

**Contract**: Frontend calculation uses the selected product per-100g values and whole-recipe grams, then divides by portions for per-serving totals, rounding results to a consistent decimal precision (e.g. 1 decimal place for macros, 0 or 1 for calories) to avoid JavaScript binary floating-point artifacts. It is preview-only; backend remains authoritative on save.

#### 3. Recipe Builder Components

**Files**:

- `src/Jadlify.Web/src/recipes/RecipesPage.tsx`
- `src/Jadlify.Web/src/recipes/RecipeFormModal.tsx`
- `src/Jadlify.Web/src/recipes/ProductPicker.tsx`
- `src/Jadlify.Web/src/recipes/RecipeMacroPreview.tsx`
- `src/Jadlify.Web/src/recipes/DeleteRecipeDialog.tsx`

**Intent**: Provide the user-visible recipe CRUD workflow.

**Contract**: The page lists current-user recipes with total and per-serving macro summaries. The builder requires name, portions, and at least one ingredient; prevents duplicate selected products; each ingredient row has product search/select and whole-recipe grams; macro preview updates live; save is disabled until the recipe is complete. Delete uses a confirmation dialog and surfaces `409` when a recipe is already used by a meal-plan entry.

#### 4. Add Missing Product From Builder

**Files**:

- `src/Jadlify.Web/src/products/ProductFormModal.tsx`
- `src/Jadlify.Web/src/recipes/RecipeFormModal.tsx`
- `src/Jadlify.Web/src/recipes/ProductPicker.tsx`

**Intent**: Let the user create a missing product from the recipe builder without abandoning the recipe.

**Contract**: Product creation reuses the existing product form behavior and barcode fallback. After a product is created, product search/list data is invalidated and the new product can be selected for the ingredient row. The recipe draft state must survive opening and closing the product modal.

#### 5. Route Wiring

**File**: `src/Jadlify.Web/src/routes/sections/RecipesPage.tsx`

**Intent**: Point the existing protected `/recipes` route at the real recipe feature.

**Contract**: The route continues to live inside the authenticated app shell and navigation defined by the current app structure.

### Success Criteria:

#### Automated Verification:

- Recipe frontend component tests pass: `cd src/Jadlify.Web; npm test -- src/recipes`
- Existing product frontend tests still pass: `cd src/Jadlify.Web; npm test -- src/products`
- Frontend lint passes: `cd src/Jadlify.Web; npm run lint`
- Frontend build passes: `cd src/Jadlify.Web; npm run build`

#### Manual Verification:

- In the browser, create a recipe with two products and confirm live total and per-serving macros match manual 100g proportional math.
- Add a missing product from inside the recipe builder, return to the recipe draft, select it, and save without losing prior ingredient rows.
- Edit a recipe by changing portions and ingredient grams; confirm the list reflects updated totals.
- Delete an unused recipe and confirm it disappears from the list.
- Verify the layout is usable on desktop and a narrow mobile viewport without text overlap.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 4: Vertical Verification And Handoff

### Overview

This phase closes the slice by adding/confirming cross-layer regression coverage, running narrow verification, and making the handoff explicit for later meal-plan and shopping-list slices.

### Changes Required:

#### 1. Domain And Application Regression Tests

**Files**:

- `tests/Jadlify.Domain.Tests/Nutrition/MacroCalculatorTests.cs`
- `tests/Jadlify.Domain.Tests/Recipes/RecipeTests.cs`
- `tests/Jadlify.Application.Tests/Recipes/*`

**Intent**: Lock the load-bearing business rules: snapshot math, whole-recipe grams, per-serving division, duplicate ingredient rejection, and invalid/cross-user products failing at save.

**Contract**: Tests prove recipe totals come from ingredient snapshots, not current product rows, and that repeated calculations produce identical results.

#### 2. Persistence And API Isolation Tests

**Files**:

- `tests/Jadlify.Infrastructure.Tests/Persistence/RecipeRepositoryTests.cs`
- `tests/Jadlify.API.Tests/Recipes/RecipeEndpointsTests.cs`

**Intent**: Prove the hardest guardrail of the PRD for the new resource type.

**Contract**: User A cannot list, get, update, or delete User B's recipes; User A cannot create/update a recipe with User B's product id; missing/cross-user ingredients produce a safe failure; deleting a recipe used by a meal-plan entry returns conflict.

#### 3. Frontend Workflow Tests

**Files**:

- `src/Jadlify.Web/src/recipes/RecipesPage.test.tsx`
- `src/Jadlify.Web/src/recipes/macroMath.test.ts`

**Intent**: Cover the user workflow and the UI's live calculation preview.

**Contract**: Tests cover empty state, list rendering, create/edit/delete mutation calls, product search, add-missing-product handoff, disabled save for incomplete recipes, duplicate product prevention, and live macro preview.

#### 4. Change Documentation Final Pass

**Files**:

- `context/changes/recipe-builder-with-macro-calculation/plan.md`
- `context/changes/recipe-builder-with-macro-calculation/plan-brief.md`
- `docs/reference/contract-surfaces.md`

**Intent**: Ensure the final implementation contract and future handoff are current after implementation.

**Contract**: If implementation changes a route, DTO field, snapshot policy, or verification command from this plan, update the plan/checklist or contract registry rather than leaving stale instructions.

### Success Criteria:

#### Automated Verification:

- Backend narrow verification passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API/Jadlify.API.csproj -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj`
- Domain/application/infrastructure recipe tests pass: `pwsh ./.scripts/test-min.ps1 -Filter FullyQualifiedName~Recipe`
- Frontend recipe tests pass: `cd src/Jadlify.Web; npm test -- src/recipes`
- Frontend lint/build pass: `cd src/Jadlify.Web; npm run lint` and `cd src/Jadlify.Web; npm run build`

#### Manual Verification:

- Complete one authenticated recipe CRUD smoke test against the local API and Vite frontend.
- Confirm recipe totals remain stable after deleting or editing the source product used to create an ingredient.
- Confirm no committed secrets or generated local environment files were added.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before marking the change complete.

---

## Testing Strategy

### Unit Tests:

- Domain tests for `Recipe`, `RecipeIngredient`, `GramAmount`, and `MacroCalculator` snapshot math.
- Application validator tests for name, portions, at least one ingredient, duplicate product ids, positive grams, and product existence.
- Frontend `macroMath` tests mirroring backend proportional calculations.

### Integration Tests:

- Infrastructure repository tests for owner-scoped list/get/update/delete and ingredient snapshot persistence.
- API tests for `GET/POST/PUT/DELETE /api/recipes`, validation failures, `409 Recipe.InUse`, and cross-user isolation.
- Product API regression tests for new `search/skip/take` query parameters.

### Manual Testing Steps:

1. Sign in, create two products, open `/recipes`, and create a recipe using both products.
2. Confirm whole-recipe and per-serving totals match manual calculation from per-100g values and grams.
3. Edit the recipe's portions and grams, save, and confirm totals update.
4. Add a missing product from the recipe builder and use it without losing the current recipe draft.
5. Delete an unused recipe and verify it disappears.
6. Edit or delete a source product and confirm the saved recipe still displays the original snapshot totals.

## Performance Considerations

Recipe totals are cheap at MVP scale and should be computed on demand from ingredient snapshots. Product picker search should cap result size with `take` to avoid pulling large catalogs into a modal. No caching beyond react-query is required in this slice.

## Migration Notes

This change requires an EF migration for recipe ingredient snapshot columns. Existing local recipe rows, if any, may not have product snapshots; because recipes are not user-visible yet, the migration can either require empty recipe data or backfill from existing product rows where possible. Do not introduce direct foreign keys from `recipe_ingredients.product_id` to `products`; the S-02 historical delete policy depends on recipe ingredients surviving product deletion.

## References

- PRD recipe requirements: `context/foundation/prd.md`
- Roadmap slice S-03: `context/foundation/roadmap.md`
- Stack and data boundary: `context/foundation/tech-stack.md`
- Product S-02 handoff: `context/changes/product-catalog-with-barcode-fallback/plan-brief.md`
- Contract registry: `docs/reference/contract-surfaces.md`
- Existing macro surface: `src/Jadlify.Domain/Nutrition/MacroCalculator.cs:7`
- Existing recipe skeleton: `src/Jadlify.Domain/Recipes/Recipe.cs:3`
- Existing ingredient skeleton: `src/Jadlify.Domain/Recipes/RecipeIngredient.cs:5`
- Existing product endpoints pattern: `src/Jadlify.API/Products/ProductEndpoints.cs:22`
- Existing recipes placeholder: `src/Jadlify.Web/src/routes/sections/RecipesPage.tsx:3`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` - <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Domain And Application Recipe Contracts

#### Automated

- [x] 1.1 Domain recipe snapshot and macro tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Domain.Tests -FullyQualifiedNameContains Recipe` - aff86fb
- [x] 1.2 Application recipe handler and validator tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Application.Tests -FullyQualifiedNameContains Recipes` - aff86fb
- [x] 1.3 Narrow build passes for touched backend projects: `pwsh ./.scripts/build-min.ps1 -Project src/Jadlify.Application/Jadlify.Application.csproj` - aff86fb

#### Manual

- [x] 1.4 Review the application contracts and confirm they describe whole-recipe grams, not per-serving grams. - aff86fb
- [x] 1.5 Confirm no new application code depends on ASP.NET Core, EF Core, Supabase SDKs, or raw claims. - aff86fb

### Phase 2: Persistence, Product Search, And Recipe API

#### Automated

- [x] 2.1 Infrastructure recipe repository and model tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Infrastructure.Tests -FullyQualifiedNameContains Recipe`
- [x] 2.2 API recipe endpoint tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests -FullyQualifiedNameContains Recipe`
- [x] 2.3 Product endpoint regression tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests -FullyQualifiedNameContains Product`
- [x] 2.4 Backend verification for touched projects passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API/Jadlify.API.csproj -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj`

#### Manual

- [x] 2.5 Inspect generated migration for expected `recipe_ingredients` snapshot columns and no unrelated schema churn.
- [x] 2.6 Confirm unauthenticated requests to `/api/recipes` are still blocked by the global auth policy.

### Phase 3: React Recipe Builder UI

#### Automated

- [x] 3.1 Recipe frontend component tests pass: `cd src/Jadlify.Web; npm test -- src/recipes` - 68d8cbe
- [x] 3.2 Existing product frontend tests still pass: `cd src/Jadlify.Web; npm test -- src/products` - 68d8cbe
- [x] 3.3 Frontend lint passes: `cd src/Jadlify.Web; npm run lint` - 68d8cbe
- [x] 3.4 Frontend build passes: `cd src/Jadlify.Web; npm run build` - 68d8cbe

#### Manual

- [x] 3.5 In the browser, create a recipe with two products and confirm live total and per-serving macros match manual 100g proportional math. - 68d8cbe
- [x] 3.6 Add a missing product from inside the recipe builder, return to the recipe draft, select it, and save without losing prior ingredient rows. - 68d8cbe
- [x] 3.7 Edit a recipe by changing portions and ingredient grams; confirm the list reflects updated totals. - 68d8cbe
- [x] 3.8 Delete an unused recipe and confirm it disappears from the list. - 68d8cbe
- [x] 3.9 Verify the layout is usable on desktop and a narrow mobile viewport without text overlap. - 68d8cbe

### Phase 4: Vertical Verification And Handoff

#### Automated

- [x] 4.1 Backend narrow verification passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API/Jadlify.API.csproj -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj` - a4c4bb6
- [x] 4.2 Domain/application/infrastructure recipe tests pass: `pwsh ./.scripts/test-min.ps1 -Filter FullyQualifiedName~Recipe` - a4c4bb6
- [x] 4.3 Frontend recipe tests pass: `cd src/Jadlify.Web; npm test -- src/recipes` - a4c4bb6
- [x] 4.4 Frontend lint/build pass: `cd src/Jadlify.Web; npm run lint` and `cd src/Jadlify.Web; npm run build` - a4c4bb6

#### Manual

- [x] 4.5 Complete one authenticated recipe CRUD smoke test against the local API and Vite frontend. - a4c4bb6
- [x] 4.6 Confirm recipe totals remain stable after deleting or editing the source product used to create an ingredient. - a4c4bb6
- [x] 4.7 Confirm no committed secrets or generated local environment files were added. - a4c4bb6
