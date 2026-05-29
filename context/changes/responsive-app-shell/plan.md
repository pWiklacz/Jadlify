# Responsive App Shell Implementation Plan

## Overview

Stand up Jadlify's first frontend: a Vite + React + TypeScript single-page app (styled with Tailwind CSS), built into the ASP.NET Core API's `wwwroot` and served single-origin by the existing API host. The shell provides authentication *plumbing* — a Supabase JS session, a Bearer-attaching API client, and a route guard — plus a responsive app-bar/drawer layout with placeholder routes for the MVP sections. It deliberately does **not** build the real sign-in/registration/sign-out screens (S-01) or any feature pages (S-02…S-06).

This is roadmap foundation **F-03**. Its only prerequisite (F-01 account-data-boundary) and its parallel sibling (F-02 persistent-user-resources) are already implemented and merged.

## Current State Analysis

- **Backend is mature.** F-01 delivered Supabase JWT bearer validation in `src/Jadlify.API/Program.cs`: `JwtSecurityTokenHandler.DefaultMapInboundClaims = false`, a `JwtBearer` scheme bound to `SupabaseJwtOptions`, and a **global fallback authorization policy** that requires an authenticated user with the `sub` claim. `/health` is the only `AllowAnonymous` runtime endpoint. F-02 added EF Core/Npgsql persistence, repositories, and `MacroCalculator`. No domain HTTP endpoints exist yet — only `/health` and scaffolded OpenAPI.
- **No frontend exists.** There is no `package.json`, no `node_modules`, no client app. The repo is .NET-only (`Jadlify.slnx` with five `src/` projects and four `tests/` projects).
- **Hosting model is already decided** (`context/foundation/infrastructure.md`): build React to static assets, serve them from the ASP.NET Core app at a single origin, with `index.html` fallback routing. No CORS, no split deploy. "Getting Started" step 1 states the production build must be copied into the ASP.NET Core static asset directory with fallback routing to `index.html`.
- **CI is .NET-only** (`.github/workflows/deploy-azure-app-service.yml`): on push to `main`, restore → build → test `Jadlify.slnx` → `dotnet publish` the API → ZIP-deploy to Azure App Service F1 (`jadlify-mvp-wikla`). There is no Node setup and no frontend step.
- **Auth contract is fixed** (`docs/reference/contract-surfaces.md`): `ICurrentUser.UserId` returns an `ApplicationUserId` whose `.Value` is the literal Supabase `sub`. Inbound claim remapping is disabled. The browser may use Supabase only for auth/session; all domain data must go through the API.
- **API test harness pattern exists** (`tests/Jadlify.API.Tests/Authentication/AuthBoundaryTests.cs`): a `WebApplicationFactory<Program>` subclass swaps in a `TestAuthenticationHandler` keyed on the bearer token string (`"invalid"` → fail, `"missing-sub"` → no `sub` claim, anything else → `sub=<token>`). New endpoint tests should reuse this pattern.
- `.gitignore` already ignores `node_modules/` and `.env`, and has a commented `#wwwroot/` line ready to enable.
- Backend dev URLs: `https://localhost:7206` (HTTPS) and `http://localhost:5182` (HTTP).

## Desired End State

After this plan:

- `npm run dev` in `src/Jadlify.Web/` runs a Vite dev server that proxies `/api/*` (and the `/api/me` call) to the running ASP.NET Core backend, with HMR.
- `dotnet publish src/Jadlify.API/Jadlify.API.csproj -c Release` produces a publish output whose `wwwroot/` contains the built SPA, and visiting `/` serves `index.html`. Deep links (e.g. `/products`) also serve `index.html` (client-side routing), while `/api/*` and `/health` behave as API/health endpoints.
- Anonymous visitors can load the SPA (so they can reach the future login screen), but `GET /api/me` returns `401` without a valid token and `200` with the authenticated user's `sub` when a token is present.
- The SPA boots a Supabase session, attaches the current access token as a Bearer header on API calls via a typed client, and a route guard redirects unauthenticated users to a `/login` placeholder. A post-login landing renders the `/api/me` result, proving the protected round-trip.
- The app renders a responsive shell: an app bar with navigation that collapses to a drawer on mobile, and placeholder protected routes for the MVP sections.
- CI builds and quality-gates the frontend (`npm run lint`, `npm test`) and the existing `dotnet publish` step bundles the SPA into `wwwroot` via an MSBuild target, all before the Azure deploy.

