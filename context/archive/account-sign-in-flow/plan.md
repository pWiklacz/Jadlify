# Account Sign-In Flow (S-01) Implementation Plan

## Overview

Turn the auth *plumbing* delivered by F-01 (backend boundary) and F-03 (app shell) into a real, user-visible account flow: a user can **register**, **sign in**, **sign out**, and reach **only** the protected app surface. Scope is frontend-only — no backend endpoint, data model, or auth-policy change is needed, because Supabase issues sessions directly to the browser and the API already validates them.

This slice fills the three placeholders F-03 deliberately left for S-01:

1. `LoginPage.tsx` — placeholder text ("real sign-in / registration / sign-out flow is roadmap slice S-01").
2. `AppShell.tsx` — "Account-area placeholder; real account/sign-out UI is slice S-01".
3. Redirect correctness — an authenticated user can currently still render `/login`; "only the protected surface" means bouncing authed users off `/login` and navigating into the app after auth.

## Current State Analysis

**Backend (done — do not touch):** `src/Jadlify.API/Program.cs` validates Supabase ES256 JWTs via JWKS, enforces a global "authenticated + `sub`" fallback policy, exposes `GET /api/me` (authenticated session probe → `MeResponse(userId)`), and serves the SPA with an anonymous `index.html` fallback. `/health` is the only anonymous runtime endpoint. `ICurrentUser` / `ApplicationUserId` / `UserScope` are the registered identity contracts (`docs/reference/contract-surfaces.md`).

**Frontend session plumbing (done — reuse):**
- `src/Jadlify.Web/src/lib/supabase.ts` — Supabase client, auth-only, `persistSession` + `autoRefreshToken` on by default.
- `src/Jadlify.Web/src/auth/SessionProvider.tsx` — boots `getSession()` and subscribes to `onAuthStateChange`; exposes `{ session, isLoading }`.
- `src/Jadlify.Web/src/auth/useSession.ts` — reads that context (throws outside provider).
- `src/Jadlify.Web/src/auth/RequireAuth.tsx` — gates the protected outlet, redirects to `/login` when no session, shows a loader while resolving.
- `src/Jadlify.Web/src/api/apiClient.ts` + `client.ts` — attaches the live Bearer token per request.
- `src/Jadlify.Web/src/App.tsx` — `/login` is a public route; the protected area lives under `RequireAuth` → `AppShell`; `*` → `/`.

**What's missing (this slice):** the actual sign-in/sign-up form, the sign-out control, and the authed-on-`/login` redirect.

### Key Discoveries:

- Local Supabase (`supabase/config.toml`) has `enable_confirmations = false` and `minimum_password_length = 6` — `signUp` yields an **instant session** with no email-confirmation step. The plan assumes this; the production Supabase project must match (see Migration Notes).
- All required dependencies are already installed (`@supabase/supabase-js`, `react-router-dom` v7, `@tanstack/react-query`, `@testing-library/*`, `user-event`) — **no new dependencies**.
- The test pattern for "signed-out Supabase" is established in `src/Jadlify.Web/src/App.test.tsx` (mocks `supabase.auth.getSession` / `onAuthStateChange`); S-01 tests extend it by also mocking `signInWithPassword` / `signUp` / `signOut`.
- `session.user.email` is available on the Supabase `Session` already held by `useSession()` — the account menu shows the email **client-side**, with no `/api/me` change.
- `MeResponse` returns only `userId` (the `sub`); we deliberately do **not** enrich it. Identity display reads the Supabase session.

## Desired End State

A visitor lands on `/login`, can toggle between **Zaloguj się** and **Załóż konto**, submit email + password, and on success is taken to `/` (the protected home rendering their `/api/me` identity). A signed-in user who navigates to `/login` is redirected to `/`. In the app bar, an accessible account menu shows their email and a **Wyloguj** action; signing out clears the session and returns them to `/login`. After sign-out, any protected route redirects to `/login`.

Verify: `npm run lint`, `npm test`, `npm run build` (from `src/Jadlify.Web`) are green; the manual flow above works against a local Supabase stack.

## What We're NOT Doing

