# Account Data Boundary Implementation Plan

## Overview

Implement the backend account boundary that every later user-owned resource will depend on. This change wires ASP.NET Core JWT bearer authentication for Supabase-issued access tokens, defines a current-user contract in the application layer, and adds contract tests proving that missing, invalid, and cross-user contexts cannot silently pass through.

## Current State Analysis

Jadlify has the selected auth/data platform and a now-building clean architecture skeleton, but the runtime code does not yet enforce identity or per-user access. The API currently registers Application services, exposes `/health`, and still exposes the scaffolded `/weatherforecast` endpoint without authentication. The Application layer now has a lightweight CQRS/mediator pipeline with validation behavior, and SharedKernel now owns the `Result`, `Error`, `ErrorType`, and `ValidationError` primitives that new application contracts must reuse.

## Desired End State

After this plan is complete, the API has a strict bearer-token authentication boundary, `/health` remains the only intentionally anonymous runtime endpoint, and future domain endpoints can require an authenticated `ApplicationUserId` through a stable application-layer abstraction. The implementation is verifiable by focused unit/integration tests plus a full solution build/test run.

### Key Discoveries:

- PRD guardrail requires that products, recipes, plans, goals, and lists from one user are never visible to another user or anonymous traffic: `context/foundation/prd.md:58`, `context/foundation/prd.md:156`.
- Operational privacy requires auth secrets and tokens to stay out of logs and diagnostics: `context/foundation/prd.md:157`.
- Tech stack says Supabase Auth issues sessions, the React frontend may use Supabase only for auth/session, and domain behavior must go through ASP.NET Core API: `context/foundation/tech-stack.md:31`.
- API must validate Supabase JWTs, treat `sub` as the stable user id, and enforce user scope in application/data access: `context/foundation/tech-stack.md:33`.
- Roadmap F-01 explicitly calls for Supabase JWT validation, a backend current-user abstraction, no browser access to core tables, and tests/contracts proving user-scope isolation: `context/foundation/roadmap.md:65`, `context/foundation/roadmap.md:66`, `context/foundation/roadmap.md:67`, `context/foundation/roadmap.md:68`, `context/foundation/roadmap.md:69`.
- Code baseline has OpenAPI, `AddApplication()`, `/health`, and scaffolded `/weatherforecast`, but no auth middleware or protected surface: `src/Jadlify.API/Program.cs:8`, `src/Jadlify.API/Program.cs:20`, `src/Jadlify.API/Program.cs:27`.
- SharedKernel now provides the result/error surface that application authorization contracts should use: `src/Jadlify.SharedKernel/Result.cs:5`, `src/Jadlify.SharedKernel/Error.cs:3`, `src/Jadlify.SharedKernel/ErrorType.cs:3`, `src/Jadlify.SharedKernel/ValidationError.cs:3`.
- Application now has a CQRS/mediator and validation pipeline registered through `AddApplication()`: `src/Jadlify.Application/DependencyInjection.cs:11`, `src/Jadlify.Application/DependencyInjection.cs:21`, `src/Jadlify.Application/Common/Mediator/IMediator.cs:5`, `src/Jadlify.Application/Common/Behaviours/ValidationBehavior.cs:8`.
- Existing test shape includes xUnit plus Application behavior tests that already exercise `Result` failures: `tests/Jadlify.Application.Tests/Common/Behaviours/ValidationBehaviorTests.cs:8`, `tests/Jadlify.Application.Tests/Common/Behaviours/ValidationBehaviorTests.cs:49`.

## What We're NOT Doing

- Building the sign-up/sign-in UI or account flow; that belongs to `account-sign-in-flow`.
- Adding EF Core, Npgsql, DbContext, migrations, or real user-owned tables; that belongs to `persistent-user-resources`.
- Adding product, recipe, goal, meal-plan, macro-summary, or shopping-list behavior.
- Adding a public `/api/me` or dev-only auth smoke endpoint.
- Committing Supabase secrets, JWT secrets, publishable keys, service keys, or database connection strings.
- Relying on direct browser-to-table Supabase access for MVP domain data.