### Key Discoveries:

- The **global fallback authorization policy** in `src/Jadlify.API/Program.cs:55` applies to any endpoint without its own auth metadata. The SPA's `MapFallbackToFile` endpoint and static assets must be `AllowAnonymous`, or anonymous users cannot load the login page — `UseStaticFiles()` itself short-circuits before authorization, but the fallback *endpoint* is subject to the policy.
- The `sub`-as-`ApplicationUserId` contract (`docs/reference/contract-surfaces.md`, `HttpContextCurrentUser`) means `/api/me` can return the identity with zero new persistence — it just reads `ICurrentUser`.
- The test handler in `AuthBoundaryTests.cs` lets `/api/me` tests assert `401`/`200`/subject deterministically without real Supabase tokens.
- `infrastructure.md` already mandates the static-hosting model, so no architecture choice is open here — only the wiring.

## What We're NOT Doing

- **No real sign-in / registration / sign-out UI.** The shell ships a `/login` *placeholder* and auth plumbing only; the working account flow is roadmap slice **S-01**.
- **No feature pages.** Products, recipes, meal plan, goals, and shopping list get placeholder routes only — their UIs are S-02…S-06.
- **No new domain endpoints** beyond the single authenticated `/api/me` session probe.
- **No CORS / split deployment.** Single-origin static hosting per `infrastructure.md`.
- **No SSR / Next.js.** Static SPA only.
- **No Playwright / E2E** in this change (Vitest + RTL unit/component tests only).
- **No Supabase Row Level Security or direct browser-to-table access.** All domain data continues to go through the API.
- **No real secrets committed.** Supabase URL + anon key are injected via `.env` (gitignored) and Azure app settings; only `.env.example` is committed.

## Implementation Approach

Build inward-to-outward and backend-before-its-consumer:

1. Scaffold the frontend project and wire it into both the local dev loop (Vite proxy) and the production build (MSBuild target → `wwwroot`), plus ASP.NET Core static + SPA-fallback serving. This phase is verifiable on its own: the built SPA is served by the API.
2. Add the authenticated `/api/me` endpoint the frontend will consume, with API tests proving the auth boundary holds for it.
3. Wire frontend auth plumbing (Supabase session, Bearer client, TanStack Query `useMe`, route guard) that consumes `/api/me`.
4. Layer the responsive shell (app bar + drawer + placeholder routes) and integrate the frontend quality gate into CI.

## Critical Implementation Details

- **SPA fallback vs. auth fallback policy (ordering + anonymous).** In `Program.cs`, place `app.UseStaticFiles()` before `UseAuthentication()`/`UseAuthorization()`, and register `app.MapFallbackToFile("index.html").AllowAnonymous()` after them. Without `AllowAnonymous`, the global fallback policy returns 401 for the SPA entrypoint and anonymous users can never reach the login screen. `/api/me` must live under the `/api` prefix so the SPA fallback does not shadow it and the Vite dev proxy can target `/api`.
- **MSBuild target needs Node on the runner.** The publish-time target shells out to `npm`. CI must run `actions/setup-node` before the `dotnet publish` step, even though the explicit lint/test step also installs Node. Make the target run only on publish (and Release build), not on every inner-loop `dotnet build`, to avoid slowing backend iteration.
- **Token freshness.** The API client must read the access token from the *current* Supabase session at call time (not cache it at startup), so a refreshed token is used after Supabase rotates it.

## Phase 1: Frontend Scaffold + Build/Dev Integration

### Overview

Create the Vite + React + TypeScript project with Tailwind, ESLint, and Vitest; wire the local dev proxy and the production MSBuild build-into-`wwwroot`; make ASP.NET Core serve the SPA with an anonymous `index.html` fallback.

### Changes Required:

#### 1. Frontend project scaffold

**File**: `src/Jadlify.Web/` (new project: `package.json`, `vite.config.ts`, `tsconfig*.json`, `index.html`, `src/main.tsx`, `src/App.tsx`)

**Intent**: Scaffold a Vite React-TS SPA that will own all client code. Keep it a plain npm project (not a `.csproj`) living under `src/` per the AGENTS.md "runtime code under src/" convention.

