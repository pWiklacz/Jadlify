# Follow-ups from implementation review (2026-06-02)

Deferred items from `/10x-impl-review` triage that were intentionally not fixed in this change.

## F1 — Bound the recipe list query (deferred, MVP-acceptable)

- **Where**: `src/Jadlify.Infrastructure/Persistence/Repositories/RecipeRepository.cs:53-57` (`ListAsync`)
- **What**: `ListAsync` loads ALL of the user's recipes with `.Include(r => r.Ingredients)` and no take cap, unlike the bounded `ProductRepository.ListAsync` (`ProductListBounds`). Not N+1, but the result set itself is unbounded.
- **Decision**: Accepted for MVP — per-user recipe counts are low at this stage; both review agents judged it acceptable. No user-facing impact now.
- **When to revisit**: If recipe volumes grow. Mirror `ProductListBounds` with a `take` cap and add pagination to the `RecipesPage` list UI (which does not currently paginate).
