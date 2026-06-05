# Contract Surfaces

This registry records names that future changes must reuse when building user-owned data flows. Do not create parallel identity or authorization contracts unless this document is updated with the replacement decision.

## Account Boundary

- `ApplicationUserId` (`src/Jadlify.Application/Identity/ApplicationUserId.cs`) is the application-layer value object for the stable Supabase user subject. It accepts any non-empty subject string and compares by value.
- `ICurrentUser` (`src/Jadlify.Application/Identity/ICurrentUser.cs`) is the only application-layer abstraction for the authenticated user. Handlers and repositories should depend on this contract instead of ASP.NET Core, Supabase SDK types, or raw claims.
- `HttpContextCurrentUser` (`src/Jadlify.API/Authentication/HttpContextCurrentUser.cs`) adapts `HttpContext.User` to `ICurrentUser` in the API layer. It reads the literal `sub` claim and fails explicitly when authentication or subject identity is missing.
- `SupabaseJwtOptions` (`src/Jadlify.API/Authentication/SupabaseJwtOptions.cs`) owns non-secret JWT validation setting names. Real issuer, metadata, audience overrides, keys, and connection strings stay in user secrets or hosting configuration.
- `UserScope` (`src/Jadlify.Application/Identity/UserScope.cs`) is the reusable same-user guard for future user-owned resources. Cross-user denial returns `Result` or `Result<T>` with `ErrorType.Forbidden` and `UserScope.Forbidden`.

## Resource Persistence and Calculation (F-02)

- `JadlifyDbContext` (`src/Jadlify.Infrastructure/Persistence/JadlifyDbContext.cs`) is the single EF Core/Npgsql data-access root for user-owned MVP resources (products, recipes, daily goals, meal-plan entries). It applies entity configurations from the Infrastructure assembly and must not depend on ASP.NET Core or Supabase SDK types. Its connection string name is `JadlifyDatabase`.
- The owner subject is persisted on every user-owned table as a required `user_id` text column (mapped as the `UserId` shadow property; see `src/Jadlify.Infrastructure/Persistence/PersistenceConstants.cs`). It stores the `ICurrentUser.UserId.Value` (the Supabase `sub`) — Domain entities deliberately do not carry `ApplicationUserId`; ownership is stamped and filtered at the repository/persistence boundary.
- Repository ports are the only contract future handlers depend on for persistence (never `DbContext` directly): `IProductRepository` (`src/Jadlify.Application/Products/`), `IRecipeRepository` (`src/Jadlify.Application/Recipes/`), `IDailyMacroGoalRepository` and `IMealPlanRepository` (`src/Jadlify.Application/Planning/`). Every read/write is implicitly owner-scoped; implementations (`src/Jadlify.Infrastructure/Persistence/Repositories/`) never return or mutate another user's records.
- Delete operations protect deterministic calculations at the recipe boundary: product deletion always succeeds for owned products because recipe ingredients carry historical snapshots, while deleting a recipe referenced by a meal-plan entry returns an `ErrorType.Conflict` `Result` (`Recipe.InUse`) instead of cascading user data. Cross-user or missing targets return `ErrorType.NotFound`.
- `MacroCalculator` (`src/Jadlify.Domain/Nutrition/MacroCalculator.cs`) is the single deterministic macro-math surface: product-amount, recipe-total, recipe-per-serving, and meal-entry calculations. Reuse it instead of recomputing macros; do not persist precomputed recipe/day totals in F-02.
- Quantities are grams-only for the MVP: `GramAmount` (`src/Jadlify.Domain/Nutrition/GramAmount.cs`) holds positive `decimal` grams, and macro values use `MacroNutrients` (`src/Jadlify.Domain/Nutrition/MacroNutrients.cs`) with `decimal` components. Recipe ingredient amounts (`RecipeIngredient.WholeRecipeAmount`) are whole-recipe quantities, not per serving. Adding non-gram units is a new change.

## Frontend Hosting and Session Probe (F-03)