- **No backend change** — no new endpoint, no `MeResponse` enrichment, no auth-policy edit.
- **No password reset / change / "forgot password"** (out of FR-001/FR-002 scope; deferred).
- **No email-confirmation UX** — no "check your inbox" screen, no confirmation-redirect handling (relies on confirmations being off).
- **No deep-link preservation** — post-auth always navigates to `/` (not the originally requested route).
- **No OAuth / social / magic-link / MFA** — email + password only.
- **No new identity/authorization contract** — S-01 consumes the existing `ICurrentUser` / session contracts; `contract-surfaces.md` gains no new identity surface.
- **No styling system change** — Tailwind classes consistent with `LandingPage` / `AppShell`.

## Implementation Approach

Two frontend-only phases, split by direction of the flow: **entry** (sign-in / registration) then **exit** (account menu / sign-out). Each phase is independently testable with the existing Vitest + RTL setup and leaves the app shippable. Supabase auth calls are made directly from the components; `SessionProvider`'s existing `onAuthStateChange` subscription propagates the resulting session change to the guard and the rest of the app, so neither phase adds new global state.

Error handling uses a small pure mapping helper so Supabase SDK errors become friendly, localized messages without ever logging passwords or tokens (PRD: "hasła i tokeny … nigdy nie pojawiają się w logach"). Client validation is light: email shape + password ≥ 6 chars, matching the Supabase minimum.

## Critical Implementation Details

- **Auth-state propagation is implicit.** Components call `signInWithPassword` / `signUp` / `signOut`; they must NOT manually set session state. `SessionProvider.onAuthStateChange` is the single source that updates `{ session }`, which re-renders `RequireAuth`. After `signOut`, the guard redirect to `/login` happens on the next render — components should not also imperatively navigate to `/login` on sign-out (avoid a double redirect race); navigating to `/` post sign-in/up is correct because the guard then allows it.
- **Sign-up may or may not return a session.** With confirmations off, `signUp` returns a session and the app proceeds like sign-in. The submit handler should treat "session now present" as success and navigate to `/`; if `signUp` succeeds but returns no session (confirmations unexpectedly on in prod), surface a neutral message rather than navigating — this is the one defensive branch kept, but no dedicated confirmation screen is built.

## Phase 1: Entry — Sign-In / Registration (`/login`)

### Overview

Replace the placeholder `LoginPage` with a real email/password form that toggles between sign-in and registration, validates lightly, maps Supabase errors to friendly messages, shows loading/disabled states, navigates to `/` on success, and redirects already-authenticated visitors away from `/login`.

### Changes Required:

#### 1. Auth error mapping helper

**File**: `src/Jadlify.Web/src/auth/authErrors.ts` (new)

**Intent**: Convert a Supabase `AuthError` (or unknown thrown value) into a short, user-facing Polish message, so the form never renders raw SDK strings and never echoes credentials. Keeps the mapping pure and unit-testable.

**Contract**: Export a function `toAuthMessage(error: unknown): string`. Maps known cases — invalid credentials, user already registered, weak/short password, network/unknown — to neutral messages (e.g. invalid login → "Nieprawidłowy e-mail lub hasło"). Must not include the attempted email/password in the returned string. Does not log.

#### 2. Real LoginPage with sign-in / sign-up toggle

**File**: `src/Jadlify.Web/src/routes/LoginPage.tsx` (rewrite)

**Intent**: Render a single accessible form for both modes; a toggle switches between "Zaloguj się" and "Załóż konto". On submit, call the matching Supabase method, handle errors via `toAuthMessage`, and on success navigate to `/`. Redirect authenticated users away.

**Contract**:
- Local state: `mode: 'signin' | 'signup'`, `email`, `password`, `error: string | null`, `isSubmitting: boolean`.
- Uses `useSession()`; if `session` is present, render `<Navigate to="/" replace />` (authed users never see the form). While `isLoading`, render the same loader idiom as `RequireAuth`.
- Light client validation before calling Supabase: non-empty valid-looking email, password length ≥ 6; invalid input sets `error` and skips the network call.
- Sign-in → `supabase.auth.signInWithPassword({ email, password })`; sign-up → `supabase.auth.signUp({ email, password })`. On `{ error }`, set the mapped message; on success with a session, `useNavigate()(' /', { replace: true })` (see Critical Implementation Details for the no-session branch).
- During the request, the submit button is disabled and shows a pending affordance (NFR: visible confirmation < 200 ms). Error text uses `role="alert"`; the form is keyboard-usable (labels tied to inputs, submit on Enter).
- Tailwind layout consistent with the existing centered `LoginPage` placeholder.

