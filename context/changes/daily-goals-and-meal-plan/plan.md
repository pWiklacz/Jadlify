# Daily Goals And Meal Plan Implementation Plan

## Overview

Implement roadmap slice S-04: a signed-in user can set one current daily macro goal and manage recipe-based meal-plan entries for one selected day. The slice deliberately stops before daily macro totals, goal deltas, and shopping-list generation; those remain S-05 and S-06.

## Current State Analysis

Jadlify already has the foundations this change should build on. Auth, per-user ownership, product CRUD, recipe CRUD, recipe macro calculation, and the responsive app shell are in place. The planning persistence model also already exists: `daily_macro_goals` stores one current goal per user, and `meal_plan_entries` stores date, recipe id, meal type, and integer portions. What is missing is the user-visible vertical: application use-cases, HTTP contracts, endpoint tests, React pages, and handoff docs for later daily-summary and shopping-list slices.

## Desired End State

After this plan, `/goals` is a protected page where the user can view and replace their current daily kcal/protein/fat/carbohydrate target. `/meal-plan` is a protected page where the user selects one date, lists entries for that date, adds an existing recipe to a meal type with an integer portion count, edits only meal type and portions, and deletes entries. The meal plan can be used even before a goal is configured. Existing entries reference the current recipe by id rather than snapshotting recipe details; recipe deletion already returns conflict when a recipe is used by a meal-plan entry.

### Key Discoveries:

- Roadmap S-04 is scoped to goals and meal-plan entries; daily macro totals and deltas are S-05, and shopping-list generation is S-06 (`context/foundation/roadmap.md:36`, `context/foundation/roadmap.md:37`).
- PRD FR-011, FR-012, and FR-014 define one current daily goal plus recipe/date/meal-type/portion plan entries with edit/delete support (`context/foundation/prd.md:132`, `context/foundation/prd.md:137`, `context/foundation/prd.md:141`).
- Daily goal and meal-plan EF tables already exist with `target_*`, `meal_type`, and integer `portions` columns, so this plan should not introduce a schema migration for the selected MVP behavior (`src/Jadlify.Infrastructure/Persistence/Migrations/20260528132833_InitialCreate.cs:15`, `src/Jadlify.Infrastructure/Persistence/Migrations/20260528132833_InitialCreate.cs:63`).
- `DailyMacroGoalRepository` and `MealPlanRepository` already owner-scope reads and writes through `ICurrentUser` (`src/Jadlify.Infrastructure/Persistence/Repositories/DailyMacroGoalRepository.cs:8`, `src/Jadlify.Infrastructure/Persistence/Repositories/MealPlanRepository.cs:8`).
- `MealPlanEntry` currently has no mutation method even though FR-014 requires editing meal type and portions (`src/Jadlify.Domain/Planning/MealPlanEntry.cs:3`).
- The existing API pattern is Minimal API route groups through `IMediator` and `ResultExtensions.ToProblem` (`src/Jadlify.API/Recipes/RecipeEndpoints.cs:19`, `src/Jadlify.API/Common/ResultExtensions.cs:9`).
- `/goals` and `/meal-plan` currently render placeholders inside the protected app shell (`src/Jadlify.Web/src/routes/sections/GoalsPage.tsx:3`, `src/Jadlify.Web/src/routes/sections/MealPlanPage.tsx:3`).

## What We're NOT Doing

- No daily macro totals, remaining-to-goal values, progress bars, or goal deltas in S-04.
- No shopping-list generation, aggregation, export, or checkbox shopping UI.
- No weekly or multi-day plan view; the UI edits one selected day at a time.
- No goal history, named goal profiles, reduction/bulk cycles, or effective-date versioning.
- No decimal/fractional portions; meal-plan entries use positive integer portions.
- No recipe snapshots on meal-plan entries. Entries reference the current recipe by id.
- No ability to change an entry's date or recipe during edit; changing those means delete and add again.
- No direct browser-to-database access; all planning data stays behind the ASP.NET Core API.

## Implementation Approach

Build the S-04 vertical through the existing Clean Architecture boundaries. Add the missing domain/application planning contracts first, expose them through authenticated Minimal API endpoints, then replace the two placeholder React routes with focused pages that use the same API client and react-query patterns as products and recipes.

The selected behavior from planning is:

- S-04 stores and displays goals and meal-plan entries only; S-05 owns daily totals and deltas.
- The meal-plan UI selects a single date, defaulting sensibly to today.
- `/goals` and `/meal-plan` remain separate routes.
- Meal-plan portions are positive integers.
- Duplicate entries are allowed, including the same date, meal type, and recipe.
- Meal planning works without a configured goal.
- Meal-plan entries reference the current recipe, not a snapshot.
- Entry edit supports only meal type and portions.

## Critical Implementation Details

### Scope Boundary With S-05

Do not add daily macro summary DTOs or UI deltas in this change. It is acceptable to display recipe names and existing recipe metadata needed to identify entries, but day-level macro totals must wait for S-05.

### No Migration Expected

The selected behavior matches the existing EF model: one current daily goal, string meal type, and integer portions. If implementation discovers schema drift, inspect it carefully before adding a migration; a new migration should not be the default outcome of this plan.

## Phase 1: Planning Application Contracts

### Overview

This phase adds the missing domain and application layer contract for daily goals and meal-plan entries, including validation and focused application tests.

### Changes Required:

#### 1. Meal Plan Entry Editing

**File**: `src/Jadlify.Domain/Planning/MealPlanEntry.cs`

**Intent**: Support FR-014 without bypassing the domain object from handlers.

**Contract**: Add an instance method that updates only `MealType` and positive integer `Portions`. It must not change `Id`, `Date`, or `RecipeId`.

#### 2. Goal Application Contracts

**Files**:

- `src/Jadlify.Application/Planning/DailyGoals/*`
- `src/Jadlify.Application/Planning/PlanningMacroGoalDto.cs`
- `src/Jadlify.Application/Planning/PlanningValidationBounds.cs`

**Intent**: Add use-cases for reading and replacing the current user's single daily macro goal.

**Contract**: Add a query for the current goal and an upsert command accepting calories, protein, fat, and carbohydrates. Missing goal is a normal result, not an error. Validation rejects negative values, zero-or-negative calories, and obviously unrealistic upper bounds using a planning-specific bounds class.

#### 3. Meal Plan Application Contracts

**Files**:

- `src/Jadlify.Application/Planning/MealPlans/*`
- `src/Jadlify.Application/Planning/MealPlanEntryDto.cs`

**Intent**: Add use-cases for listing, adding, editing, and deleting entries for a selected day.

**Contract**: Add:

- `ListMealPlanEntriesQuery(DateOnly date)`
- `AddMealPlanEntryCommand(DateOnly date, Guid recipeId, MealType mealType, int portions)`
- `UpdateMealPlanEntryCommand(Guid id, MealType mealType, int portions)`
- `DeleteMealPlanEntryCommand(Guid id)`

Create validates that the recipe exists for the current user before adding an entry. List returns entries for the requested date with enough current recipe data for display, at minimum recipe id and recipe name. Duplicate entries are allowed.

#### 4. Recipe Repository Lookup Support

**File**: `src/Jadlify.Application/Recipes/IRecipeRepository.cs`

**Intent**: Let planning list entries with current recipe display data without unbounded recipe loading or cross-user leakage.

**Contract**: Add an owner-scoped batch lookup by recipe id, implemented in Phase 2. Existing recipe CRUD behavior remains unchanged.

#### 5. Application Tests

**Files**:

- `tests/Jadlify.Application.Tests/Planning/*`
- `tests/Jadlify.Application.Tests/Recipes/FakeRecipeRepository.cs`

**Intent**: Lock planning behavior before API/UI work.

**Contract**: Tests cover goal upsert validation, missing-goal query behavior, meal-plan create rejecting missing/cross-user recipes through the owner-scoped repository boundary, integer portion validation, duplicate entries being allowed, update changing only meal type and portions, and delete delegating to the repository.

### Success Criteria:

#### Automated Verification:

- Planning domain and macro-related tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Domain.Tests -FullyQualifiedNameContains Planning`
- Planning application handler and validator tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Application.Tests -FullyQualifiedNameContains Planning`
- Narrow application build passes: `pwsh ./.scripts/build-min.ps1 -Project src/Jadlify.Application/Jadlify.Application.csproj`

#### Manual Verification:

- Confirm application contracts do not include daily summary, goal deltas, or shopping-list output.
- Confirm no application code depends on ASP.NET Core, EF Core, Supabase SDKs, or raw claims.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase. Phase blocks use plain bullets; the corresponding checkboxes live in the `## Progress` section at the bottom of the plan.

