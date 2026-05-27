---
bootstrapped_at: 2026-05-21T17:00:21.1708905+02:00
starter_id: dotnet
starter_name: ".NET (ASP.NET Core webapi)"
project_name: jadlify
language_family: dotnet
package_manager: dotnet
cwd_strategy: subdir-then-move
bootstrapper_confidence: verified
phase_3_status: ok
audit_command: "dotnet list package --vulnerable --include-transitive"
---

## Hand-off

```yaml
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
```

## Why this stack

Jadlify is a small, after-hours web-app MVP with a 4-week target and a user preference for a .NET backend with a React-capable frontend path. The selected `dotnet` starter is the recommended .NET option for this product shape: it gives a strongly typed ASP.NET Core API baseline with DI, OpenAPI, Entity Framework alignment, and verified bootstrapper support. Azure App Service matches the starter default, while GitHub Actions with auto-deploy on merge keeps the delivery path simple for a solo build. Auth is marked in scope because the PRD requires accounts and login; the accepted resolution is to add the ASP.NET authentication setup explicitly after scaffolding.

## Pre-scaffold verification

| Signal | Value | Severity | Notes |
| --- | --- | --- | --- |
| npm package | not run | n/a | non-JS starter |
| GitHub repo | not run | n/a | card docs_url is `https://learn.microsoft.com/aspnet/core`, not a GitHub repository URL |

## Scaffold log

**Resolved invocation**: `dotnet new webapi -n .bootstrap-scaffold --no-restore`, expanded for the requested Clean Architecture layout with `dotnet new sln -n Jadlify`, five production projects under `src/`, and four xUnit projects under `tests/`.

**Strategy**: scaffold into a temp directory then move files up

**Exit code**: 0

**Files moved**: 3 top-level entries: `src`, `tests`, `Jadlify.slnx`

**Conflicts (.scaffold siblings)**: none

**.gitignore handling**: created after scaffold via `dotnet new gitignore`

**.bootstrap-scaffold cleanup**: deleted

**Project layout**:

| Path | Kind |
| --- | --- |
| `src/Jadlify.API/Jadlify.API.csproj` | ASP.NET Core Web API |
| `src/Jadlify.Application/Jadlify.Application.csproj` | class library |
| `src/Jadlify.Infrastructure/Jadlify.Infrastructure.csproj` | class library |
| `src/Jadlify.Domain/Jadlify.Domain.csproj` | class library |
| `src/Jadlify.SharedKernel/Jadlify.SharedKernel.csproj` | class library |
| `tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj` | xUnit test project |
| `tests/Jadlify.Application.Tests/Jadlify.Application.Tests.csproj` | xUnit test project |
| `tests/Jadlify.Infrastructure.Tests/Jadlify.Infrastructure.Tests.csproj` | xUnit test project |
| `tests/Jadlify.Domain.Tests/Jadlify.Domain.Tests.csproj` | xUnit test project |

**Project references**:

```text
Jadlify.API -> Jadlify.Infrastructure
Jadlify.Infrastructure -> Jadlify.Application
Jadlify.Application -> Jadlify.Domain
Jadlify.Domain -> Jadlify.SharedKernel
```

## Post-scaffold audit

**Tool**: `dotnet list package --vulnerable --include-transitive`

**Summary**: 0 CRITICAL, 0 HIGH, 0 MODERATE, 0 LOW

**Direct vs transitive**: no vulnerable direct or transitive packages reported by NuGet for the audited API and test projects.

Audited projects:

| Project | Result |
| --- | --- |
| `src/Jadlify.API/Jadlify.API.csproj` | no vulnerable packages |
| `tests/Jadlify.API.Tests/Jadlify.API.Tests.csproj` | no vulnerable packages |
| `tests/Jadlify.Application.Tests/Jadlify.Application.Tests.csproj` | no vulnerable packages |
| `tests/Jadlify.Infrastructure.Tests/Jadlify.Infrastructure.Tests.csproj` | no vulnerable packages |
| `tests/Jadlify.Domain.Tests/Jadlify.Domain.Tests.csproj` | no vulnerable packages |

## Hints recorded but not acted on

| Hint | Value |
| --- | --- |
| bootstrapper_confidence | verified |
| quality_override | false |
| path_taken | standard |
| self_check_answers | null |
| team_size | solo |
| deployment_target | azure-app-service |
| ci_provider | github-actions |
| ci_default_flow | auto-deploy-on-merge |
| has_auth | true |
| has_payments | false |
| has_realtime | false |
| has_ai | false |
| has_background_jobs | false |

## Verification commands

```powershell
dotnet build Jadlify.slnx --no-restore -m:1 -p:UseSharedCompilation=false --verbosity minimal
dotnet test Jadlify.slnx --no-restore -m:1 -p:UseSharedCompilation=false --verbosity minimal
dotnet list src\Jadlify.API\Jadlify.API.csproj package --vulnerable --include-transitive
```

Notes:

- `dotnet restore` and `dotnet build` without `-m:1 -p:UseSharedCompilation=false` failed in this sandbox because the Roslyn/MSBuild compiler server could not use its named pipe reliably. The serialized build/test commands above passed.
- NuGet vulnerability audit needed network access to `https://api.nuget.org/v3/index.json`; the sandboxed first attempt failed against `127.0.0.1:9`, then the escalated audit completed.

## Next steps

Next: a future skill will set up agent context (CLAUDE.md, AGENTS.md). For now, your project is scaffolded and verified.

Useful manual steps in the meantime:

- Review the Clean Architecture dependency direction before adding feature code.
- Add authentication explicitly, because the hand-off marked `has_auth: true` and bootstrapper v1 does not implement auth.
- Add CI once you are ready to formalize the GitHub Actions flow.
