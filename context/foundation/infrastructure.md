---
project: Jadlify
researched_at: 2026-05-23
recommended_platform: Azure App Service F1
runner_up: Render Free
context_type: mvp
tech_stack:
  language: C# / .NET 10
  framework: ASP.NET Core API + React static frontend
  runtime: .NET 10 LTS on Azure App Service
  auth_provider: Supabase Auth
  database_provider: Supabase Postgres
---

## Recommendation

**Deploy on Azure App Service F1 for the course/MVP phase.**

Jadlify is a small, low-QPS ASP.NET Core / .NET 10 web app, and the accepted frontend model is a single deployment: build React to static assets and serve it from the ASP.NET Core app. Azure App Service F1 wins because it is the only researched option that gives a native .NET 10 App Service path with a $0 start, one origin for frontend and API, and no Docker requirement. Render Free is the runner-up because it can also run frontend + API together for $0, but only through Docker and with idle sleep/cold-start behavior.

The decision is intentionally optimized for "free course app first". When the app becomes something that should be reliably fast and always warm, revisit the hosting tier or switch to a low paid container PaaS.

## Platform Comparison

| Platform | Runtime fit | CLI-first | Managed/serverless | Agent-readable docs | Stable deploy API | MCP / integration | Cost fit | Total |
|---|---|---|---|---|---|---|---|---|
| Azure App Service F1 | Pass | Pass | Pass | Partial | Pass | Pass | Pass | Recommended |
| Render Free | Pass via Docker | Pass | Pass | Pass | Partial | Pass | Pass | Runner-up |
| Railway Free | Pass via Docker | Pass | Pass | Pass | Partial | Pass | Partial | Third |
| Fly.io | Pass via Docker | Pass | Pass | Partial | Pass | Pass | Partial | Viable paid option |
| Cloudflare Workers + Pages | Partial via Containers | Pass | Pass | Pass | Pass | Pass | Partial | Not primary for ASP.NET Core |
| Vercel | Fail for ASP.NET Core API | Pass | Pass | Pass | Pass | Partial | Pass | Frontend-only fit |
| Netlify | Fail for ASP.NET Core API | Pass | Pass | Pass | Partial | Pass | Pass | Frontend-only fit |

**Azure App Service F1** supports the app's chosen backend directly: ASP.NET Core / .NET 10. It also keeps the React frontend and API under one host, which avoids CORS, split-domain cookie issues, and duplicated deploy ownership. The tradeoff is that the free tier is constrained: no Always On, no staging slots, and limited resources.

**Render Free** can run Jadlify as one Dockerized web service. This is attractive for a course demo because it stays free and has simpler platform concepts than Azure. Its key downside is operational: free web services sleep after inactivity and wake slowly, which makes the first request look broken or sluggish.

**Railway** has excellent developer experience and good agent-readable docs, but the free plan is not a durable free hosting plan. Railway's ASP.NET Core guide currently requires a Dockerfile because Railpack does not yet support .NET, and practical use usually moves to Hobby at about $5/month.

**Fly.io** is technically strong for .NET via Docker and has excellent CLI operations, but it is pay-as-you-go and not the best match for a "free first" course deployment. Managed Postgres on Fly is also too expensive for this MVP's current budget.

**Cloudflare, Vercel, and Netlify** are excellent for static React hosting and agent workflows, but they are not the right primary host for Jadlify's ASP.NET Core API. Choosing one of them now would force a split frontend/API deployment or a container workaround, adding CORS, auth, and deploy complexity before the product needs it.

### Shortlisted Platforms

#### 1. Azure App Service F1 (Recommended)

Best fit for the current stack and constraints: .NET 10 first, one web app, no Docker required, and a real $0 starting tier. It matches the existing `tech-stack.md` deployment target and keeps the operational model simple enough for a course project.

#### 2. Render Free

Best free fallback if the project wants to exercise Docker early. It keeps frontend + API together, but the free tier's sleep/cold-start behavior must be treated as acceptable demo friction, not a production posture.

#### 3. Railway Free / Hobby

Best developer experience among the PaaS options, but less aligned with "free hosting" because the free credit is tiny and durable use tends to require Hobby. Keep it as the paid convenience option if Azure feels too heavy.

## Anti-Bias Cross-Check: Azure App Service F1

### Devil's Advocate - Weaknesses

1. The free F1 tier can make the app feel unreliable because there is no Always On and resources are limited.
2. Azure adds account, resource group, app service plan, app settings, and deployment concepts that are heavier than Render/Railway for a solo course project.
3. Rollback on the free tier is weaker than on paid tiers because deployment slots are not available on F1.
4. If .NET 10 runtime availability differs by region or OS image, the first deploy can fail unless the App Service runtime is checked during setup.
5. Database cost is not solved by App Service F1; the app uses a separate Supabase Postgres data layer, so connection strings, free-tier limits, and migration safety must be handled outside Azure hosting.

### Pre-Mortem - How This Could Fail

The team chose Azure App Service F1 because it looked like the cleanest free .NET host. The first deploy worked, but the app later felt inconsistent during demos: after idle periods, the first request was slow and the frontend looked like it was broken. Because the project stayed on the free tier, there were no deployment slots, so every production update was a direct deploy. One bad migration or bad static asset build required manual recovery rather than a clean slot swap. The team also underestimated how many Azure concepts had to be understood just to debug a small app: resource groups, plan tiers, app settings, publish profiles, runtime stack, logs, and Kudu-style deployment behavior. Finally, hosting and data now span two platforms, so a bad Supabase connection string, auth setting, or migration can still break the app even when the App Service deploy succeeds.