---

## Phase 2: Planning API And Integration Tests

### Overview

This phase exposes daily goals and meal-plan entries through authenticated API endpoints, completes the infrastructure lookup needed by Phase 1, and proves ownership boundaries with integration tests.

### Changes Required:

#### 1. Recipe Repository Batch Lookup

**File**: `src/Jadlify.Infrastructure/Persistence/Repositories/RecipeRepository.cs`

**Intent**: Support planning list responses with current recipe display data.

**Contract**: Implement the owner-scoped batch lookup added to `IRecipeRepository`. It must filter by current user, include only requested ids, and not return another user's recipes.

#### 2. Daily Goal API Contracts And Endpoints

**Files**:

- `src/Jadlify.API/Planning/DailyGoalContracts.cs`
- `src/Jadlify.API/Planning/DailyGoalEndpoints.cs`
- `src/Jadlify.API/Program.cs`

**Intent**: Expose the current daily goal as a singleton resource under the API boundary.

**Contract**: Add:

- `GET /api/daily-goal` returns `200` with the current goal response or JSON `null` when not configured.
- `PUT /api/daily-goal` upserts the current goal and returns `204`.

Routes inherit the global authenticated fallback policy and must not call `AllowAnonymous`.

#### 3. Meal Plan API Contracts And Endpoints

**Files**:

- `src/Jadlify.API/Planning/MealPlanContracts.cs`
- `src/Jadlify.API/Planning/MealPlanEndpoints.cs`
- `src/Jadlify.API/Program.cs`

**Intent**: Expose one-day meal-plan CRUD through the same Minimal API + mediator + Result mapping pattern used by products and recipes.

**Contract**: Add:

- `GET /api/meal-plan?date=yyyy-MM-dd` returns entries for that date.
- `POST /api/meal-plan` creates an entry and returns `201` with `Location`.
- `PUT /api/meal-plan/{id:guid}` updates meal type and portions only, returning `204`.
- `DELETE /api/meal-plan/{id:guid}` deletes an entry, returning `204`.

Requests use ISO `DateOnly` strings and meal type names matching `Breakfast`, `Lunch`, `Dinner`, and `Snack`. Missing or cross-user entries return safe not-found style failures.

#### 4. API Integration Tests

**Files**:

- `tests/Jadlify.API.Tests/Planning/DailyGoalEndpointsTests.cs`
- `tests/Jadlify.API.Tests/Planning/MealPlanEndpointsTests.cs`

**Intent**: Prove the protected API contract and PRD data-isolation guardrail.

**Contract**: Tests cover authentication required, goal get/upsert, one user's goal not visible to another user, meal-plan create/list/update/delete, date filtering, duplicate entries allowed, missing/cross-user recipe rejected on create, and one user unable to access another user's entries.

#### 5. Infrastructure Regression Tests

**Files**:

- `tests/Jadlify.Infrastructure.Tests/Persistence/RecipeRepositoryTests.cs`
- `tests/Jadlify.Infrastructure.Tests/Persistence/MealPlanRepositoryTests.cs`

**Intent**: Cover repository behavior introduced or depended on by the API.

**Contract**: Tests cover owner-scoped recipe batch lookup and update behavior for meal-plan entries. Existing owner-scoped list/get/delete tests stay intact.

### Success Criteria:

#### Automated Verification:

- Infrastructure planning/repository tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Infrastructure.Tests -FullyQualifiedNameContains Planning`
- API planning endpoint tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests -FullyQualifiedNameContains Planning`
- Recipe endpoint regressions still pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests -FullyQualifiedNameContains Recipe`
- Backend verification for touched projects passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API/Jadlify.API.csproj -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj`

#### Manual Verification:

- Confirm unauthenticated requests to `/api/daily-goal` and `/api/meal-plan` are blocked by the global auth policy.
- Inspect any generated EF changes and confirm no planning schema migration was introduced unless a real schema mismatch was found.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 3: Goals And Meal Plan React UI

### Overview

This phase replaces the `/goals` and `/meal-plan` placeholders with usable protected pages backed by the new planning API.

### Changes Required:

#### 1. Planning Frontend Types And Hooks

**Files**:

- `src/Jadlify.Web/src/planning/types.ts`
- `src/Jadlify.Web/src/planning/useDailyGoal.ts`
- `src/Jadlify.Web/src/planning/useMealPlan.ts`
- `src/Jadlify.Web/src/planning/useMealPlanMutations.ts`

