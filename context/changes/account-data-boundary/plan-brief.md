# Account Data Boundary — Plan Brief

> Full plan: `context/changes/account-data-boundary/plan.md`

## What & Why

This plan creates the backend account/data boundary that every future user-owned resource must use. It exists because the PRD's hardest guardrail is per-user isolation: products, recipes, plans, goals, and shopping lists from one user must never be visible to another user or anonymous traffic.

## Starting Point

The repo currently has a .NET 10 ASP.NET Core API skeleton with `/health`, scaffolded `/weatherforecast`, and `AddApplication()` already wired. Application now has a lightweight CQRS/mediator and validation pipeline, while SharedKernel owns `Result`, `Error`, `ErrorType`, and `ValidationError`. Supabase Auth and Supabase Postgres are selected in foundation docs, but no token verification, authorization middleware, current-user abstraction, or persistence layer exists yet.

## Desired End State

The API validates Supabase-issued JWT bearer access tokens, treats the literal `sub` claim as the stable application user id, and exposes a framework-independent current-user contract to Application and later Infrastructure code. `/health` remains anonymous for deployment smoke checks; future domain API routes require authentication by default. Contract tests prove missing, invalid, and cross-user contexts do not silently pass.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| F-01 scope | Foundation only | Keeps sign-in UI and persistence in their roadmap slices while unblocking every later user-owned resource. | Plan |
| Auth model | JWT bearer only | Matches Supabase Auth + ASP.NET Core API boundary from `tech-stack.md`. | Research / Plan |
| Failure behavior | Strict 401/403 | Makes missing/invalid identity distinct from authorization failures. | Plan |
| Testing depth | Contract tests | Provides user-scope proof before real domain tables exist. | Plan |
| Persistence | Defer EF/Npgsql to F-02 | Preserves the roadmap dependency boundary. | Roadmap / Plan |
| Secrets/config | Document keys only | Prevents committed Supabase secrets while giving implementers the required config shape. | Research / Plan |
| Smoke endpoint | No public `/api/me` | Avoids a temporary public API contract before the account flow exists. | Plan |
| Error handling | Use existing Result Pattern | Prevents a second authorization/error style beside SharedKernel `Result` and `Error`. | Repo / Plan |

## Scope

**In scope:**

- Application-layer `ApplicationUserId`, `ICurrentUser`, and user-scope guard.
- Result/Error authorization failure shape for cross-user denial.
- API JWT bearer authentication and authorization registration.
- Supabase auth configuration contract without real secret values.
- Removal or protection of scaffolded anonymous sample endpoints.
- Application/API tests for auth boundary and user-scope behavior.
- Contract-surface registry for load-bearing identity names.

**Out of scope:**

- Sign-up/sign-in UI or full account flow.
- EF Core, Npgsql, DbContext, migrations, and real tables.
- Product, recipe, goal, meal-plan, macro-summary, or shopping-list behavior.
- Public `/api/me` or dev-only auth smoke endpoint.
- Direct browser access to core domain tables.

## Architecture / Approach

The boundary flows from the API inward: ASP.NET Core validates Supabase bearer tokens, `HttpContextCurrentUser` maps the literal `sub` claim into `ApplicationUserId`, and Application-layer CQRS handlers/guards consume `ICurrentUser` without depending on ASP.NET Core or Supabase SDKs. User-scope denial uses the existing SharedKernel Result Pattern. Later persistence work will attach `ApplicationUserId` to user-owned tables and scope repositories/queries with this same contract.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Application User Context Contract | Framework-independent user id, current-user abstraction, Result-based scope guard, and authorization error shape. | Introducing a parallel error style instead of extending SharedKernel carefully. |
| 2. API JWT Authentication Boundary | JWT bearer auth, authorization defaults, current-user adapter, and config shape beside existing `AddApplication()`. | Misconfiguring Supabase JWT validation or leaving sample endpoints anonymous. |
| 3. Boundary Verification And Handoff Contracts | API auth tests, anonymous surface checks, contract registry, and F-02 handoff. | Tests becoming too synthetic if they do not exercise the real authorization policies. |

**Prerequisites:** Supabase project auth settings must be supplied through user secrets or Azure App Service app settings during implementation/testing; no real values belong in source.

**Estimated effort:** ~2-3 focused sessions across 3 phases.

## Open Risks & Assumptions

- The target path assumes Supabase JWT verification via asymmetric signing keys/JWKS; if the project still uses legacy shared-secret signing, key strategy must be resolved without committing JWT secrets.
- F-01 verifies the boundary with contracts and API tests, not real user-owned database rows; DB enforcement lands in F-02.
- Because no frontend exists yet, manual auth smoke testing is limited until the sign-in flow is implemented.
- If `ErrorType` needs a `Forbidden`/authorization value, it must be added without breaking existing validation behavior tests.

## Success Criteria (Summary)

- Future domain endpoints have a stable way to require and consume the authenticated `ApplicationUserId`.
- Anonymous or malformed-auth requests cannot access protected runtime API surface.
- No Supabase secrets, service keys, JWT secrets, or database connection strings are committed.
