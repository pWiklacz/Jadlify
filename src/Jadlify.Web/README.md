# Jadlify.Web

The Jadlify single-page app: Vite + React + TypeScript, styled with Tailwind CSS. It is served **single-origin** from the ASP.NET Core API's `wwwroot` in production; there is no separate frontend deployment and no CORS.

## Prerequisites

- Node `>=20.19.0` (see `engines` in `package.json`)
- The Jadlify API for backend calls during local dev

## Local development (two processes)

The Vite dev server proxies `/api` and `/health` to the backend at `https://localhost:7206`, so local dev mirrors the single-origin production model.

```bash
# terminal 1 — backend (repo root)
dotnet run --project src/Jadlify.API

# terminal 2 — frontend (this directory)
npm install        # first time only
npm run dev        # Vite dev server with HMR + API proxy
```

Open the URL Vite prints (default `http://127.0.0.1:5173`).

## Scripts

| Script          | Purpose                                            |
| --------------- | -------------------------------------------------- |
| `npm run dev`     | Vite dev server (HMR) with the API/health proxy.   |
| `npm run build`   | Type-check (`tsc -b`) + production build to `dist/`. |
| `npm run preview` | Serve the production build locally.                |
| `npm run lint`    | ESLint.                                            |
| `npm test`        | Vitest + React Testing Library (jsdom).            |

## Environment

Public Supabase config is injected at build time via Vite env vars. Copy the template and fill in real values (the real `.env` is gitignored):

```bash
cp .env.example .env
# VITE_SUPABASE_URL=...
# VITE_SUPABASE_ANON_KEY=...
```

In production these are provided as build-time env on the CI runner / Azure build. Local Supabase stack setup (ports, secrets) is covered by the local-dev-environment work (plan Phase 5).

## Production build & deploy

`dotnet publish src/Jadlify.API/Jadlify.API.csproj -c Release` runs an MSBuild target (`BuildSpa`) that executes `npm ci` + `npm run build` here and copies `dist/**` into the published `wwwroot/`. The app serves `index.html` for client-side deep links while `/api/*` and `/health` stay server endpoints. CI builds and quality-gates the frontend (`npm ci`, `npm run lint`, `npm test`) before the publish step.

## Structure

- `src/auth/` — Supabase session context + route guard (`RequireAuth`).
- `src/api/` — Bearer-attaching API client, TanStack Query, `useMe`.
- `src/layout/` — responsive app shell (`AppShell`) and navigation.
- `src/routes/` — pages: `LoginPage` (public placeholder), `LandingPage` (home), and `sections/` placeholders for the MVP sections.