- `GET /api/me` (`src/Jadlify.API/Program.cs`) is the authenticated session probe: it reads `ICurrentUser` and returns `200` with a `MeResponse` (`src/Jadlify.API/Session/MeResponse.cs`) body carrying the caller's `sub` (`ICurrentUser.UserId.Value`). It is the single read-only endpoint the SPA uses to confirm its bearer token reaches the backend — it persists nothing. Reuse it for "who am I" rather than adding a parallel identity endpoint.
- `/api/me` is intentionally **not** `AllowAnonymous`: it inherits the global fallback policy, so anonymous → `401`, authenticated-without-`sub` → `403`, valid token → `200`. New protected endpoints should follow the same pattern (rely on the fallback policy; live under `/api`).
- The SPA is served single-origin from the API's `wwwroot` (built in via the `dotnet publish` MSBuild target). Static assets serve before authentication, and `MapFallbackToFile("index.html")` is the one anonymous catch-all so unauthenticated visitors can load the app shell and reach the future login screen. All `/api/*` and `/health` routes match before the fallback and keep their own auth behavior; domain data still flows only through `/api/*`.

## Handoff Rules

- Supabase Auth access tokens map their `sub` claim to `ApplicationUserId`; inbound claim remapping stays disabled so code reads `sub` literally.
- Access tokens are validated through one asymmetric path: the API discovers Supabase's (ES256) public key via JWKS/OIDC under `SupabaseAuth:Authority`. Production discovers over HTTPS; local dev against a Supabase CLI stack sets `SupabaseAuth:RequireHttpsMetadata=false` because that stack's discovery endpoint is HTTP. There is no symmetric/shared-secret validation path.
- ASP.NET Core API endpoints are the backend boundary for domain data. The browser may use Supabase for auth/session, but product, recipe, plan, goal, and shopping-list data must go through the API.
- `/health` is the only intentionally anonymous runtime endpoint. Future endpoints must rely on the fallback authenticated policy or state an explicit exception in their change plan.
- User-owned persistence in F-02 must store an owner `ApplicationUserId` value or equivalent persisted subject and pass it through `UserScope` before returning data.

## Recipe Builder Contracts (S-03)

- `RecipeIngredient` (`src/Jadlify.Domain/Recipes/RecipeIngredient.cs`) snapshots `ProductId`, `ProductName`, `Per100Grams`, and `WholeRecipeAmount` at ingredient add/update time. Recipe calculations must use these snapshots, not a later reload of the source product.
- `/api/recipes` (`src/Jadlify.API/Recipes/RecipeEndpoints.cs`) exposes authenticated list/get/create/update/delete routes. Create and update accept whole-recipe ingredient grams; get/list return `RecipeResponse` with ingredient snapshots, total macros, and per-serving macros.
- `RecipeDto` / `RecipeIngredientDto` / `RecipeMacroSummaryDto` (`src/Jadlify.Application/Recipes/`) are the application-layer recipe read models. Future meal-plan and shopping-list slices should reuse these totals and snapshot semantics rather than creating parallel recipe response shapes.
- Product picker search uses `GET /api/products?search=&skip=&take=` and remains owner-scoped. Omitted query parameters keep the legacy product-list behavior within the bounded default page size.
- Future meal-plan and shopping-list calculations must flow through `MacroCalculator` against recipe ingredient snapshots so deleted or edited products do not change saved recipe totals.

## Daily Goals and Meal Plan Contracts (S-04)

