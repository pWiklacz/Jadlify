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
- Delete operations protect deterministic calculations: deleting a product referenced by a recipe ingredient, or a recipe referenced by a meal-plan entry, returns an `ErrorType.Conflict` `Result` (`Product.InUse` / `Recipe.InUse`) instead of cascading user data. Cross-user or missing targets return `ErrorType.NotFound`.
- `MacroCalculator` (`src/Jadlify.Domain/Nutrition/MacroCalculator.cs`) is the single deterministic macro-math surface: product-amount, recipe-total, recipe-per-serving, and meal-entry calculations. Reuse it instead of recomputing macros; do not persist precomputed recipe/day totals in F-02.
- Quantities are grams-only for the MVP: `GramAmount` (`src/Jadlify.Domain/Nutrition/GramAmount.cs`) holds positive `decimal` grams, and macro values use `MacroNutrients` (`src/Jadlify.Domain/Nutrition/MacroNutrients.cs`) with `decimal` components. Recipe ingredient amounts (`RecipeIngredient.WholeRecipeAmount`) are whole-recipe quantities, not per serving. Adding non-gram units is a new change.

## Handoff Rules

- Supabase Auth access tokens map their `sub` claim to `ApplicationUserId`; inbound claim remapping stays disabled so code reads `sub` literally.
- ASP.NET Core API endpoints are the backend boundary for domain data. The browser may use Supabase for auth/session, but product, recipe, plan, goal, and shopping-list data must go through the API.
- `/health` is the only intentionally anonymous runtime endpoint. Future endpoints must rely on the fallback authenticated policy or state an explicit exception in their change plan.
- User-owned persistence in F-02 must store an owner `ApplicationUserId` value or equivalent persisted subject and pass it through `UserScope` before returning data.