**Intent**: Mirror the API planning contracts and keep cache invalidation consistent with recipe/product hooks.

**Contract**: Daily-goal query handles `null` as the empty state. Meal-plan query keys include the selected ISO date. Mutations invalidate only the relevant goal or selected-date meal-plan queries. Hooks are enabled only for signed-in sessions.

#### 2. Daily Goals Page

**Files**:

- `src/Jadlify.Web/src/planning/DailyGoalsPage.tsx`
- `src/Jadlify.Web/src/routes/sections/GoalsPage.tsx`

**Intent**: Let the user configure the single current daily macro goal.

**Contract**: The page displays an empty state when no goal exists, renders a compact form for calories, protein, fat, and carbohydrates, saves via `PUT /api/daily-goal`, and clearly communicates that MVP has one current goal rather than goal history. It should show loading, error, saving, and saved states without exposing implementation details.

#### 3. Meal Plan Page

**Files**:

- `src/Jadlify.Web/src/planning/MealPlanPage.tsx`
- `src/Jadlify.Web/src/planning/MealPlanEntryForm.tsx`
- `src/Jadlify.Web/src/routes/sections/MealPlanPage.tsx`

**Intent**: Let the user manage a selected day's recipe entries.

**Contract**: The page defaults to today, allows selecting one date, lists entries grouped or ordered by meal type, supports adding a recipe from existing recipes, supports positive integer portions, allows duplicate entries, edits only meal type and portions, and deletes entries. It does not require a configured goal and does not show day-level macro totals or deltas.

#### 4. Recipe Selection Reuse

**Files**:

- `src/Jadlify.Web/src/recipes/useRecipes.ts`
- `src/Jadlify.Web/src/planning/MealPlanEntryForm.tsx`

**Intent**: Reuse current recipe data as the source for meal-plan entries.

**Contract**: The add-entry form loads the user's recipes through the existing recipes hook or a small planning-specific wrapper over `/api/recipes`. If no recipes exist, the UI directs the user to create a recipe rather than allowing an invalid entry.

#### 5. Frontend Tests

**Files**:

- `src/Jadlify.Web/src/planning/DailyGoalsPage.test.tsx`
- `src/Jadlify.Web/src/planning/MealPlanPage.test.tsx`

**Intent**: Cover the S-04 user flows and cache behavior.

**Contract**: Tests cover goal empty state and save, meal-plan date filtering query path, add entry body, duplicate entries rendering, edit body limited to meal type and portions, delete call, no-recipes state, and absence of daily summary/delta UI.

### Success Criteria:

#### Automated Verification:

- Planning frontend tests pass: `cd src/Jadlify.Web; npm test -- src/planning`
- Existing recipe frontend tests still pass: `cd src/Jadlify.Web; npm test -- src/recipes`
- Frontend lint passes: `cd src/Jadlify.Web; npm run lint`
- Frontend build passes: `cd src/Jadlify.Web; npm run build`

#### Manual Verification:

- In the browser, set a daily goal, refresh, and confirm the current goal remains visible.
- Select a date, add two entries including duplicate recipe usage, and confirm both appear.
- Edit an entry's meal type and portions; confirm date and recipe do not change.
- Delete an entry and confirm only that entry disappears.
- Verify `/meal-plan` still works when no goal is configured.
- Verify desktop and narrow mobile layouts have no text overlap or unusable controls.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 4: Verification And Handoff

### Overview

This phase closes the slice with narrow verification, documentation updates, and explicit handoff contracts for daily summary and shopping list work.

### Changes Required:

#### 1. Contract Surface Registry

**File**: `docs/reference/contract-surfaces.md`

**Intent**: Record the S-04 contracts that future S-05 and S-06 work must reuse.

**Contract**: Add a planning section naming the singleton daily goal API, meal-plan endpoint routes, integer portion semantics, duplicate-entry allowance, current-recipe reference semantics, and the rule that S-05 should compute day totals from meal entries plus recipe snapshots through `MacroCalculator.ForMealEntry`.

#### 2. Change Plan Final Pass

**Files**:

- `context/changes/daily-goals-and-meal-plan/plan.md`
- `context/changes/daily-goals-and-meal-plan/plan-brief.md`
- `context/changes/daily-goals-and-meal-plan/change.md`