- The daily macro goal is a singleton resource, not a collection: `GET /api/daily-goal` returns `200` with a `DailyGoalResponse` (calories/protein/fat/carbohydrates) or a literal JSON `null` when no goal is configured; `PUT /api/daily-goal` upserts the current goal and returns `204`. There is one current goal per user with no id, history, or versioning — do not add goal identity, named profiles, or history endpoints without updating this decision (`src/Jadlify.API/Planning/DailyGoalEndpoints.cs`, `src/Jadlify.API/Planning/DailyGoalContracts.cs`).
- Meal-plan entries are one-day CRUD under `/api/meal-plan`: `GET /api/meal-plan?date=yyyy-MM-dd` lists the selected day's entries, `POST /api/meal-plan` creates one and returns `201` with a `Location` header, `PUT /api/meal-plan/{id:guid}` returns `204`, and `DELETE /api/meal-plan/{id:guid}` returns `204`. The slice edits a single selected day at a time; there is no weekly or multi-day view (`src/Jadlify.API/Planning/MealPlanEndpoints.cs`, `src/Jadlify.API/Planning/MealPlanContracts.cs`).
- Meal type is the wire name `Breakfast`, `Lunch`, `Dinner`, or `Snack`, parsed at the API boundary into `MealType` (`src/Jadlify.Domain/Planning/MealType.cs`); an unknown or numeric name is a `400` field error rather than a defaulted entry. Dates are ISO `DateOnly` (`yyyy-MM-dd`).
- Portions are positive integers (1..`PlanningValidationBounds.MaxPortions`); there are no fractional portions. Goal validation shares the same bounds class (`src/Jadlify.Application/Planning/PlanningValidationBounds.cs`): calories must be strictly positive (≤ `MaxDailyCalories`), and protein/fat/carbohydrate grams are non-negative (≤ `MaxDailyMacroGrams`).
- Duplicate entries are allowed by design — the same date + meal type + recipe may appear more than once. Do not introduce hidden uniqueness rules.
- Entries reference the **current** recipe by id rather than snapshotting recipe details. `MealPlanEntryDto` / `MealPlanEntryResponse` resolve `RecipeName` from the owner-scoped recipe at read time via `IRecipeRepository.ListByIdsAsync` — one batched lookup per selected day, never a per-entry call (`src/Jadlify.Application/Planning/MealPlanEntryDto.cs`, `src/Jadlify.Application/Recipes/IRecipeRepository.cs`). Editing an entry changes only meal type and portions, never its date or recipe; changing those means delete and re-add.
- S-04 stops at storage and display: no day-level macro totals, goal deltas, progress bars, or shopping-list output. **S-05 must compute day totals from each meal entry plus its current recipe's ingredient snapshots through `MacroCalculator.ForMealEntry(MealPlanEntry, Recipe)` (`src/Jadlify.Domain/Nutrition/MacroCalculator.cs`)** rather than persisting precomputed day totals or building a parallel macro path. S-06 shopping-list aggregation reuses these same entries and recipe snapshots.

## Daily Macro Summary Contracts (S-05)

- `GET /api/meal-plan/summary?date=yyyy-MM-dd` is the read-only summary endpoint for the selected day. It inherits the authenticated fallback policy and returns `DailyMacroSummaryResponse` from `src/Jadlify.API/Planning/MealPlanContracts.cs`: `date`, per-entry macro contributions keyed by `entryId`, `total`, nullable `goal`, and nullable signed `remaining`.
- Per-entry contributions and the day total use the same four-field macro shape (`calories`, `protein`, `fat`, `carbohydrates`) as recipe and goal read models. The S-04 `GET /api/meal-plan?date=yyyy-MM-dd` list contract is unchanged and remains display-only; summary consumers must call `/summary` instead of adding macro fields to the entry list.
- Day totals are computed on read only through `MacroCalculator.DayTotal(...)` and `MacroCalculator.ForMealEntry(MealPlanEntry, Recipe)` against current recipe ingredient snapshots. Do not persist precomputed day totals, per-entry totals, or goal deltas.
- `remaining` is `goal - consumed` per field and may be negative when the selected day is over goal. It is represented by `MacroRemainingDto`, not `MacroNutrients`, because `MacroNutrients` is non-negative by construction.
- `IRecipeRepository.ListByIdsWithIngredientsAsync(...)` is the owner-scoped, ingredient-loaded batch read for summary macro math. Keep `IRecipeRepository.ListByIdsAsync(...)` as the display-only batch read for meal-plan entry names.
- A missing daily goal is normal: `goal` and `remaining` are both `null`, while `total` and per-entry macros still return. A defensively missing recipe contributes zero rather than failing the whole summary; normal recipe deletion is blocked while meal-plan entries reference it.