**Contract**: `package.json` exposes scripts `dev`, `build`, `preview`, `lint`, `test`. Build output goes to `src/Jadlify.Web/dist`. Node engine pinned to an LTS major.

#### 2. Tailwind CSS

**File**: `src/Jadlify.Web/` (Tailwind + PostCSS config, base stylesheet imported in `main.tsx`)

**Intent**: Add Tailwind as the styling/responsive system so later phases use utility breakpoints.

**Contract**: Tailwind content globs cover `index.html` and `src/**/*.{ts,tsx}`; base directives present in the global stylesheet.

#### 3. ESLint + Vitest + React Testing Library

**File**: `src/Jadlify.Web/` (ESLint flat config, Vitest config with jsdom + RTL setup file)

**Intent**: Establish the frontend quality tooling so the route guard and session context get unit/component coverage in Phase 3.

**Contract**: `npm run lint` and `npm test` run clean on the scaffold. Vitest uses the `jsdom` environment and an RTL/jest-dom setup file.

#### 4. Vite dev proxy

**File**: `src/Jadlify.Web/vite.config.ts`

**Intent**: Forward API calls to the running backend during local dev so there is no CORS and one mental model matches production.

**Contract**: `server.proxy` routes `/api` (and `/health` if used) to `https://localhost:7206` with `secure: false` (dev self-signed cert) and `changeOrigin: true`.

#### 5. MSBuild publish target (SPA → wwwroot)

**File**: `src/Jadlify.API/Jadlify.API.csproj`

**Intent**: Make `dotnet publish` build the SPA and emit it into the API's `wwwroot` so the existing CI publish step produces a single deployable artifact with no workflow change to the publish command itself.

**Contract**: A target that runs after/within publish executes `npm ci` + `npm run build` in `src/Jadlify.Web`, then includes `src/Jadlify.Web/dist/**` as published content under `wwwroot/`. Guard it to publish (and Release build) only, not every `dotnet build`. The `SpaRoot`/output path is a property so it is defined once.

#### 6. ASP.NET Core static + SPA fallback serving

**File**: `src/Jadlify.API/Program.cs`

**Intent**: Serve the built SPA from `wwwroot` and route client-side deep links to `index.html`, while keeping the API and health endpoints intact and the auth boundary unbroken.

**Contract**: `app.UseStaticFiles()` before auth middleware; `app.MapFallbackToFile("index.html").AllowAnonymous()` registered after `UseAuthorization()`. `/health` and future `/api/*` endpoints are unaffected. See Critical Implementation Details for ordering and the anonymous requirement.

#### 7. Ignore build artifacts

**File**: `.gitignore`

**Intent**: Keep generated frontend output and the API `wwwroot` out of source control.

**Contract**: `node_modules/` and `.env` already ignored; enable `wwwroot/` (uncomment the existing line) and add `src/Jadlify.Web/dist/`.

### Success Criteria:

#### Automated Verification:

- Frontend installs and builds: `npm ci && npm run build` in `src/Jadlify.Web` produces `dist/`
- Frontend lint passes: `npm run lint`
- Frontend test runner executes (scaffold smoke test): `npm test`
- Publish bundles the SPA: `dotnet publish src/Jadlify.API/Jadlify.API.csproj -c Release -o .publish` yields `.publish/wwwroot/index.html`
- Backend still builds and tests green: `pwsh ./.scripts/verify-min.ps1`

#### Manual Verification:

- Running the published app serves the SPA at `/` and a deep link like `/products` returns `index.html` (not 404)
- `npm run dev` (Vite) with the backend running proxies an `/api`/`/health` request to the backend without CORS errors
- `/health` still returns 200 anonymously after the static/fallback wiring

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Authenticated `/api/me` Endpoint

### Overview

Add a single minimal protected endpoint the frontend uses to confirm its token reaches the backend, and prove the auth boundary holds for it with API tests.

### Changes Required:

#### 1. `GET /api/me` endpoint

**File**: `src/Jadlify.API/Program.cs` (or a small endpoint module it calls)

**Intent**: Expose the authenticated user's id so the SPA can verify a real protected round-trip. No persistence — read `ICurrentUser`.

