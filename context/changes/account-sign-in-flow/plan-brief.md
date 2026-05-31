# Account Sign-In Flow (S-01) — Plan Brief

> Full plan: `context/changes/account-sign-in-flow/plan.md`

## What & Why

Roadmap slice **S-01**: turn the auth *plumbing* from F-01 (backend boundary) and F-03 (app shell) into a real account flow — a user can **register, sign in, sign out, and reach only the protected app surface** (PRD FR-001/FR-002, US-01). It's the first user-visible test of per-user data isolation, so the account can't be treated as a technical detail.

## Starting Point

The backend is done and untouched: Supabase JWT validation, a global "must be authenticated + `sub`" policy, `GET /api/me`, and anonymous SPA fallback. The frontend already boots a Supabase session (`SessionProvider`/`useSession`), guards routes (`RequireAuth` → `/login`), and attaches the Bearer per request. F-03 left three explicit placeholders for S-01: the `LoginPage`, the app-bar "Account" area, and the authed-on-`/login` redirect.

## Desired End State

`/login` hosts one form that toggles between **Zaloguj się** and **Załóż konto**; on success the user lands on `/`. A signed-in user who hits `/login` is bounced to `/`. The app bar shows an accessible account dropdown with the user's email and **Wyloguj**; signing out clears the session and the guard returns them to `/login`. No backend change.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| Email confirmation | Off — instant session on sign-up | Local Supabase has `enable_confirmations=false`; simplest flow for single-user MVP. | Plan |
| Page structure | Single `/login` with mode toggle | One form, one route, shared validation/error handling. | Plan |
| Scope | Core only (register/sign-in/sign-out) | Exactly covers FR-001/FR-002; no password-reset mail flows. | Plan |
| Validation & errors | Light client checks + mapped Supabase errors | Meets responsiveness + "no secrets in logs" NFRs without overbuilding. | Plan |
| Account area | Dropdown menu with email + sign-out | Scales to future account actions; user-chosen over a bare button. | Plan |
| Redirect | Always to home (`/`) | Predictable, closes "only the protected surface", easy to test. | Plan |
| Tests | Key flow paths (Vitest + RTL) | Covers account-critical behavior at reasonable cost, matching `App.test.tsx`. | Plan |

## Scope

**In scope:**

- Real `LoginPage` form: email + password, sign-in ⇄ sign-up toggle, light validation, mapped errors, pending state, navigate to `/` on success, redirect authed users off `/login`.
- `authErrors.ts` pure error-mapping helper (no credential/secret leakage).
- Accessible `AccountMenu` in the app shell: email + **Wyloguj** (`signOut`), keyboard + outside-click close.
- Unit tests for the form, the menu, and the error mapper.

**Out of scope:**

- Any backend change (no new endpoint, no `MeResponse` enrichment, no policy edit).
- Password reset / change / "forgot password"; email-confirmation UX; deep-link preservation; OAuth/magic-link/MFA; new identity contracts.

## Architecture / Approach

Frontend-only, two phases by flow direction. **Entry** (Phase 1): the `LoginPage` form calls `signInWithPassword` / `signUp` directly. **Exit** (Phase 2): the `AccountMenu` calls `signOut`. In both, the existing `SessionProvider.onAuthStateChange` subscription is the *single* source that updates session state and drives `RequireAuth` — components never set session state or imperatively navigate on sign-out, avoiding redirect races. Email display reads `session.user.email` client-side.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Entry — sign-in / registration | Real `/login` form + error mapping + redirect logic | Sign-up no-session branch if prod confirmations are on; not leaking creds in errors/logs |
| 2. Exit — account menu + sign-out | Accessible account dropdown + `signOut` wiring | Dropdown a11y (Escape/outside-click/focus); double-redirect race on sign-out |

**Prerequisites:** F-01 and F-03 merged (done); a running local Supabase stack (`supabase start`) with `.env` `VITE_*` values for manual testing.
**Estimated effort:** ~1–2 focused sessions across 2 phases.

## Open Risks & Assumptions

- **Production Supabase must have email confirmations disabled** to match local; otherwise `signUp` returns no session and registrants appear stuck (a neutral message is shown, but no confirmation screen is built). Confirm before first production deploy.
- No automated test exercises a *real* Supabase; auth correctness against the live provider is manual/E2E (E2E is out of MVP scope).

## Success Criteria (Summary)

- A user can register a new email, sign in, and reach the protected home (showing their `/api/me` id).
- The account menu shows their email; **Wyloguj** returns them to `/login`, and protected routes then redirect to `/login`.
- A signed-in user can't see `/login`; `npm run lint`, `npm test`, and `npm run build` are green.
