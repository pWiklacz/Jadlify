# Responsive App Shell — Plan Brief

> Full plan: `context/changes/responsive-app-shell/plan.md`

## What & Why

Stand up Jadlify's first frontend (roadmap **F-03**): a Vite + React + TypeScript SPA, styled with Tailwind, served single-origin from the existing ASP.NET Core API. It exists so the MVP flow (S-01…S-06) has a responsive, authenticated shell to land in — login plumbing and navigation, not the features themselves.

## Starting Point

The backend is already mature: F-01 (Supabase JWT bearer auth, global "must be authenticated" fallback policy, `ICurrentUser`/`ApplicationUserId`) and F-02 (EF Core/Npgsql persistence, repositories, `MacroCalculator`) are merged. There is **no frontend at all** (no `package.json`), and the API exposes only `/health` plus scaffolded OpenAPI — no domain endpoints. `infrastructure.md` already mandates serving React static assets from ASP.NET Core with `index.html` fallback; CI is .NET-only.

## Desired End State

`npm run dev` runs Vite with an `/api` proxy to the backend; `dotnet publish` bundles the built SPA into the API's `wwwroot` and serves it single-origin with client-side deep links falling back to `index.html`. Anonymous users can load the SPA (to reach a future login screen) but `GET /api/me` is `401` without a token and `200`+`sub` with one. The SPA boots a Supabase session, attaches the access token as a Bearer on API calls, guards protected routes (redirect to a `/login` placeholder), and renders a responsive app-bar/drawer shell with placeholder routes for the MVP sections.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| Frontend stack | Vite + React + TypeScript | Static SPA matches the single-origin hosting model; TS protects the API contract. | Plan |
| Hosting / dev integration | MSBuild publish target → `wwwroot` + Vite dev-proxy | Keeps the existing `dotnet publish` CI flow and gives HMR with no CORS. | Infrastructure / Plan |
| Styling / responsive | Tailwind CSS | Fastest path to consistent responsive breakpoints, fits the low-complexity mandate. | Plan |
| Auth scope (F-03 vs S-01) | Plumbing only | Real sign-in/registration/sign-out UI is roadmap slice S-01; shell stays lean. | Roadmap / Plan |
| Auth round-trip proof | Add authenticated `GET /api/me` | Gives a real protected round-trip reusing F-01's boundary; authenticated, not public. | Plan |
| Data fetching | TanStack Query + typed fetch (auto-Bearer) | Loading/pending states realize the responsiveness NFR and set the slice pattern. | Plan |
| Navigation shape | Top app bar + mobile drawer | Simplest proven responsive pattern meeting the device NFR. | Plan |
| Frontend testing / CI | Vitest + RTL + ESLint, gated in CI | Lightweight coverage for the load-bearing guard/session plumbing. | Plan |

## Scope

**In scope:**

- Vite + React + TS scaffold at `src/Jadlify.Web/` with Tailwind, ESLint, Vitest/RTL.
- Vite dev proxy + MSBuild publish target bundling the SPA into API `wwwroot`.
- ASP.NET Core static serving + anonymous `index.html` SPA fallback.
- One authenticated `GET /api/me` endpoint + API tests.
- Supabase session context, Bearer-attaching API client, TanStack Query `useMe`, route guard, `/login` placeholder, landing showing `/api/me`.
- Responsive app-bar/drawer layout + placeholder protected routes for MVP sections.
- CI Node setup + frontend lint/test gate before publish; dev-run docs.

**Out of scope:**

- Real sign-in / registration / sign-out UI (S-01) and feature pages (S-02…S-06).
- Any domain endpoint beyond `/api/me`; CORS / split deploy; SSR/Next.js.
- Playwright/E2E; Supabase RLS or direct browser-to-table access; committed secrets.

## Architecture / Approach

Single origin: ASP.NET Core serves API + static SPA. Inward-to-outward build order — scaffold & serving first, then the `/api/me` endpoint, then the frontend plumbing that consumes it, then the responsive layout + CI. The SPA's static assets and `index.html` fallback are `AllowAnonymous` while `/api/*` stays behind the existing global auth policy.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Scaffold + build/dev integration | Vite/React/TS+Tailwind project, Vite proxy, MSBuild→`wwwroot`, SPA fallback serving | SPA fallback colliding with the global auth policy or shadowing `/api` routes |
| 2. Authenticated `/api/me` | Minimal protected session probe + API tests + contract-surface update | Accidentally allowing anonymous or being shadowed by the SPA fallback |
| 3. Auth plumbing + API client | Supabase session, Bearer client, TanStack Query `useMe`, route guard, `/login` placeholder | Token-freshness on refresh; guard/redirect correctness |
| 4. Responsive layout + nav + CI | App-bar/drawer shell, placeholder routes, CI Node + lint/test gate | Responsive correctness across breakpoints; CI Node/build wiring |

**Prerequisites:** F-01 (done) and F-02 (done) are merged. Supabase URL + anon key must be available as `VITE_*` env locally (`.env`) and on the CI/Azure build; no real values in source.

**Estimated effort:** ~3–4 focused sessions across 4 phases.

## Open Risks & Assumptions

- The MSBuild publish target requires Node on the CI runner; `actions/setup-node` must precede `dotnet publish`.
- Because there is no login UI in F-03, the full auth flow is only manually testable once S-01 lands; F-03 verifies plumbing with mocked sessions + the `/api/me` round-trip.
- Azure App Service F1 build must expose the `VITE_*` Supabase values at frontend build time.

## Success Criteria (Summary)

- Published app serves the responsive SPA single-origin; deep links and `/health` behave; `/api/me` enforces auth (`401`/`200`).
- A guarded route redirects anonymous users to `/login`; an authenticated session renders the landing with the `/api/me` user id.
- Frontend lint/tests and backend verify are green, and CI builds the SPA into `wwwroot` before deploy.
