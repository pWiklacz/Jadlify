# Persistent User Resources Implementation Plan

## Overview

Implement the persistence foundation for Jadlify user-owned resources. This change adds EF Core/Npgsql data access against Supabase Postgres, defines a thin MVP schema for products, recipes, daily goals, and meal-plan entries, and creates a deterministic macro-calculation path that later product, recipe, plan, summary, and shopping-list slices can reuse.

## Current State Analysis

The account boundary from F-01 is already implemented: the API validates Supabase JWTs, maps the literal `sub` claim into `ApplicationUserId`, exposes `ICurrentUser`, and records the contract surface future user-owned persistence must reuse. The repository still has no EF Core, Npgsql, DbContext, migrations, domain resource model, or repository implementation. `Infrastructure` is a project shell that depends on `Application`, and `Domain` depends only on `SharedKernel`.

## Desired End State

After this plan is complete, the solution has a local-testable EF Core persistence layer for the MVP resource graph, every user-owned persisted table carries the authenticated application user id, and deterministic macro calculations are available in pure code without depending on SQL computed values or persisted totals. The normal agent verification path remains local and stable: build/test/format scripts, model/migration checks, and secret scanning do not require live Supabase credentials.

### Key Discoveries:

- Roadmap F-02 selects Supabase Postgres, EF Core with Npgsql, backend-scoped repositories/handlers, external secret storage, and optional-only RLS defense in depth: `context/foundation/roadmap.md:82`.
- F-02 risk explicitly warns against overscaling the data layer; this plan limits the foundation to resources required by the first daily flow: `context/foundation/roadmap.md:90`.
- Tech stack requires product, recipe, goal, meal-plan, macro-summary, and shopping-list behavior to go through the ASP.NET Core API, not direct browser table access: `context/foundation/tech-stack.md:31`.
- Tech stack requires user-owned tables to include a user id and repositories/queries/commands to scope by the authenticated user: `context/foundation/tech-stack.md:33`.
- `ApplicationUserId` already wraps the stable Supabase subject value: `src/Jadlify.Application/Identity/ApplicationUserId.cs:3`.
- `UserScope` already returns `Result` / `Result<T>` forbidden failures for cross-user access: `src/Jadlify.Application/Identity/UserScope.cs:18`.
- The API already registers `ICurrentUser`, bearer auth, fallback authorization, and `/health` as the only anonymous endpoint: `src/Jadlify.API/Program.cs:15`, `src/Jadlify.API/Program.cs:19`, `src/Jadlify.API/Program.cs:25`, `src/Jadlify.API/Program.cs:53`, `src/Jadlify.API/Program.cs:71`.
- The contract registry says F-02 must store an owner `ApplicationUserId` value or equivalent persisted subject and pass it through `UserScope` before returning data: `docs/reference/contract-surfaces.md:18`.

## What We're NOT Doing

- Building product, recipe, goal, meal-plan, macro-summary, or shopping-list API endpoints.
- Building React UI, forms, sign-in UI, or barcode lookup behavior.
- Adding direct browser-to-table Supabase access for MVP domain data.
- Making Supabase Row Level Security the primary MVP authorization mechanism.
- Supporting `ml`, `unit`, or arbitrary text units in the MVP schema.
- Persisting generated shopping lists as separate tables; shopping lists remain a future derived projection from meal-plan ingredients.
- Implementing history/versioning for daily goals.
- Applying migrations to a live Supabase database as part of normal automated verification.
- Committing Supabase secrets, service keys, JWT secrets, or database connection strings.

## Implementation Approach

Build from stable boundaries inward. First add EF Core/Npgsql infrastructure, DbContext registration, connection-string configuration shape, and the initial migration path without requiring live Supabase in tests. Then add the minimal domain model and deterministic macro-calculation service using grams-only quantities and whole-recipe ingredient amounts. Finally define feature-specific Application ports and Infrastructure EF implementations that always include `ApplicationUserId` ownership in stored entities and query filters.

## Critical Implementation Details

Use `decimal` for grams and nutrient values in code and database mappings. Do not use `double`/`float` for macro math, and do not precompute and persist recipe/day totals in F-02; later read models may cache values only through an explicit separate decision.

Do not make `Domain` depend on `ApplicationUserId` from the Application project. Ownership is a persistence/application-boundary concern in this solution: EF records store the persisted user subject, Application repository methods are owner-scoped, and Infrastructure maps/checks that subject through `ApplicationUserId` / `UserScope` before returning data.