## Implementation Approach

Build the boundary from inside out while reusing the existing Application and SharedKernel primitives. First define the application-layer user id and current-user contracts that later CQRS handlers/repositories can consume without depending on ASP.NET Core. Then wire ASP.NET Core authentication and authorization around those contracts through the existing `AddApplication()` registration path, keeping `/health` anonymous and removing or protecting scaffolded sample endpoints. Finally, replace placeholder tests with contract tests for user id validation, required authenticated context, Result-based user-scope failures, 401/403 behavior, and the absence of unintended anonymous API surface.

## Critical Implementation Details

Supabase's current JWT verification guidance supports JWKS verification for projects using asymmetric signing keys; the JWKS endpoint does not expose keys for projects still using only a shared secret. The implementation must therefore configure the API for JWKS/asymmetric verification as the target path and document the required Supabase auth settings without committing secret material. If the actual Supabase project is still on legacy HS256 at implementation time, do not add the JWT secret to source-controlled config; treat key migration or server-side Auth validation as a deployment prerequisite.

ASP.NET Core must keep inbound claim mapping disabled for bearer auth so the code reads the literal `sub` claim. Middleware order matters: authentication must run before authorization, and `/health` must be explicitly anonymous if a fallback authenticated policy is used.

Application-level authorization failures must not introduce a second error pattern. Use the existing SharedKernel `Result` / `Result<T>` flow for user-scope checks, extending `ErrorType` and `Error` with explicit authorization semantics only if the current `Failure`, `Problem`, `NotFound`, and `Conflict` values cannot represent 403-style denial cleanly.

## Phase 1: Application User Context Contract

### Overview

Create the application-layer identity primitives that later features use to bind data access to the authenticated user.

### Changes Required:

#### 1. Application identity primitives

**File**: `src/Jadlify.Application/Identity/ApplicationUserId.cs`

**Intent**: Represent the stable Supabase user id from the JWT `sub` claim as an explicit application value instead of passing raw strings through handlers and repositories.

**Contract**: Add an immutable `ApplicationUserId` type that accepts non-empty, non-whitespace string values and compares by value. It must not assume a specific Supabase UUID/string format beyond being a stable non-empty subject.

#### 2. Current user abstraction

**File**: `src/Jadlify.Application/Identity/ICurrentUser.cs`

**Intent**: Give application services and future repository contracts a framework-independent way to access the authenticated user's id.

**Contract**: Define an `ICurrentUser` abstraction exposing the required authenticated `ApplicationUserId`. The contract should fail explicitly when no authenticated user is available rather than returning nullable ids that later code might ignore.

#### 3. User-scope guard

**File**: `src/Jadlify.Application/Identity/UserScope.cs`

**Intent**: Provide a reusable contract for checking that a user-owned record or request belongs to the current user before later domain behavior is implemented.

**Contract**: Add a small guard or policy helper that compares an owner `ApplicationUserId` to the current `ApplicationUserId` and returns a deterministic `Result` / `Result<T>` failure when they differ. The helper must not perform database access and must not throw for expected cross-user denial.

#### 4. Authorization error surface

**File**: `src/Jadlify.SharedKernel/ErrorType.cs`

**Intent**: Keep user-scope denial aligned with the existing Result Pattern instead of encoding authorization failures as generic exceptions or ad hoc strings.

**Contract**: If existing `ErrorType` values cannot cleanly represent user-scope denial, add an explicit authorization-oriented value such as `Forbidden`. Do not rename existing values used by validation tests.

**File**: `src/Jadlify.SharedKernel/Error.cs`

**Intent**: Provide a reusable factory for application authorization failures if a new error type is added.

**Contract**: Add a matching factory such as `Forbidden(code, description)` only when `ErrorType` gains that value. Keep `Error.None` and existing factories backwards compatible.

#### 5. Application contract tests

**File**: `tests/Jadlify.Application.Tests/Identity/ApplicationUserIdTests.cs`

**Intent**: Replace the scaffolded placeholder tests with tests for the identity primitives that later features will depend on.

