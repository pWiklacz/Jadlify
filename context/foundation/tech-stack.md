---
starter_id: dotnet
package_manager: dotnet
project_name: jadlify
hints:
  language_family: dotnet
  team_size: solo
  deployment_target: azure-app-service
  ci_provider: github-actions
  ci_default_flow: auto-deploy-on-merge
  auth_provider: supabase-auth
  database_provider: supabase-postgres
  data_access_model: backend-api-only
  bootstrapper_confidence: verified
  path_taken: standard
  quality_override: false
  self_check_answers: null
  has_auth: true
  has_payments: false
  has_realtime: false
  has_ai: false
  has_background_jobs: false
---

## Why this stack

Jadlify is a small, after-hours web-app MVP with a 4-week target and a user preference for a .NET backend with a React-capable frontend path. The selected `dotnet` starter is the recommended .NET option for this product shape: it gives a strongly typed ASP.NET Core API baseline with DI, OpenAPI, Entity Framework alignment, and verified bootstrapper support. Azure App Service matches the starter default, while GitHub Actions with auto-deploy on merge keeps the delivery path simple for a solo build. Auth is marked in scope because the PRD requires accounts and login; the accepted resolution is to add the ASP.NET authentication setup explicitly after scaffolding.

## Auth and data decision

Use Supabase for the MVP identity and data platform: Supabase Auth for sign-up/sign-in/session issuing, and Supabase Postgres for persisted application data. The React frontend may use the Supabase client only for authentication/session management. Product, recipe, goal, meal-plan, macro-summary, and shopping-list behavior must go through the ASP.NET Core API.

The ASP.NET Core API validates Supabase-issued JWT bearer tokens, treats the token `sub` claim as the stable application user id, and enforces per-user authorization in the application/data-access layer. User-owned tables must include a user id column, and repositories/queries/commands must be scoped to the current authenticated user. Supabase Row Level Security can be added later as defense in depth, but the MVP plan must not rely on direct browser-to-table access for core domain data.

Use EF Core with the Npgsql provider for backend persistence against Supabase Postgres. Store Supabase URL, JWT/JWKS settings, anon/public client key, service secrets, and database connection strings in local user secrets or Azure App Service application settings; do not commit secrets to `appsettings*.json`.