Deletion behavior should protect deterministic calculations. Product and recipe source records that are referenced by recipe ingredients or meal-plan entries must not be silently cascade-deleted in this foundation; later user-facing slices can translate the database/application restriction into a clear UX message.

## Phase 1: EF Core Persistence Infrastructure

### Overview

Add the database access foundation: EF Core/Npgsql packages, `JadlifyDbContext`, Infrastructure service registration, non-secret configuration shape, and migration support.

### Changes Required:

#### 1. Infrastructure package references

**File**: `src/Jadlify.Infrastructure/Jadlify.Infrastructure.csproj`

**Intent**: Add the EF Core provider and design-time tooling needed to model and migrate Supabase Postgres from the backend.

**Contract**: Add EF Core relational packages and Npgsql provider packages aligned with the repository's .NET 10 package family. Preserve the existing reference direction: Infrastructure depends on Application; Domain remains independent of Infrastructure.

#### 2. API registration bridge

**File**: `src/Jadlify.API/Jadlify.API.csproj`

**Intent**: Allow the API entrypoint to call Infrastructure service registration without leaking Infrastructure dependencies into Application or Domain.

**Contract**: Keep the existing Infrastructure project reference and avoid adding EF package references directly to API unless required by the registration surface.

#### 3. Infrastructure dependency injection

**File**: `src/Jadlify.Infrastructure/DependencyInjection.cs`

**Intent**: Provide one extension method for registering persistence services from the API composition root.

**Contract**: Add `AddInfrastructure(this IServiceCollection, IConfiguration)` or equivalent. It must register `JadlifyDbContext` with Npgsql using a named connection string from configuration and must not read secrets from source-controlled files.

#### 4. API composition root

**File**: `src/Jadlify.API/Program.cs`

**Intent**: Wire Infrastructure into the runtime after `AddApplication()` and before the app is built.

**Contract**: Add the Infrastructure registration call while preserving the existing auth middleware order and `/health` anonymous behavior. Do not add domain endpoints in this phase.

#### 5. DbContext and configuration root

**File**: `src/Jadlify.Infrastructure/Persistence/JadlifyDbContext.cs`

**Intent**: Own EF Core sets and model configuration for the MVP resource graph.

**Contract**: Add a DbContext under Infrastructure. It must apply entity configurations from the Infrastructure assembly and must not depend on ASP.NET Core or Supabase SDK types.

#### 6. Entity configuration folder

**File**: `src/Jadlify.Infrastructure/Persistence/Configurations/*.cs`

**Intent**: Keep database mapping decisions explicit and reviewable instead of spreading them through entity classes.

**Contract**: Add configuration classes for the resource entities created in Phase 2. Map owner subjects to non-empty text columns such as `user_id`, map decimal quantities/nutrients with explicit precision, and configure restrict delete behavior for dependency edges.

#### 7. Non-secret configuration placeholder

**File**: `src/Jadlify.API/appsettings.json`

**Intent**: Make the required connection-string key discoverable without committing a value.

**Contract**: Add only a placeholder or empty `ConnectionStrings` shape for the application database. Real Supabase Postgres connection strings stay in user secrets or Azure App Service application settings.

#### 8. Initial migration

**File**: `src/Jadlify.Infrastructure/Persistence/Migrations/*`

**Intent**: Create the first database schema artifact for the MVP user-owned resource graph.

**Contract**: Add the initial migration after the model exists. The migration must create user-owned tables with owner columns, foreign keys, decimal precision, and restrict delete behavior. It must not include seed data containing personal product, recipe, or goal values.

### Success Criteria:

#### Automated Verification:

- Infrastructure-focused verification passes: `pwsh ./.scripts/verify-min.ps1 -TestProject tests/Jadlify.Infrastructure.Tests/Jadlify.Infrastructure.Tests.csproj`
- API-focused verification passes after registration: `pwsh ./.scripts/verify-min.ps1 -TestProject tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj`
- Secret scan returns no committed connection strings or Supabase secrets: `rg -n "SUPABASE_|service_role|JWT_SECRET|sb_secret_|postgresql://|Host=.*Password=" src tests context -g "!context/archive/**" -g "!**/bin/**" -g "!**/obj/**"`

#### Manual Verification:

- Review `src/Jadlify.API/appsettings*.json` and confirm no real database connection string was committed.
- Review project references and confirm Domain does not reference Application or Infrastructure, and Application does not reference Infrastructure.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual review was successful before proceeding to the next phase.

---

## Phase 2: MVP Resource Model And Deterministic Calculations

### Overview

Create the thin domain/application model for the first daily flow and centralize deterministic macro math.

