# Persistent User Resources - Plan Brief

> Full plan: `context/changes/persistent-user-resources/plan.md`

## What & Why

This change creates the persistence and deterministic-calculation foundation for Jadlify's user-owned MVP resources. It gives later product, recipe, meal-plan, daily-summary, and shopping-list slices stable data and calculation contracts without building their API/UI behavior yet.

## Starting Point

F-01 has already implemented the account boundary: Supabase JWT validation, `ApplicationUserId`, `ICurrentUser`, `UserScope`, and a contract registry. What is missing is EF Core/Npgsql persistence, migrations, a user-owned resource schema, repository ports, and central macro calculation code.

## Desired End State

The solution has a local-testable EF Core persistence layer for the first daily flow: products, recipes, recipe ingredients, current daily goals, and meal-plan entries. Every persisted user-owned table carries the authenticated user id, all reads/writes are backend-scoped, and macro calculations are deterministic `decimal` code based on values per 100g.

## Key Decisions Made

| Decision | Choice | Why |
| --- | --- | --- |
| F-02 scope | Thin MVP schema | Stabilizes contracts for S-02..S-06 without building endpoints or UI. |
| Units | Grams only | Matches the PRD and keeps shopping-list aggregation deterministic. |
| Recipe ingredient quantity | Whole recipe | Fits meal prep and makes per-serving math explicit. |
| Ownership enforcement | App-owned user scoping | Matches backend-only data access; RLS remains optional defense in depth. |
| Delete behavior | Restrict dependent deletes | Avoids silent loss of recipes or plan data. |
| Calculation location | Domain/application service on `decimal` | Keeps macro math deterministic and unit-testable. |
| Data access contract | Feature-specific Application ports | Preserves Clean Architecture and avoids leaking `DbContext` into handlers. |
| Verification | Local contract checks | Keeps normal agent verification stable without live Supabase secrets. |

## Scope

**In scope:**

- EF Core/Npgsql persistence registration and `JadlifyDbContext`.
- Initial migration artifact for the MVP resource graph.
- Products, recipes, recipe ingredients, current daily goals, and meal-plan entries.
- Grams-only quantity contracts and deterministic macro calculation.
- Application repository ports plus Infrastructure EF implementations.
- Local model/repository/calculation tests and secret scanning.
- Contract registry update for future slices.

**Out of scope:**

- Product/recipe/goal/plan/shopping-list endpoints.
- React UI, sign-in UI, barcode lookup, or shopping-list screen.
- Direct browser-to-table Supabase access.
- Primary reliance on Supabase RLS.
- `ml` / `unit` support, goal history, or persisted generated shopping lists.
- Live Supabase migration application during automated verification.

## Architecture / Approach

`Domain` owns resource models and pure macro math without depending on Application identity types. `Application` owns feature-specific repository ports and consumes `ApplicationUserId` / `UserScope`. `Infrastructure` owns EF Core mappings, owner-subject columns, migrations, DbContext, and repository implementations. `API` remains the composition root: it registers Application, Infrastructure, authentication, and authorization, but F-02 adds no domain endpoints.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. EF Core Persistence Infrastructure | EF/Npgsql packages, DbContext, DI, config shape, migration path. | Accidentally committing secrets or bending project references. |
| 2. MVP Resource Model And Deterministic Calculations | Thin domain model plus grams-only macro calculator and tests. | Ambiguous quantity semantics or non-deterministic numeric types. |
| 3. Application Ports, EF Implementations, And Handoff | Feature ports, EF repositories, local verification, contract docs. | Cross-user query leaks or verification depending on live Supabase. |

**Prerequisites:** F-01 account boundary remains in place and `docs/reference/contract-surfaces.md` remains the identity contract source.
**Estimated effort:** ~3 focused implementation sessions across 3 phases.

## Open Risks & Assumptions

- The plan assumes grams-only remains acceptable for MVP despite real products often using ml or units.
- Restrict-delete behavior needs later user-facing error copy in product/recipe slices.
- Live Supabase migration application is intentionally manual and may reveal provider/environment configuration issues not covered by local verification.

## Success Criteria (Summary)

- Future slices can persist and query user-owned resources without re-deciding auth, ownership, or calculation contracts.
- Macro calculations are repeatable and proportional to product values per 100g.
- Normal verification uses repo scripts and does not require real Supabase secrets or network access.