**Contract**: Cover non-empty validation, value equality, and rejection of null/empty/whitespace ids.

**File**: `tests/Jadlify.Application.Tests/Identity/UserScopeTests.cs`

**Intent**: Prove the application-level user-scope contract denies cross-user access before there are real user-owned tables.

**Contract**: Cover same-user allow behavior and different-user deny behavior through the existing `Result` / `Result<T>` flow. If a new authorization `ErrorType` is added, assert it explicitly.

### Success Criteria:

#### Automated Verification:

- Application identity tests pass: `dotnet test tests/Jadlify.Application.Tests/Jadlify.Application.Tests.csproj -m:1 -p:UseSharedCompilation=false --verbosity minimal`
- Application project builds through the solution: `dotnet build Jadlify.slnx -m:1 -p:UseSharedCompilation=false --verbosity minimal`
- User-scope denial uses the SharedKernel Result Pattern and does not introduce a second error abstraction
- No placeholder `UnitTest1.cs` remains in `tests/Jadlify.Application.Tests`

#### Manual Verification:

- Review the Application identity namespace and confirm it has no ASP.NET Core, Supabase client, EF Core, or Infrastructure dependency.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual review was successful before proceeding to the next phase.

---

## Phase 2: API JWT Authentication Boundary

### Overview

Wire ASP.NET Core authentication/authorization to Supabase JWTs and adapt `HttpContext.User` into the application-layer current-user contract.

### Changes Required:

#### 1. API auth package and project references

**File**: `src/Jadlify.API/Jadlify.API.csproj`

**Intent**: Add the framework package needed for JWT bearer validation and make API registration code compile against the application-layer identity contracts.

**Contract**: Add `Microsoft.AspNetCore.Authentication.JwtBearer` with a version aligned to the existing ASP.NET Core package family. Preserve the existing API references and do not add duplicate Application/CQRS registration paths.

#### 2. Supabase auth options

**File**: `src/Jadlify.API/Authentication/SupabaseJwtOptions.cs`

**Intent**: Centralize the non-secret configuration keys required to validate Supabase access tokens.

**Contract**: Define an options type for Supabase auth configuration such as authority/issuer, audience, JWKS metadata address, and required subject claim name. The options must not contain actual secrets.

#### 3. Current user implementation

**File**: `src/Jadlify.API/Authentication/HttpContextCurrentUser.cs`

**Intent**: Translate the authenticated ASP.NET Core principal into the application-layer `ICurrentUser` contract.

**Contract**: Read the literal `sub` claim from `HttpContext.User` after authentication. Missing or empty `sub` must fail explicitly and must not fall back to a fake user id.

#### 4. Authentication and authorization registration

**File**: `src/Jadlify.API/Program.cs`

**Intent**: Configure strict JWT bearer auth and make authenticated access the default for future endpoints while keeping health checks public.

**Contract**: Register JWT bearer authentication, authorization policies, `ICurrentUser`, and the required middleware order alongside the existing `builder.Services.AddApplication()` call. Configure token validation to validate signature, issuer, audience, expiration, and signing key; keep inbound claim mapping disabled so `sub` remains `sub`. `/health` remains anonymous. The scaffolded `/weatherforecast` endpoint must be removed or protected so it is not an accidental anonymous API surface.

#### 5. Configuration documentation placeholders

**File**: `src/Jadlify.API/appsettings.json`

**Intent**: Make the required configuration shape discoverable without committing secrets.

**Contract**: Add only non-secret placeholder keys or an empty section for Supabase auth settings. Real Supabase URL, JWKS metadata address, audience, publishable key, service key, JWT secret, and DB connection strings must remain in user secrets or Azure App Service settings.

### Success Criteria:

#### Automated Verification:

- API tests pass: `dotnet test tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj -m:1 -p:UseSharedCompilation=false --verbosity minimal`
- Full solution builds: `dotnet build Jadlify.slnx -m:1 -p:UseSharedCompilation=false --verbosity minimal`
- `rg -n "weatherforecast|WeatherForecast" src tests -g "!**/bin/**" -g "!**/obj/**"` returns no live scaffolded runtime endpoint unless it is explicitly protected in tests.