### Changes Required:

#### 1. Macro value model

**File**: `src/Jadlify.Domain/Nutrition/MacroNutrients.cs`

**Intent**: Represent kcal, protein, fat, and carbohydrates as one immutable value used by products, recipes, goals, and summaries.

**Contract**: Add a value object or record using `decimal` properties for kcal/protein/fat/carbohydrates. Values must reject negative numbers unless a later explicitly named delta type needs signed differences.

#### 2. Gram quantity model

**File**: `src/Jadlify.Domain/Nutrition/GramAmount.cs`

**Intent**: Keep the MVP unit decision explicit: all product and ingredient quantities are grams.

**Contract**: Add a value object or record for positive gram amounts using `decimal`. It should be reusable by product serving inputs, recipe ingredients, and meal-plan portions where grams are required.

#### 3. Product entity

**File**: `src/Jadlify.Domain/Products/Product.cs`

**Intent**: Represent a user-owned product with nutrient values per 100g.

**Contract**: Include an id, name, optional barcode, and `MacroNutrients` per 100g. Ownership must be applied by Application repository contracts and Infrastructure persistence mapping without adding an Application project dependency to Domain. The entity must not perform external barcode lookup.

#### 4. Recipe entity graph

**File**: `src/Jadlify.Domain/Recipes/Recipe.cs`

**Intent**: Represent a user-owned recipe made from product ingredients and a declared number of portions.

**Contract**: Include an id, name, portion count, and ingredient collection. Ownership must be applied at the repository/persistence boundary. Ingredient gram amounts are for the whole recipe, not per serving.

**File**: `src/Jadlify.Domain/Recipes/RecipeIngredient.cs`

**Intent**: Represent the product and whole-recipe gram amount that make up a recipe.

**Contract**: Reference a product id and store a positive gram amount. The model must support repeated products only through a deliberate rule; default should prevent duplicate product ingredients within one recipe if the aggregate can enforce it cleanly.

#### 5. Daily goal and meal-plan entities

**File**: `src/Jadlify.Domain/Planning/DailyMacroGoal.cs`

**Intent**: Represent the current user-owned daily macro target.

**Contract**: Represent the macro target value and its business rules. Persistence stores one current target per user for MVP. Do not add history/versioning.

**File**: `src/Jadlify.Domain/Planning/MealPlanEntry.cs`

**Intent**: Represent a recipe assigned to a selected day, meal type, and portion count.

**Contract**: Represent date, recipe id, meal type, and positive portion count. Persistence stores the owner. Meal type supports breakfast, lunch, dinner, and snack.

#### 6. Macro calculation service

**File**: `src/Jadlify.Domain/Nutrition/MacroCalculator.cs`

**Intent**: Make product-to-grams, recipe-total, recipe-per-serving, and meal-entry macro math deterministic and centrally testable.

**Contract**: Calculate macros proportionally from product values per 100g. Recipe totals sum whole-recipe ingredients; per-serving divides by declared portion count; meal-plan entries scale per-serving values by selected portions. Use `decimal` throughout and define rounding only at presentation boundaries later, not inside the core calculation unless required by tests.

#### 7. Domain tests

**File**: `tests/Jadlify.Domain.Tests/Nutrition/MacroCalculatorTests.cs`

**Intent**: Prove the PRD guardrail for deterministic and proportional macro calculations before any API/UI behavior exists.

**Contract**: Cover product per-100g proportional calculation, recipe total, recipe per-serving calculation, meal-entry portion scaling, zero/negative input rejection, and repeatability for the same inputs.

### Success Criteria:

#### Automated Verification:

- Domain-focused tests pass: `pwsh ./.scripts/verify-min.ps1 -TestProject tests/Jadlify.Domain.Tests/Jadlify.Domain.Tests.csproj`
- `rg -n "double|float" src/Jadlify.Domain src/Jadlify.Application src/Jadlify.Infrastructure -g "!**/bin/**" -g "!**/obj/**"` does not show macro-calculation numeric types.
- No placeholder `UnitTest1.cs` remains in `tests/Jadlify.Domain.Tests`.

#### Manual Verification:

- Review the model names and confirm the grams-only MVP decision is visible in type names or contracts.
- Review recipe ingredient wording and confirm ingredient quantities are for the whole recipe.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual review was successful before proceeding to the next phase.

---

## Phase 3: Application Ports, EF Implementations, And Handoff

### Overview

Expose persistence through feature-specific Application contracts, implement them in Infrastructure with user-scoped EF queries, and lock the foundation down with local verification and documentation.

