# Recipe Builder With Macro Calculation - Plan Brief

> Full plan: `context/changes/recipe-builder-with-macro-calculation/plan.md`

## What & Why

Build roadmap slice S-03: a signed-in user can create recipes from their own products and see deterministic macro totals for the whole recipe and per serving. This is the first user-visible test of Jadlify's core promise that planned meals can be composed from product gram amounts and trusted macro math.

## Starting Point

Auth, per-user persistence, product CRUD, and the responsive app shell already exist. The repo also has preliminary `Recipe`, `RecipeIngredient`, `IRecipeRepository`, EF mapping, and `MacroCalculator` types, but `/recipes` is still a placeholder and there are no recipe application handlers, API endpoints, or frontend builder.

## Desired End State

The `/recipes` route lists the user's recipes and opens a builder where the user searches products, adds ingredient gram amounts, sees live total/per-serving macros, saves complete recipes, edits them, and deletes unused ones. Recipe ingredients store a product snapshot at add-time, so recipe totals remain stable if a source product is later edited or deleted.

## Key Decisions Made

| Decision | Choice | Why |
| --- | --- | --- |
| Scope | Full S-03 vertical: domain, app, persistence, API, UI, tests | Delivers a usable recipe builder, not only backend plumbing. |
| Ingredient source | Existing current-user products | Preserves per-user isolation and uses the product catalog as source of truth at selection time. |
| Historical product data | Snapshot product name + per-100g macros into recipe ingredients | Required by S-02's product-delete policy and keeps recipe totals stable. |
| Quantity model | Whole-recipe grams + recipe portions | Matches F-02 contract and keeps per-serving math explicit. |
| Macro feedback | Live UI preview, backend authoritative on save | Gives fast UX while keeping persisted results validated server-side. |
| Save mode | Only complete valid recipes | Avoids draft state and keeps the domain model simple. |
| Invalid products | Reject save when missing or cross-user | Enforces the PRD data-isolation guardrail. |
| Product picker | Search with capped API query and inline add-product flow | Handles larger catalogs and lets users recover when a product is missing. |
| Testing | Full vertical coverage | The feature spans math, ownership, API contracts, and UI state. |

## Scope

**In scope:**

- Recipe aggregate and ingredient snapshot contract.
- Recipe create/list/get/update/delete application use-cases and validators.
- EF migration and repository update for ingredient snapshots.
- `GET/POST/PUT/DELETE /api/recipes` endpoints.
- Product list search parameters for the recipe picker.
- React recipes page, builder modal, product picker, live macro preview, delete confirmation.
- Reuse or adapt existing product modal so a missing product can be added from the builder.
- Domain, application, infrastructure, API, and frontend tests.

**Out of scope:**

- Meal plans, daily goals, daily macro summary, and shopping list generation.
- Non-gram units, unit conversion, recipe tags, images, steps, public sharing, and AI generation.
- Persisted precomputed totals or direct browser access to database tables.

## Architecture / Approach

Build bottom-up through the existing Clean Architecture boundaries. Domain owns recipe math and snapshot semantics, Application owns CQRS use-cases and owner-scoped product validation, Infrastructure owns EF mapping/repository behavior, API maps Minimal API routes through the mediator, and React consumes those routes with react-query.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Domain And Application Recipe Contracts | Snapshot-capable recipe model, DTOs, commands, queries, validators | Missing snapshot semantics would break product-delete history. |
| 2. Persistence, Product Search, And Recipe API | Migration, complete repository update, product search, `/api/recipes` | Cross-user product ids must fail safely. |
| 3. React Recipe Builder UI | Recipes page, builder, live macro preview, inline add-product flow | Preserving draft state while adding a missing product. |
| 4. Vertical Verification And Handoff | Cross-layer tests, smoke checks, contract docs | Later meal-plan slices depend on stable recipe contracts. |

**Prerequisites:** S-02 product catalog is in place; local auth/session and API token flow work.
**Estimated effort:** ~4 focused sessions across 4 phases.

## Open Risks & Assumptions

- Existing local recipe data is not user-visible yet; the snapshot migration can be strict or backfilled from products if needed.
- Product search should stay capped; full catalog search/sort UX beyond the picker is not part of this slice.
- The UI preview must match backend math, but backend calculation remains authoritative.

## Success Criteria (Summary)

- A user can create, edit, list, view, and delete recipes made from their own products.
- Whole-recipe and per-serving macro totals match manual 100g proportional calculation.
- Recipes cannot use another user's products, and recipe totals remain stable after source product edits/deletes.