**Intent**: Keep change documentation aligned with what was actually implemented.

**Contract**: If implementation changes route names, DTO fields, validation bounds, or verification commands from this plan, update the plan/brief before closing the phase. `change.md` remains `status: planned` until implementation progress updates it.

#### 3. Narrow Backend Verification

**Files**:

- Backend projects and tests touched by S-04.

**Intent**: Confirm backend compile, validation, ownership, and endpoint behavior.

**Contract**: Use the repo's minimal scripts, not raw `dotnet build` or `dotnet test`.

#### 4. Frontend Verification

**Files**:

- `src/Jadlify.Web/src/planning/*`
- Existing route and recipe files touched by S-04.

**Intent**: Confirm planning UI tests, lint, and production build.

**Contract**: Frontend verification uses npm scripts from `src/Jadlify.Web` and must include at least planning tests, lint, and build.

### Success Criteria:

#### Automated Verification:

- Backend narrow verification passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API/Jadlify.API.csproj -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj`
- Planning tests across backend layers pass: `pwsh ./.scripts/test-min.ps1 -Filter FullyQualifiedName~Planning`
- Frontend planning tests pass: `cd src/Jadlify.Web; npm test -- src/planning`
- Frontend lint/build pass: `cd src/Jadlify.Web; npm run lint` and `cd src/Jadlify.Web; npm run build`

#### Manual Verification:

- Complete one authenticated S-04 smoke test in the browser: set goal, add entries for a selected date, edit type/portions, delete an entry.
- Confirm no daily summary, goal delta, shopping-list, weekly plan, or goal-history behavior slipped into the UI.
- Confirm no committed secrets or generated local environment files were added.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before marking the change complete.

---

## Testing Strategy

### Unit Tests:

- Domain tests for `MealPlanEntry` edit semantics and positive portion enforcement.
- Application validator tests for daily-goal bounds, date/recipe/meal-type inputs, positive integer portions, and update/delete id handling.
- Application handler tests for missing goal, upsert, recipe existence checks, duplicate entries allowed, update scope, and delete delegation.
- Frontend component tests for goals form, meal-plan date selection, add/edit/delete flows, empty states, and duplicate rendering.

### Integration Tests:

- Infrastructure tests for owner-scoped daily goals, meal-plan list/update/delete, and recipe batch lookup.
- API tests for `GET/PUT /api/daily-goal` and `GET/POST/PUT/DELETE /api/meal-plan`.
- Cross-user API tests proving one user cannot read, mutate, or create entries using another user's recipe.

### Manual Testing Steps:

1. Sign in and open `/goals`.
2. Save a daily target with kcal/protein/fat/carbohydrates, refresh, and confirm it remains the current target.
3. Open `/meal-plan`, select a specific date, and add an existing recipe as breakfast with 1 portion.
4. Add the same recipe again for the same date and meal type; confirm duplicate entries are allowed and visible.
5. Edit one entry to a different meal type and portion count; confirm its date and recipe did not change.
6. Delete one entry and confirm other entries for the date remain.
7. Confirm the page does not show daily macro totals, goal deltas, or shopping-list output.

## Performance Considerations

MVP data volume is small, and one selected day should contain few entries. Owner-scoped queries should still filter by date in the database and avoid loading all meal-plan entries. Recipe lookup for meal-plan display should batch by ids for the selected day rather than looping through per-entry HTTP requests or repository calls. React Query caching is sufficient for the UI; no additional caching is required.

## Migration Notes

No migration is expected for the chosen MVP behavior because `daily_macro_goals` and `meal_plan_entries` already exist with the required columns. If implementation changes portion granularity, goal versioning, or entry snapshots, that would be a scope change and should be re-planned before adding schema changes.

## References

- PRD goals and meal-plan requirements: `context/foundation/prd.md`
- Roadmap S-04: `context/foundation/roadmap.md`
- Stack and backend data boundary: `context/foundation/tech-stack.md`
- Contract registry: `docs/reference/contract-surfaces.md`
- Recipe handoff: `context/changes/recipe-builder-with-macro-calculation/plan-brief.md`
- Daily goal domain model: `src/Jadlify.Domain/Planning/DailyMacroGoal.cs:5`
- Meal-plan domain model: `src/Jadlify.Domain/Planning/MealPlanEntry.cs:3`
- Planning repository ports: `src/Jadlify.Application/Planning/IDailyMacroGoalRepository.cs:9`, `src/Jadlify.Application/Planning/IMealPlanRepository.cs:11`
- Planning repository implementations: `src/Jadlify.Infrastructure/Persistence/Repositories/DailyMacroGoalRepository.cs:8`, `src/Jadlify.Infrastructure/Persistence/Repositories/MealPlanRepository.cs:8`
- Existing recipe endpoint pattern: `src/Jadlify.API/Recipes/RecipeEndpoints.cs:19`
- Existing frontend placeholders: `src/Jadlify.Web/src/routes/sections/GoalsPage.tsx:3`, `src/Jadlify.Web/src/routes/sections/MealPlanPage.tsx:3`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` - <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Planning Application Contracts