### Changes Required:

#### 1. Product persistence port

**File**: `src/Jadlify.Application/Products/IProductRepository.cs`

**Intent**: Give future product handlers a focused contract for user-owned product persistence.

**Contract**: Define operations needed by S-02 without exposing `DbContext`: add, get/list by current user, update, delete with dependency restriction surfaced as a `Result` failure, and barcode lookup by owner if needed later. All read/write operations must be owner-scoped.

#### 2. Recipe persistence port

**File**: `src/Jadlify.Application/Recipes/IRecipeRepository.cs`

**Intent**: Give future recipe handlers a focused contract for recipe persistence and ingredient loading.

**Contract**: Define operations for add, get/list by owner, update, and delete with dependency restriction. Loading a recipe for calculation must include its ingredients and product nutrient values through an owner-scoped query.

#### 3. Planning persistence ports

**File**: `src/Jadlify.Application/Planning/IDailyMacroGoalRepository.cs`

**Intent**: Support the single-current-goal MVP decision without leaking storage details.

**Contract**: Define get/upsert current goal by owner. Do not expose goal history APIs.

**File**: `src/Jadlify.Application/Planning/IMealPlanRepository.cs`

**Intent**: Support day-plan persistence and future daily summary/shopping-list projections.

**Contract**: Define add/list/update/delete meal-plan entries by owner and date. Query contracts must be able to include recipe ingredients and products for deterministic calculation without cross-user joins.

#### 4. EF repository implementations

**File**: `src/Jadlify.Infrastructure/Persistence/Repositories/*.cs`

**Intent**: Implement Application ports through `JadlifyDbContext`.

**Contract**: Every query includes owner filtering using the persisted user subject. Implementations must never return a record from another user and must use `UserScope` or equivalent owner checks before returning loaded records. Delete methods must return deterministic `Result` failures when dependencies exist instead of silently cascading user data.

#### 5. Infrastructure tests

**File**: `tests/Jadlify.Infrastructure.Tests/Persistence/*.cs`

**Intent**: Prove EF mapping and repository contracts locally without live Supabase credentials.

**Contract**: Cover model configuration for owner columns, decimal precision, restrict delete behavior, and repository owner filtering. Use a local relational test strategy that runs under the repo scripts without requiring Supabase secrets, network, or Docker.

#### 6. Contract surface registry update

**File**: `docs/reference/contract-surfaces.md`

**Intent**: Record the new persistence and calculation surfaces future changes must reuse.

**Contract**: Add the DbContext, repository port names, grams-only quantity decision, whole-recipe ingredient quantity decision, macro-calculation service, and local verification rule. Preserve the existing Account Boundary section.

#### 7. Change lifecycle metadata

**File**: `context/changes/persistent-user-resources/change.md`

**Intent**: Keep the change folder aligned with the planning workflow.

**Contract**: Ensure frontmatter status is `planned` and `updated` is `2026-05-28`.

### Success Criteria:

#### Automated Verification:

- Full solution verification passes: `pwsh ./.scripts/verify-min.ps1`
- Infrastructure tests pass locally without live Supabase credentials: `pwsh ./.scripts/verify-min.ps1 -TestProject tests/Jadlify.Infrastructure.Tests/Jadlify.Infrastructure.Tests.csproj`
- Domain calculation tests pass: `pwsh ./.scripts/verify-min.ps1 -TestProject tests/Jadlify.Domain.Tests/Jadlify.Domain.Tests.csproj`
- Format check passes: `pwsh ./.scripts/format-min.ps1`
- Secret scan returns no real secrets or database URLs in source-controlled paths: `rg -n "SUPABASE_|service_role|JWT_SECRET|sb_secret_|postgresql://|Host=.*Password=" src tests context docs -g "!context/archive/**" -g "!**/bin/**" -g "!**/obj/**"`

#### Manual Verification:

- Review `docs/reference/contract-surfaces.md` and confirm later slices have one clear persistence/calculation contract to reuse.
- Confirm no files were written under `context/archive/`.
- Confirm generated migrations target Supabase Postgres but normal automated verification does not require live Supabase credentials.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual review was successful before marking the change implemented.

---

## Testing Strategy

### Unit Tests:

- `MacroNutrients` and `GramAmount` reject invalid negative or zero inputs where the business rule requires positive values.
- `MacroCalculator` calculates product contribution from per-100g values proportionally to grams.
- Recipe totals are the sum of whole-recipe ingredients.
- Recipe per-serving values divide whole-recipe totals by portion count.
- Meal-plan entry totals scale per-serving values by selected portion count.
- Same inputs always produce the same macro output.