**Contract**: `GET /api/me` requires authentication (no `AllowAnonymous`; relies on the global fallback policy). Returns `200` with a JSON body containing the user id (`ICurrentUser.UserId.Value`). Anonymous → `401`. Lives under `/api` so the SPA fallback does not shadow it.

#### 2. API tests for `/api/me`

**File**: `tests/Jadlify.API.Tests/` (new test class, e.g. `Session/SessionEndpointTests.cs`)

**Intent**: Lock the auth boundary for the new endpoint using the existing test harness.

**Contract**: Reuse the `WebApplicationFactory<Program>` + `TestAuthenticationHandler` pattern from `AuthBoundaryTests.cs`. Assert: anonymous → `401`; `missing-sub` token → `403`; valid token (`"user-123"`) → `200` and body contains `user-123`.

#### 3. Contract-surface registry update

**File**: `docs/reference/contract-surfaces.md`

**Intent**: Record `/api/me` and the SPA hosting boundary so later slices reuse rather than duplicate them.

**Contract**: Add a section noting `GET /api/me` is the authenticated session probe returning the `sub`, and that the SPA is served from API `wwwroot` with an anonymous `index.html` fallback while `/api/*` stays protected.

### Success Criteria:

#### Automated Verification:

- New endpoint tests pass: `pwsh ./.scripts/test-min.ps1 -Project tests/Jadlify.API.Tests`
- Full backend verify is green: `pwsh ./.scripts/verify-min.ps1`

#### Manual Verification:

- With the backend running, `GET /api/me` without a token returns `401`
- `GET /api/me` with a valid Supabase access token returns `200` and the expected `sub`

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 3: Frontend Auth Plumbing + API Client

### Overview

Wire the client-side session and the API access layer the rest of the app depends on: a Supabase session source, a typed Bearer-attaching fetch client, TanStack Query with a `useMe()` round-trip, and a route guard that redirects unauthenticated users to a `/login` placeholder.

### Changes Required:

#### 1. Supabase client + session context

**File**: `src/Jadlify.Web/src/` (Supabase client module + a React session provider/hook)

**Intent**: Initialize the Supabase JS client and expose the current session/user/token to the app through context, reacting to Supabase auth state changes.

**Contract**: Reads `VITE_SUPABASE_URL` and `VITE_SUPABASE_ANON_KEY` from env. Exposes a hook returning `{ session, isLoading }` (or equivalent) and subscribes to `onAuthStateChange`. No UI here.

#### 2. Typed API client with auto-Bearer

**File**: `src/Jadlify.Web/src/` (fetch wrapper module)

**Intent**: Centralize API calls and attach the current access token as `Authorization: Bearer <token>` at call time.

**Contract**: A typed wrapper around `fetch` that resolves the token from the *current* Supabase session per request (see Critical Implementation Details — token freshness), targets relative `/api/...` URLs, and surfaces non-2xx as typed errors.

#### 3. TanStack Query provider + `useMe()`

**File**: `src/Jadlify.Web/src/` (Query client provider wrapping the app + a `useMe` query)

**Intent**: Establish the data-fetching pattern (loading/error/pending states satisfying the responsiveness NFR) that S-02…S-06 will follow, and prove the protected round-trip.

**Contract**: `QueryClientProvider` wraps the router. `useMe()` calls `GET /api/me` via the API client and is enabled only when a session exists.

#### 4. Route guard + `/login` placeholder

**File**: `src/Jadlify.Web/src/` (guard component + placeholder login route + post-login landing)

**Intent**: Protect app routes and give anonymous users a destination, without building the real auth form (S-01).

**Contract**: Guard renders the protected outlet when a session is present and redirects to `/login` otherwise (and shows nothing/loader while the session resolves). `/login` is a public placeholder clearly marked as wired in S-01. The landing route renders the `useMe()` result to demonstrate the round-trip.

#### 5. Env contract

**File**: `src/Jadlify.Web/.env.example`

**Intent**: Document the required public Supabase config without committing real values.

**Contract**: `.env.example` lists `VITE_SUPABASE_URL=` and `VITE_SUPABASE_ANON_KEY=`. Real `.env` stays gitignored; production values come from Azure app settings at build time.

#### 6. Auth-plumbing tests

**File**: `src/Jadlify.Web/src/` (Vitest/RTL tests)

**Intent**: Cover the load-bearing plumbing before feature slices build on it.