#### 3. App routing comment cleanup

**File**: `src/Jadlify.Web/src/App.tsx`

**Intent**: The `/login` route now hosts real auth UI; update the stale "Public placeholder; real auth UI is slice S-01" comment so it reflects reality. No structural routing change.

**Contract**: Comment-only edit on the `/login` route block.

### Success Criteria:

#### Automated Verification:

- Lint passes: `npm run lint` (in `src/Jadlify.Web`)
- Type-check + build passes: `npm run build`
- Unit tests pass: `npm test` — covering: successful sign-in navigates to `/`; invalid credentials render a mapped error and stay on `/login`; successful sign-up establishes a session and navigates to `/`; an authenticated visitor at `/login` is redirected to `/`.
- `toAuthMessage` unit tests assert known errors map to friendly strings and never echo the input.

#### Manual Verification:

- Logging in with a real account from the local Supabase stack lands on the home view showing the `/api/me` user id.
- Registering a brand-new email creates a session and enters the app without a confirmation step.
- A wrong password shows a readable message; the browser console / network logs contain no password or token.
- Visiting `/login` while already signed in redirects to `/`.

**Implementation Note**: After Phase 1 automated verification passes, pause for manual confirmation before starting Phase 2. Phase blocks use plain bullets; checkbox state lives in `## Progress`.

---

## Phase 2: Exit — Account Menu + Sign-Out (AppShell)

### Overview

Replace the app bar's "Account" placeholder with an accessible dropdown account menu that shows the signed-in user's email and a **Wyloguj** action. Signing out clears the Supabase session; the existing guard then redirects to `/login`.

### Changes Required:

#### 1. Account menu component

**File**: `src/Jadlify.Web/src/layout/AccountMenu.tsx` (new)

**Intent**: A self-contained dropdown showing the current user's email and a sign-out action, replacing the static "Account" text. Reads identity from the live session; performs sign-out via Supabase.

**Contract**:
- Reads `session` from `useSession()`; displays `session.user.email` (falls back to a neutral label if absent). Renders nothing meaningful when there is no session (the shell only mounts under `RequireAuth`, so a session is normally present).
- Trigger is a `<button>` with `aria-haspopup="menu"` and `aria-expanded` bound to open state; the panel is a menu region containing the email and a **Wyloguj** button.
- Accessibility/behavior: `Escape` closes and returns focus to the trigger; a click (or focus move) outside closes it; menu items are keyboard reachable. The mobile-drawer pattern in `AppShell.tsx` (conditionally mounted `open` state) is the reference for testable open/close in jsdom.
- **Wyloguj** calls `supabase.auth.signOut()`; it does NOT imperatively navigate — `onAuthStateChange` clears the session and `RequireAuth` redirects to `/login` (see Critical Implementation Details). Disable the item while the sign-out request is in flight.

#### 2. Wire the menu into the app shell

**File**: `src/Jadlify.Web/src/layout/AppShell.tsx`

**Intent**: Swap the `ml-auto` "Account" placeholder span for `<AccountMenu />`, and update the stale "real account/sign-out UI is slice S-01" comment.

**Contract**: Replace the placeholder span with the component in the same app-bar slot; keep the `ml-auto` positioning and responsive behavior intact. Comment cleanup only beyond the swap.

### Success Criteria:

#### Automated Verification:

- Lint passes: `npm run lint`
- Type-check + build passes: `npm run build`
- Unit tests pass: `npm test` — covering: the menu renders the session email; opening then pressing `Escape` (and clicking outside) closes it; **Wyloguj** invokes `supabase.auth.signOut`. Existing `AppShell.test.tsx` still passes with the menu mounted.

#### Manual Verification:

- The account menu shows the signed-in user's email.
- **Wyloguj** clears the session and returns to `/login`.
- The menu is operable by keyboard (open, navigate, `Escape` to close) and closes on outside click.
- After signing out, manually visiting a protected route (e.g. `/recipes`) redirects to `/login`.