### Integration Tests:

- EF model configuration includes required user owner columns for every user-owned table.
- Decimal properties have explicit precision.
- Dependency foreign keys use restrict/no-action semantics rather than silent cascade delete.
- Repository implementations filter reads/lists by owner.
- Repository delete methods return a controlled failure when dependent records exist.

### Manual Testing Steps:

1. Inspect `src/Jadlify.API/appsettings*.json` for accidental database or Supabase secrets.
2. Inspect project references and confirm Clean Architecture direction is unchanged.
3. Inspect `docs/reference/contract-surfaces.md` and confirm future slices should not invent parallel persistence or calculation contracts.
4. Review generated migration files and confirm there is no personal seed data.

## Performance Considerations

F-02 targets the PRD's small-data MVP profile: roughly up to 1000 products and 200 recipes per account. User-owned tables should have indexes that support owner-scoped list/detail lookups and day-plan lookups by `(user_id, date)`. Calculation remains in pure code for correctness and testability; later read models can optimize only if measured latency requires it.

## Migration Notes

This plan creates the initial EF migration artifact but does not require applying it to live Supabase during normal automated verification. Live migration application is a deployment/manual step that needs the real Supabase Postgres connection string supplied through user secrets or Azure App Service app settings.

If a future implementation changes unit support beyond grams-only, it should create a new change because that affects product validation, recipe calculations, shopping-list aggregation, and user-facing copy.

## References

- Roadmap F-02 planning guidance: `context/foundation/roadmap.md:82`
- Roadmap F-02 risk statement: `context/foundation/roadmap.md:90`
- Tech stack auth/data decision: `context/foundation/tech-stack.md:31`, `context/foundation/tech-stack.md:33`, `context/foundation/tech-stack.md:35`
- PRD deterministic calculation guardrail: `context/foundation/prd.md:59`, `context/foundation/prd.md:161`
- PRD current grams-only assumption and unit open question: `context/foundation/prd.md:147`, `context/foundation/prd.md:213`
- Existing account boundary contract registry: `docs/reference/contract-surfaces.md:7`, `docs/reference/contract-surfaces.md:18`
- Existing API composition/auth boundary: `src/Jadlify.API/Program.cs:15`, `src/Jadlify.API/Program.cs:19`, `src/Jadlify.API/Program.cs:53`, `src/Jadlify.API/Program.cs:71`
- Existing Application user id and scope guard: `src/Jadlify.Application/Identity/ApplicationUserId.cs:3`, `src/Jadlify.Application/Identity/UserScope.cs:18`
- Existing Infrastructure project shell: `src/Jadlify.Infrastructure/Jadlify.Infrastructure.csproj`
- Existing minimal verification script: `.scripts/verify-min.ps1`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` - <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: EF Core Persistence Infrastructure

#### Automated

- [x] 1.1 Infrastructure-focused verification passes — e2890e2
- [x] 1.2 API-focused verification passes after registration — e2890e2
- [x] 1.3 Secret scan returns no committed connection strings or Supabase secrets — e2890e2

#### Manual

- [x] 1.4 No real database connection string is committed — e2890e2
- [x] 1.5 Clean Architecture project reference direction is preserved — e2890e2

### Phase 2: MVP Resource Model And Deterministic Calculations

#### Automated

- [x] 2.1 Domain-focused tests pass — bf6b257
- [x] 2.2 Macro-calculation code does not use `double` or `float` — bf6b257
- [x] 2.3 No placeholder Domain `UnitTest1.cs` remains — bf6b257

#### Manual

- [x] 2.4 Grams-only MVP decision is visible in model contracts — bf6b257
- [x] 2.5 Recipe ingredient quantities are clearly whole-recipe quantities — bf6b257

### Phase 3: Application Ports, EF Implementations, And Handoff

#### Automated

- [x] 3.1 Full solution verification passes — 250dc57
- [x] 3.2 Infrastructure tests pass locally without live Supabase credentials — 250dc57
- [x] 3.3 Domain calculation tests pass — 250dc57
- [x] 3.4 Format check passes — 250dc57
- [x] 3.5 Secret scan returns no real secrets or database URLs — 250dc57

#### Manual

- [x] 3.6 Contract surface registry gives later slices one persistence/calculation contract — 250dc57
- [x] 3.7 No files were written under `context/archive/` — 250dc57
- [x] 3.8 Generated migrations target Supabase Postgres but normal verification does not require live Supabase — 250dc57