#### Manual Verification:

- Review `src/Jadlify.API/appsettings*.json` and confirm no Supabase secret, JWT secret, service key, or database connection string was committed.
- Review `Program.cs` and confirm `/health` is the only intentionally anonymous runtime endpoint.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual review was successful before proceeding to the next phase.

---

## Phase 3: Boundary Verification And Handoff Contracts

### Overview

Lock the boundary down with API-level tests and handoff notes that tell future slices exactly how to consume the current-user contract.

### Changes Required:

#### 1. API auth test host

**File**: `tests/Jadlify.API.Tests/Authentication/AuthBoundaryTests.cs`

**Intent**: Prove API behavior for missing, invalid, and valid authenticated contexts without requiring live Supabase credentials.

**Contract**: Use a test authentication scheme or controlled test token validation path that exercises the same authorization policies and current-user extraction contract. Cover anonymous requests returning 401 for protected endpoints, authenticated requests with missing `sub` being denied, and valid `sub` becoming the expected `ApplicationUserId`.

#### 2. No accidental anonymous surface test

**File**: `tests/Jadlify.API.Tests/Authentication/AnonymousSurfaceTests.cs`

**Intent**: Prevent future scaffolding or sample endpoints from bypassing the default auth boundary.

**Contract**: Assert that `/health` is reachable anonymously and that any non-health API route added in this change requires authentication. If endpoint metadata inspection is simpler than HTTP probing, use that, but the test must fail when a new anonymous runtime endpoint is introduced without an explicit decision.

#### 3. Contract surface registry

**File**: `docs/reference/contract-surfaces.md`

**Intent**: Record the load-bearing auth/user-scope names that F-02 and later slices must reuse instead of inventing parallel identity contracts.

**Contract**: Create the `docs/reference/` folder if absent and register `ApplicationUserId`, `ICurrentUser`, `UserScope`, the Supabase `sub` claim mapping, the Result/Error authorization failure shape, and the rule that domain data is accessed through the ASP.NET Core API rather than direct browser-to-table paths.

#### 4. Change status

**File**: `context/changes/account-data-boundary/change.md`

**Intent**: Keep the change lifecycle aligned with the written implementation plan.

**Contract**: Ensure frontmatter status is `planned` and `updated` remains `2026-05-27`.

### Success Criteria:

#### Automated Verification:

- API auth boundary tests pass: `dotnet test tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj -m:1 -p:UseSharedCompilation=false --verbosity minimal`
- Application identity tests pass: `dotnet test tests/Jadlify.Application.Tests/Jadlify.Application.Tests.csproj -m:1 -p:UseSharedCompilation=false --verbosity minimal`
- Full solution tests pass: `dotnet test Jadlify.slnx -m:1 -p:UseSharedCompilation=false --verbosity minimal`
- Build remains green with the current CQRS/Result baseline: `dotnet build Jadlify.slnx --no-restore -m:1 -p:UseSharedCompilation=false --verbosity minimal`
- Secret scan returns no committed Supabase secrets: `rg -n "SUPABASE_|service_role|JWT_SECRET|sb_secret_|postgresql://" src tests context -g "!context/archive/**" -g "!**/bin/**" -g "!**/obj/**"` has no real secret values.

#### Manual Verification:

- Review the plan and brief handoff and confirm F-02 can build persistence without re-deciding the auth boundary.
- Confirm no files were written under `context/archive/`.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual review was successful before marking the change implemented.

---

## Testing Strategy

### Unit Tests:

- `ApplicationUserId` rejects invalid ids and compares valid ids by value.
- `ICurrentUser` implementation fails explicitly when the authenticated principal is missing a usable `sub`.
- `UserScope` allows same-user access and denies different-user access deterministically through the SharedKernel Result Pattern.
- Any new authorization `ErrorType` / `Error` factory is covered without breaking existing validation behavior tests.

### Integration Tests:

- Anonymous requests to protected non-health endpoints receive 401.
- Authenticated requests missing required user identity receive a controlled denial.
- Valid authenticated context maps `sub` to `ApplicationUserId` without claim remapping.
- `/health` remains anonymous and stable for Azure/App Service smoke checks.

### Manual Testing Steps:

1. Inspect `src/Jadlify.API/appsettings*.json` for accidental secrets.
2. Inspect `Program.cs` to confirm `UseAuthentication()` precedes `UseAuthorization()`.
3. Confirm `/health` is the only intentionally anonymous runtime endpoint.
4. Confirm Application identity contracts have no dependency on ASP.NET Core, Infrastructure, EF Core, or Supabase SDKs.

## Performance Considerations

JWT validation should use standard ASP.NET Core bearer middleware and remote metadata/JWKS caching rather than per-request custom network calls. This keeps F-01 within the PRD's low-QPS MVP profile and avoids adding latency to every future domain endpoint.

## Migration Notes

No database migration is part of this change. Supabase project configuration is a runtime/deployment prerequisite: the implementation expects Supabase auth settings to be supplied through local user secrets and Azure App Service application settings. If the project still uses legacy shared-secret signing for user access tokens, resolve key strategy before production use rather than committing JWT secrets.

## References

- PRD guardrail: `context/foundation/prd.md:58`
- PRD data isolation and operational privacy NFRs: `context/foundation/prd.md:156`, `context/foundation/prd.md:157`
- Tech stack auth/data decision: `context/foundation/tech-stack.md:31`, `context/foundation/tech-stack.md:33`, `context/foundation/tech-stack.md:35`
- Roadmap F-01 planning guidance: `context/foundation/roadmap.md:65`, `context/foundation/roadmap.md:66`, `context/foundation/roadmap.md:67`, `context/foundation/roadmap.md:68`, `context/foundation/roadmap.md:69`
- API baseline: `src/Jadlify.API/Program.cs:8`, `src/Jadlify.API/Program.cs:20`, `src/Jadlify.API/Program.cs:27`
- Current Application/CQRS baseline: `src/Jadlify.Application/DependencyInjection.cs:11`, `src/Jadlify.Application/Common/Mediator/IMediator.cs:5`, `src/Jadlify.Application/Common/Behaviours/ValidationBehavior.cs:8`
- Current Result/Error baseline: `src/Jadlify.SharedKernel/Result.cs:5`, `src/Jadlify.SharedKernel/Error.cs:3`, `src/Jadlify.SharedKernel/ErrorType.cs:3`
- Microsoft JWT bearer guidance: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
- Supabase JWT verification guidance: https://supabase.com/docs/guides/auth/jwts
- Supabase signing key guidance: https://supabase.com/docs/guides/auth/signing-keys

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` - <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Application User Context Contract

#### Automated

- [ ] 1.1 Application identity tests pass
- [ ] 1.2 Application project builds through the solution
- [ ] 1.3 User-scope denial uses SharedKernel Result Pattern
- [ ] 1.4 No placeholder Application UnitTest1 remains

#### Manual

- [ ] 1.5 Application identity namespace has no web, Supabase, EF Core, or Infrastructure dependency

### Phase 2: API JWT Authentication Boundary

#### Automated

- [ ] 2.1 API tests pass
- [ ] 2.2 Full solution builds
- [ ] 2.3 Scaffolded weather endpoint is removed or explicitly protected

#### Manual

- [ ] 2.4 No Supabase secret or database connection string is committed
- [ ] 2.5 `/health` is the only intentionally anonymous runtime endpoint

### Phase 3: Boundary Verification And Handoff Contracts

#### Automated

- [ ] 3.1 API auth boundary tests pass
- [ ] 3.2 Application identity tests pass
- [ ] 3.3 Full solution tests pass
- [ ] 3.4 Build remains green with current CQRS/Result baseline
- [ ] 3.5 Secret scan has no real secret values

#### Manual

- [ ] 3.6 F-02 can build persistence without re-deciding the auth boundary
- [ ] 3.7 No files were written under `context/archive/`