**Implementation Note**: After Phase 2 automated verification passes, pause for manual confirmation. This completes the S-01 end-to-end flow (register → sign in → sign out → only the protected surface).

---

## Testing Strategy

### Unit Tests:

- `authErrors.test.ts` — each known Supabase error maps to the expected friendly message; unknown input yields a safe default; returned message never contains the supplied email/password.
- `LoginPage.test.tsx` — sign-in success navigates to `/`; invalid credentials show a mapped error; sign-up success establishes a session and navigates; authed visitor is redirected off `/login`. Supabase is mocked as in `App.test.tsx`, extended with `signInWithPassword` / `signUp`.
- `AccountMenu.test.tsx` — renders email; open/close via trigger and `Escape`; outside-click closes; **Wyloguj** calls `signOut`.

### Integration Tests:

- `App.test.tsx` continues to assert the anonymous → `/login` redirect (regression). Optionally extend it so a mocked authed session renders the shell with the account menu instead of the login form.

### Manual Testing Steps:

1. Start the backend (`dotnet run --project src/Jadlify.API`) and Vite (`npm run dev`) against a running local Supabase stack (`supabase start`).
2. Register a new email/password → lands in the app, home shows the `/api/me` id.
3. Open the account menu → see the email → **Wyloguj** → returns to `/login`.
4. Sign back in with the same credentials → success.
5. Enter a wrong password → friendly error, nothing sensitive in console/network.
6. While signed in, visit `/login` → redirected to `/`. While signed out, visit `/recipes` → redirected to `/login`.

## Performance Considerations

Negligible — two small components and direct Supabase auth calls. The NFR that applies is responsiveness: disable the submit/sign-out controls and show a pending state immediately on click so feedback appears < 200 ms.

## Migration Notes

No data migration. **Configuration dependency:** this flow assumes the Supabase project has email confirmations disabled (`enable_confirmations = false`), as the local stack does. The production / hosted Supabase project must match, otherwise `signUp` will not return a session and users will appear "stuck" after registering (the defensive no-session branch shows a neutral message but no confirmation screen is built). Confirm this setting before first production deploy.

## References

- Roadmap slice: `context/foundation/roadmap.md` (S-01)
- Contract surfaces (reuse, do not duplicate): `docs/reference/contract-surfaces.md`
- F-03 plumbing this builds on: `context/changes/responsive-app-shell/plan-brief.md`
- Existing test pattern: `src/Jadlify.Web/src/App.test.tsx`
- PRD: `context/foundation/prd.md` (FR-001, FR-002, US-01; NFR privacy/responsiveness)

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Entry — Sign-In / Registration (`/login`)

#### Automated

- [x] 1.1 Lint passes: `npm run lint` — a7c7b0e
- [x] 1.2 Type-check + build passes: `npm run build` — a7c7b0e
- [x] 1.3 Flow unit tests pass: sign-in success → `/`, invalid creds → mapped error, sign-up → session → `/`, authed visitor redirected off `/login` — a7c7b0e
- [x] 1.4 `toAuthMessage` unit tests pass and never echo input — a7c7b0e

#### Manual

- [x] 1.5 Real-account login lands on home showing `/api/me` id — a7c7b0e
- [x] 1.6 New-email registration creates a session and enters the app (no confirmation step) — a7c7b0e
- [x] 1.7 Wrong password shows a readable message; no password/token in console or network — a7c7b0e
- [x] 1.8 Visiting `/login` while signed in redirects to `/` — a7c7b0e

### Phase 2: Exit — Account Menu + Sign-Out (AppShell)

#### Automated

- [x] 2.1 Lint passes: `npm run lint` — 52d1c57
- [x] 2.2 Type-check + build passes: `npm run build` — 52d1c57
- [x] 2.3 Menu unit tests pass: renders email, open/close via trigger + `Escape` + outside-click, **Wyloguj** calls `signOut`; `AppShell.test.tsx` still green — 52d1c57

#### Manual

- [x] 2.4 Account menu shows the signed-in user's email — 52d1c57
- [x] 2.5 **Wyloguj** clears the session and returns to `/login` — 52d1c57
- [x] 2.6 Menu is keyboard-operable and closes on outside click — 52d1c57
- [x] 2.7 After sign-out, visiting a protected route redirects to `/login` — 52d1c57
