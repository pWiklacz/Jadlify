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

In production these are provided as build-time env on the CI runner / Azure build. For local dev, get the values from a local Supabase stack — see below.

## Local Supabase stack (dev auth)

Local auth runs against a Supabase CLI stack (Docker) defined by `supabase/config.toml` at the repo root. It uses `project_id = "jadlify"` and a dedicated `544xx` host-port range, so it **coexists** with any other project's default-port (`5432x`) Supabase stack on the same machine — the CLI namespaces all containers/volumes per `project_id`.

```bash
# from the repo root — starts only the Jadlify stack (API on http://127.0.0.1:54421)
supabase start
supabase status          # prints URLs, anon key, and service_role key
supabase stop            # stops only the Jadlify stack
```

Copy the printed values into the two processes (none of these are committed):

1. **SPA** — `src/Jadlify.Web/.env`:

   ```bash
   VITE_SUPABASE_URL=http://127.0.0.1:54421
   VITE_SUPABASE_ANON_KEY=<anon key from "supabase status">
   ```

2. **API** — user secrets (run from `src/Jadlify.API`). Supabase signs user access tokens **asymmetrically (ES256)** and publishes the public key via JWKS, so the API validates them by discovering that key from `SupabaseAuth:Authority` — the same mechanism production uses. The local stack serves discovery over **HTTP**, so dev sets `RequireHttpsMetadata` to `false` (production leaves it `true`):

   ```bash
   dotnet user-secrets set "SupabaseAuth:Authority"            "http://127.0.0.1:54421/auth/v1"
   dotnet user-secrets set "SupabaseAuth:Issuer"               "http://127.0.0.1:54421/auth/v1"
   dotnet user-secrets set "SupabaseAuth:Audience"             "authenticated"
   dotnet user-secrets set "SupabaseAuth:RequireHttpsMetadata" "false"
   ```

Create a local test user via Supabase Studio (`http://127.0.0.1:54423` → Authentication → Add user, "Auto Confirm User") or the GoTrue admin API using the service-role key:

```bash
curl -X POST http://127.0.0.1:54421/auth/v1/admin/users \
  -H "apikey: <service_role key>" -H "Authorization: Bearer <service_role key>" \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@jadlify.local","password":"password123","email_confirm":true}'
```

Then run the two-process dev loop (backend + `npm run dev`), sign in, and the landing page renders the `/api/me` result — confirming the bearer token reaches the backend.

## Production build & deploy

`dotnet publish src/Jadlify.API/Jadlify.API.csproj -c Release` runs an MSBuild target (`BuildSpa`) that executes `npm ci` + `npm run build` here and copies `dist/**` into the published `wwwroot/`. The app serves `index.html` for client-side deep links while `/api/*` and `/health` stay server endpoints. CI builds and quality-gates the frontend (`npm ci`, `npm run lint`, `npm test`) before the publish step.

## Structure

- `src/auth/` — Supabase session context + route guard (`RequireAuth`).
- `src/api/` — Bearer-attaching API client, TanStack Query, `useMe`.
- `src/layout/` — responsive app shell (`AppShell`) and navigation.
- `src/routes/` — pages: `LoginPage` (public placeholder), `LandingPage` (home), and `sections/` placeholders for the MVP sections.