### Unknown Unknowns

- F1 is useful for course/demo hosting, but it is not a realistic "always responsive" production tier.
- Deployment slots and clean blue/green rollback are paid-tier features; the free-tier rollback story is mostly redeploying a known-good package.
- Serving React from ASP.NET Core is simpler operationally, but it means frontend preview deploys are not as rich as Vercel/Netlify/Cloudflare Pages.
- The first deploy should verify .NET 10 runtime availability in the chosen App Service region before relying on it.
- The database is separate from Azure App Service: use Supabase Postgres, and keep DB credentials plus Supabase auth/JWT settings in Azure App Service application settings.

## Operational Story

- **Preview deploys**: MVP default is no automatic preview deploys. Pull requests run build/test only. For manual previews, create a temporary App Service app or use a separate resource group; do not use production for exploratory changes.
- **Secrets**: Store connection strings and API tokens in Azure App Service application settings. Do not commit them to `appsettings*.json`. GitHub Actions should use a scoped publish profile or federated Azure credential stored in GitHub Secrets.
- **Rollback**: On F1, keep the last known-good publish ZIP or GitHub Actions artifact and redeploy it with `az webapp deploy`. Deployment slots become the preferred rollback path only after moving to a tier that supports slots.
- **Approval**: Human approval is required for production deploy, tier upgrades, database deletion, primary secret rotation, and any change that can spend money. An agent may read logs, build artifacts, and prepare deploy commands.
- **Logs**: Read runtime logs with `az webapp log tail --resource-group <rg> --name <app>` after logging is enabled. Read deployment logs with `az webapp log deployment list/show`.

## Risk Register

| Risk | Source | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| Free tier cold start or slow first request makes demos look broken | Devil's advocate | M | M | Mention F1 limits in README/deploy notes; upgrade only if course demo needs always-warm behavior. |
| No deployment slots on F1 means weak rollback | Devil's advocate | M | M | Keep versioned publish artifacts and document redeploy rollback; move to Basic/Standard before production use. |
| Azure operational complexity slows the first deploy | Pre-mortem | M | M | Keep deployment plan short, scripted, and App Service-only; avoid adding Azure SQL, Key Vault, or CDN until needed. |
| .NET 10 runtime mismatch in chosen region/OS | Unknown unknowns | L | H | During setup, create a test App Service in the target region and confirm the runtime before wiring CI. |
| Supabase data/auth configuration drifts from Azure app settings | Stack decision | M | H | Document required Supabase URL, JWT/JWKS settings, anon/public key, and database connection string before implementing persistence; verify them in local and deployed smoke checks. |
| React static hosting through ASP.NET Core reduces frontend preview ergonomics | Unknown unknowns | M | L | Accept for MVP; split frontend to a static platform only when preview deploys become a real workflow need. |
| Direct production deploy breaks auth/static assets | Pre-mortem | M | M | Require `dotnet test Jadlify.slnx` and a local `dotnet publish` smoke check before manual deploy. |

## Getting Started

1. Add the React app so its production build is copied into the ASP.NET Core app's static asset directory, then configure ASP.NET Core fallback routing to `index.html`.
2. Verify locally: `dotnet test Jadlify.slnx` and `dotnet publish src/Jadlify.API/Jadlify.API.csproj -c Release -o .publish`.
3. Create the free Azure host: `az group create --name rg-jadlify-dev --location westeurope`, then create an App Service plan with `--sku F1`, then create the web app with the .NET 10 runtime.
4. Deploy the publish output as a ZIP with `az webapp deploy --resource-group rg-jadlify-dev --name <app-name> --src-path <publish.zip> --type zip`.
5. Enable logs, tail startup output, and record the exact deploy commands in `context/deployment/deploy-plan.md` before automating with GitHub Actions.

## Evidence Links

- Azure App Service ASP.NET Core / .NET 10 quickstart: https://learn.microsoft.com/en-ca/azure/app-service/quickstart-dotnetcore
- Azure ZIP/package deployment: https://learn.microsoft.com/en-us/azure/app-service/deploy-run-package
- Azure App Service deployment slots: https://learn.microsoft.com/en-us/azure/app-service/deploy-staging-slots
- Azure CLI web app logs: https://learn.microsoft.com/en-us/cli/azure/webapp/log
- Azure MCP Server for App Service: https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/services/azure-mcp-server-for-app-service
- Render Docker/language support: https://render.com/docs/language-support
- Render free tier: https://render.com/docs/free
- Railway ASP.NET Core guide: https://docs.railway.com/guides/aspnet-core
- Railway pricing: https://docs.railway.com/pricing
- Fly.io .NET guide: https://fly.io/docs/languages-and-frameworks/dotnet/
- Cloudflare Workers languages: https://developers.cloudflare.com/workers/languages/
- Vercel function runtimes: https://vercel.com/docs/functions/runtimes
- Netlify functions overview: https://docs.netlify.com/build/functions/overview/

## Out of Scope

The following were not evaluated in this research:

- Docker image implementation details
- Full CI/CD pipeline setup
- Supabase project provisioning details
- Production-scale architecture such as multi-region HA, disaster recovery, and formal SLA design