**Contract**: Tests assert the guard redirects to `/login` with no session and renders children with a session (mocked), and that the API client attaches the Bearer header from the session token.

### Success Criteria:

#### Automated Verification:

- Frontend tests pass: `npm test` in `src/Jadlify.Web`
- Lint passes: `npm run lint`
- Production build still succeeds: `npm run build`

#### Manual Verification:

- With a valid Supabase session, the landing page shows the user id returned by `/api/me`
- Without a session, navigating to a protected route redirects to `/login`
- After the Supabase token refreshes, a subsequent `/api/me` call still succeeds (token read fresh)

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 4: Responsive Shell Layout + Navigation + CI

### Overview

Wrap the protected area in a responsive app-bar/drawer layout with placeholder routes for the MVP sections, and integrate the frontend quality gate into the deploy workflow.

### Changes Required:

#### 1. Responsive app shell layout

**File**: `src/Jadlify.Web/src/` (layout component + nav)

**Intent**: Provide the persistent shell — top app bar with navigation that collapses to a drawer/hamburger on mobile, wrapping a content outlet — meeting the device/browser NFR.

**Contract**: Tailwind breakpoints drive desktop nav vs. mobile drawer (toggle state for open/closed). The app bar has a brand area and an account-area placeholder slot (sign-out wiring belongs to S-01). Layout renders the routed content outlet.

#### 2. Placeholder section routes

**File**: `src/Jadlify.Web/src/` (router config + placeholder pages)

**Intent**: Give the navigation real destinations so later slices fill them in without restructuring routing.

**Contract**: Protected routes for products, recipes, meal plan, daily goals, and shopping list, each a minimal placeholder behind the guard. `/login` remains the public placeholder. Routing uses the SPA fallback for deep links (already wired in Phase 1).

#### 3. CI frontend quality gate

**File**: `.github/workflows/deploy-azure-app-service.yml`

**Intent**: Lint and test the frontend in CI and ensure Node is available for the publish-time SPA build, before the Azure deploy.

**Contract**: Add `actions/setup-node` and a frontend step running `npm ci`, `npm run lint`, `npm test` in `src/Jadlify.Web`, ordered before the existing `dotnet publish` step (whose MSBuild target performs the production SPA build). The deploy step is unchanged.

#### 4. Dev/run documentation

**File**: `AGENTS.md` (Build/Test/Run section) and a short `src/Jadlify.Web/README.md`

**Intent**: Record how to run the two-process dev loop and the frontend npm scripts so future agents stay unblocked.

**Contract**: Document `dotnet run` (backend) + `npm run dev` (Vite proxy) for local dev, and the `lint`/`test`/`build` scripts. Keep it concise.

### Success Criteria:

#### Automated Verification:

- Frontend lint + tests + build pass: `npm run lint`, `npm test`, `npm run build` in `src/Jadlify.Web`
- Full backend verify green: `pwsh ./.scripts/verify-min.ps1`
- Publish still bundles the SPA into `wwwroot`: `dotnet publish src/Jadlify.API/Jadlify.API.csproj -c Release -o .publish` yields `.publish/wwwroot/index.html`
- Workflow file is valid YAML and includes the Node setup + frontend step before publish

#### Manual Verification:

- On a desktop viewport the app bar shows inline navigation; on a narrow/mobile viewport it collapses to a working drawer/hamburger
- Each placeholder section route renders behind the guard and is reachable from the nav
- Interactions give visible feedback ≤200 ms and any >2 s load shows a progress indicator (responsiveness NFR)
- Usable on the latest Chrome/Edge/Firefox/Safari (desktop) and a current mobile browser, in both desktop and responsive-mobile modes

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful.

---

## Testing Strategy

### Unit / Component Tests (Vitest + RTL):

- Route guard: redirects to `/login` without a session; renders children with a session.
- API client: attaches `Authorization: Bearer <token>` from the current session.
- Session context: exposes session and reacts to auth-state changes (mocked Supabase).

### Backend Tests (xUnit):

- `/api/me`: `401` anonymous, `403` authenticated-without-`sub`, `200` + subject with a valid token (reusing `TestAuthenticationHandler`).

### Manual Testing Steps:

1. Run backend (`dotnet run`) + `npm run dev`; confirm proxied `/api/me` behavior with and without a Supabase session.
2. Publish (`dotnet publish -c Release`) and run the published app; confirm `/`, a deep link, and `/health` behave correctly.
3. Resize between desktop and mobile widths; confirm the nav collapses to a drawer and back.
4. Verify the responsiveness NFR (≤200 ms feedback, progress indicator for >2 s).

## Performance Considerations

- The SPA is statically served from `wwwroot` on Azure App Service F1 (no SSR, no server render cost). Keep the initial bundle modest; the shell has few dependencies (React, router, TanStack Query, Supabase JS, Tailwind-generated CSS).
- TanStack Query's pending/loading states are the mechanism for the responsiveness NFR; rely on them rather than custom spinners per call.

## Migration Notes

- No data migration. Net-new frontend plus one read-only authenticated endpoint.
- CI gains Node setup; the publish command is unchanged because the MSBuild target builds the SPA into `wwwroot` during `dotnet publish`.
- Production Supabase URL/anon key must be present as build-time env (`VITE_*`) on the CI runner / Azure build; document, do not commit.

## References

- Roadmap item F-03: `context/foundation/roadmap.md`
- Hosting model: `context/foundation/infrastructure.md` ("Getting Started" step 1)
- Stack decision: `context/foundation/tech-stack.md`
- Auth boundary + contracts: `docs/reference/contract-surfaces.md`
- API auth wiring: `src/Jadlify.API/Program.cs`
- Test harness to reuse: `tests/Jadlify.API.Tests/Authentication/AuthBoundaryTests.cs`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Frontend Scaffold + Build/Dev Integration

#### Automated

- [x] 1.1 Frontend installs and builds (`npm ci && npm run build` → `dist/`) — 1885fac
- [x] 1.2 Frontend lint passes (`npm run lint`) — 1885fac
- [x] 1.3 Frontend test runner executes (`npm test`) — 1885fac
- [x] 1.4 Publish bundles the SPA (`dotnet publish -c Release` → `.publish/wwwroot/index.html`) — 1885fac
- [x] 1.5 Backend still builds and tests green (`pwsh ./.scripts/verify-min.ps1`) — 1885fac

#### Manual

- [x] 1.6 Published app serves SPA at `/` and deep link returns `index.html` — 1885fac
- [x] 1.7 `npm run dev` proxies `/api`/`/health` to backend without CORS errors — 1885fac
- [x] 1.8 `/health` still returns 200 anonymously — 1885fac

### Phase 2: Authenticated `/api/me` Endpoint

#### Automated

- [x] 2.1 New `/api/me` endpoint tests pass (`test-min.ps1 -Project tests/Jadlify.API.Tests`) — 00893f4
- [x] 2.2 Full backend verify is green (`pwsh ./.scripts/verify-min.ps1`) — 00893f4

#### Manual

- [ ] 2.3 `GET /api/me` without a token returns `401`
- [ ] 2.4 `GET /api/me` with a valid token returns `200` and the expected `sub`

### Phase 3: Frontend Auth Plumbing + API Client

#### Automated

- [x] 3.1 Frontend tests pass (`npm test`)
- [x] 3.2 Lint passes (`npm run lint`)
- [x] 3.3 Production build succeeds (`npm run build`)

#### Manual

- [ ] 3.4 Landing page shows the `/api/me` user id with a valid session
- [ ] 3.5 Protected route redirects to `/login` without a session
- [ ] 3.6 `/api/me` still succeeds after Supabase token refresh

### Phase 4: Responsive Shell Layout + Navigation + CI

#### Automated

- [ ] 4.1 Frontend lint + tests + build pass
- [ ] 4.2 Full backend verify green (`pwsh ./.scripts/verify-min.ps1`)
- [ ] 4.3 Publish bundles SPA into `wwwroot` (`.publish/wwwroot/index.html`)
- [ ] 4.4 Workflow YAML valid with Node setup + frontend step before publish

#### Manual

- [ ] 4.5 App bar shows inline nav on desktop, collapses to drawer on mobile
- [ ] 4.6 Each placeholder section route renders behind the guard and is reachable
- [ ] 4.7 Responsiveness NFR met (≤200 ms feedback, progress indicator for >2 s)
- [ ] 4.8 Usable on the four desktop browsers + a current mobile browser, desktop + responsive modes