#### Automated

- [x] 1.1 Planning domain and macro-related tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Domain.Tests -FullyQualifiedNameContains Planning` — 84a864d
- [x] 1.2 Planning application handler and validator tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Application.Tests -FullyQualifiedNameContains Planning` — 84a864d
- [x] 1.3 Narrow application build passes: `pwsh ./.scripts/build-min.ps1 -Project src/Jadlify.Application/Jadlify.Application.csproj` — 84a864d

#### Manual

- [x] 1.4 Confirm application contracts do not include daily summary, goal deltas, or shopping-list output. — 84a864d
- [x] 1.5 Confirm no application code depends on ASP.NET Core, EF Core, Supabase SDKs, or raw claims. — 84a864d

### Phase 2: Planning API And Integration Tests

#### Automated

- [x] 2.1 Infrastructure planning/repository tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.Infrastructure.Tests -FullyQualifiedNameContains Planning` — db611c8
- [x] 2.2 API planning endpoint tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests -FullyQualifiedNameContains Planning` — db611c8
- [x] 2.3 Recipe endpoint regressions still pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests -FullyQualifiedNameContains Recipe` — db611c8
- [x] 2.4 Backend verification for touched projects passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API/Jadlify.API.csproj -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj` — db611c8

#### Manual

- [x] 2.5 Confirm unauthenticated requests to `/api/daily-goal` and `/api/meal-plan` are blocked by the global auth policy. — db611c8
- [x] 2.6 Inspect any generated EF changes and confirm no planning schema migration was introduced unless a real schema mismatch was found. — db611c8

### Phase 3: Goals And Meal Plan React UI

#### Automated

- [x] 3.1 Planning frontend tests pass: `cd src/Jadlify.Web; npm test -- src/planning`
- [x] 3.2 Existing recipe frontend tests still pass: `cd src/Jadlify.Web; npm test -- src/recipes`
- [x] 3.3 Frontend lint passes: `cd src/Jadlify.Web; npm run lint`
- [x] 3.4 Frontend build passes: `cd src/Jadlify.Web; npm run build`

#### Manual

- [x] 3.5 In the browser, set a daily goal, refresh, and confirm the current goal remains visible.
- [x] 3.6 Select a date, add two entries including duplicate recipe usage, and confirm both appear.
- [x] 3.7 Edit an entry's meal type and portions; confirm date and recipe do not change.
- [x] 3.8 Delete an entry and confirm only that entry disappears.
- [x] 3.9 Verify `/meal-plan` still works when no goal is configured.
- [x] 3.10 Verify desktop and narrow mobile layouts have no text overlap or unusable controls.

### Phase 4: Verification And Handoff

#### Automated

- [x] 4.1 Backend narrow verification passes: `pwsh ./.scripts/verify-min.ps1 -BuildProject src/Jadlify.API/Jadlify.API.csproj -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj`
- [x] 4.2 Planning tests across backend layers pass: `pwsh ./.scripts/test-min.ps1 -Filter FullyQualifiedName~Planning`
- [x] 4.3 Frontend planning tests pass: `cd src/Jadlify.Web; npm test -- src/planning`
- [x] 4.4 Frontend lint/build pass: `cd src/Jadlify.Web; npm run lint` and `cd src/Jadlify.Web; npm run build`

#### Manual

- [ ] 4.5 Complete one authenticated S-04 smoke test in the browser: set goal, add entries for a selected date, edit type/portions, delete an entry.
- [ ] 4.6 Confirm no daily summary, goal delta, shopping-list, weekly plan, or goal-history behavior slipped into the UI.
- [ ] 4.7 Confirm no committed secrets or generated local environment files were added.
