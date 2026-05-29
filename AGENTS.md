# Repository Guidelines

Jadlify is a .NET 10 ASP.NET Core web-app MVP for planning meals, matching daily macro targets, and generating shopping lists from planned recipes. Treat `@context/foundation/prd.md` and `@context/foundation/tech-stack.md` as the product and stack source of truth.

## Hard Rules

Keep writes out of `context/archive/`; archived changes are immutable. Do not add meal, recipe, product, goal, or shopping-list behavior without preserving per-user data isolation from the PRD guardrails. Macro calculations must stay deterministic and proportional to product values per 100g.

## Project Structure

The solution is `@Jadlify.slnx`. Runtime code lives under `src/`: `Jadlify.API` is the ASP.NET Core entrypoint, `Jadlify.Application` depends on Domain, `Jadlify.Domain` depends on SharedKernel, `Jadlify.Infrastructure` depends on Application, and `Jadlify.SharedKernel` has no project references. Keep that dependency direction when adding references. Tests mirror source projects under `tests/Jadlify.*.Tests`.

## Build, Test, and Run Commands

Run commands from the repo root. Target framework: .NET 10. Solution file: `phrAIse.slnx`.

Agents must use the minimal scripts for build/test/format/verify work. Do not run raw `dotnet build`, `dotnet test`, `dotnet format`, or `dotnet restore` for normal repo verification.

```bash
pwsh ./.scripts/build-min.ps1
pwsh ./.scripts/test-min.ps1
pwsh ./.scripts/format-min.ps1
pwsh ./.scripts/verify-min.ps1
```

Useful script options:

- `-Project` for build/test/format scripts.
- `-BuildProject` and `-TestProject` for `verify-min.ps1`.
- `-Name`, `-Class`, `-FullyQualifiedNameContains`, or `-Filter` for targeted tests.
- `-Help` on any script for supported parameters.

Run these scripts sequentially because they share `.logs/*.log` files and some integration tests share local resources.

### Frontend (`src/Jadlify.Web`)

The SPA is a Vite + React + TypeScript app served single-origin from the API's `wwwroot`. Local dev runs **two processes**: the backend and the Vite dev server, which proxies `/api` and `/health` to `https://localhost:7206` (no CORS).

```bash
dotnet run --project src/Jadlify.API   # terminal 1: backend
cd src/Jadlify.Web && npm run dev       # terminal 2: Vite dev server (HMR)
```

Frontend npm scripts (run from `src/Jadlify.Web`): `npm run lint`, `npm test` (Vitest + RTL), `npm run build`. `dotnet publish -c Release` bundles the SPA into the API `wwwroot` via an MSBuild target, so the publish command is unchanged. See `src/Jadlify.Web/README.md` for details.

## Coding And Naming

Use file-scoped namespaces matching the project namespace, as in `@src/Jadlify.Domain/AssemblyReference.cs`. Keep layer-specific code in its owning project; shared abstractions belong in `Jadlify.SharedKernel` only when they are truly cross-layer.

## Coding Conventions

`.editorconfig` and `Directory.Build.props` are the canonical style contract. Fix violations they enforce before committing.

Keep edits scoped to the feature, project, and ownership boundary implied by the task. Do not perform opportunistic refactors in unrelated areas.

## Testing

Use xUnit; follow `@tests/Jadlify.Domain.Tests/Jadlify.Domain.Tests.csproj` as the reference test-project shape. Add tests beside the layer they cover, not in a generic catch-all project. Replace the scaffolded `UnitTest1.cs` files as real behavior lands.

## Foundation Docs

`context/foundation/shape-notes.md`, `prd.md`, and `tech-stack.md` are living docs. Update them in place when product or stack decisions change; put change-scoped plans and reviews under `context/changes/`.

## Git Workflow

Follow `@context/standards/github-workflow.md`. Do not push directly to `master`.

Before committing or opening a PR, run the narrowest relevant verification script and state what was run.

<!-- BEGIN @przeprogramowani/10x-cli -->

## 10xDevs AI Toolkit - Module 2, Lesson 2

Turn one roadmap item into the first implementation cycle with the **change planning chain**:

```
/10x-roadmap -> /10x-new -> /10x-plan -> /10x-plan-review -> /10x-implement
```

`/10x-new`, `/10x-plan`, `/10x-plan-review`, and `/10x-implement` are the lesson focus. `/10x-frame` and `/10x-research` are not required rituals here; they are escalation paths introduced in the next lesson.

### Task Router - Where to start

| Skill | Use it when |
| --- | --- |
| **Change setup (lesson focus)** | |
| `/10x-new <change-id>` | You selected a roadmap item and need a stable change folder. Creates `context/changes/<change-id>/change.md` so planning, implementation, progress, commits, and later review all share one identity. Use AFTER roadmap selection, BEFORE `/10x-plan`. |
| **Planning (lesson focus)** | |
| `/10x-plan <change-id>` | You have a change folder and need a reviewable implementation plan. Reads roadmap context, foundation docs, codebase evidence, and any existing change notes; writes `plan.md` and `plan-brief.md` with phases, file contracts, success criteria, and `## Progress`. |
| **Plan readiness (lesson focus)** | |
| `/10x-plan-review <change-id>` | You have `plan.md` and need a light pre-code readiness check. Use it to catch missing end state, weak contracts, malformed progress, scope drift, or blind spots before code changes begin. |
| **Implementation (lesson focus)** | |
| `/10x-implement <change-id> phase <n>` | You have an approved plan and want to execute one phase with verification, manual gate, commit ritual, and SHA write-back to `## Progress`. |
| **Lifecycle closure** | |
| `/10x-archive <change-id>` | A change is merged or intentionally closed. Move it out of active `context/changes/` into archive state. |

### How the chain hands off

- `/10x-new` creates the durable change identity.
- `/10x-plan` turns that identity into an implementation contract.
- `/10x-plan-review` checks the plan before the agent mutates code.
- `/10x-implement` executes one planned phase, verifies, asks for manual confirmation when needed, commits, and records progress.

### Lesson boundaries

- Plan is the default router after roadmap selection. Start with `/10x-plan` unless the problem is unclear or external evidence is blocking.
- Do not run `/10x-frame + /10x-research` as ceremony for every change.
- Do not turn this lesson into a full end-to-end product build. A checkpoint with a planned and partially or fully implemented stream is valid.
- Code review of the implemented diff belongs to Lesson 3 via `/10x-impl-review`.
- Lifecycle closure via `/10x-archive` after a change is merged or intentionally closed.

### Paths used by this lesson

- `context/foundation/roadmap.md` - upstream roadmap
- `context/changes/<change-id>/change.md` - change identity
- `context/changes/<change-id>/plan.md` - implementation contract
- `context/changes/<change-id>/plan-brief.md` - compressed handoff
- `context/foundation/lessons.md` - recurring rules and pitfalls
- `docs/reference/contract-surfaces.md` - load-bearing names registry

Skills must not write to `context/archive/`. Archived changes are immutable; if a resolved target path starts with `context/archive/`, abort with: "This change is archived. Open a new change with `/10x-new` instead."

<!-- END @przeprogramowani/10x-cli -->
