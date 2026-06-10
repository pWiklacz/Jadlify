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

Local auth runs against a Supabase CLI stack defined by `supabase/config.toml` (`project_id = "jadlify"`, dedicated `544xx` ports so it coexists with other local Supabase stacks). Run `supabase start` / `supabase status`, then put the printed values in the SPA `.env` and the API user secrets (`SupabaseAuth:Authority`/`Issuer`/`Audience`). Supabase signs user tokens asymmetrically (ES256); the API validates them via JWKS discovered from `Authority` — same as production, except dev sets `SupabaseAuth:RequireHttpsMetadata=false` because the local discovery endpoint is HTTP. Full steps in `src/Jadlify.Web/README.md`.

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


## 10xDevs AI Toolkit - Module 3, Lesson 3

Lesson 3 is about **hooks** — turning the quality gates from Lesson 1 and the tests from Lesson 2 into automatic, deterministic checks that fire while the agent works. A hook runs outside the model, so it survives context compression, instruction changes, and the model "forgetting". The payoff for agentic hooks specifically: a `PostToolUse` check can feed its result back into the agent's context, so the agent fixes trivial errors (formatting, a missing import, a wrong type) on its own in the next iteration instead of you discovering them minutes later.

```
context/foundation/test-plan.md  (§4 Quality Gates: which check, required when)
        │
        ▼  (assign each gate to the cheapest layer that still gives signal)
   per-edit (agent hooks)  →  pre-commit (git hooks)  →  pre-push  →  CI
        │ lint, format, scoped tests          │ staged       │ heavier    │ integration
        ▼
   exit code + stdout  →  additionalContext  →  agent reacts next turn
```

### Task Router — Which layer for this check

| You want to | Do this |
| --- | --- |
| React the instant the agent edits a file | A per-edit hook (`PostToolUse` matcher `Write\|Edit` in Claude Code). Right for fast checks: lint/format, and scoped tests on risk-area files. This is the **only** layer that can hand feedback to the agent mid-session. |
| Run only the tests that depend on the edited file | Parse the path from the hook's stdin (`jq -r .tool_input.file_path`) and run your runner's related-tests mode (`vitest related "$FILE" --run`, `jest --findRelatedTests $FILE`). Gate it on whether the file is a risk area in `test-plan.md`; don't run tests on every helper or config edit. |
| Catch changes that bypassed the agent (manual edits, a teammate's commit) | A pre-commit git hook (Lefthook or Husky+lint-staged) over staged files: lint + typecheck, and tests on staged risk files. |
| Run heavier checks before code leaves the machine | Pre-push: full typecheck or a broader test set. Anything too slow for per-edit moves here. |
| Decide where a given gate belongs | Ask: is it fast enough (a few seconds) for per-edit, or should it wait for commit/push/CI? Slow checks block the agent loop on every edit — push them up a layer. |
| Use the same hook across tools | The trigger → matcher → handler → signal pattern is the same in Cursor, Codex, Windsurf, and Copilot; only the config file and event names change. See the cross-tool table below. |

### Hook lifecycle — the universal pattern

Every tool's hooks follow four steps:

1. **Trigger** — an event in the tool (e.g. the agent just saved a file: `PostToolUse`).
2. **Matcher** — a filter deciding whether this hook runs (tool name like `Write`/`Edit`, file type, or a name pattern).
3. **Handler** — the action that runs, usually a shell command.
4. **Signal** — the result returns to the tool. The exit code says pass/fail; stdout can flow into the agent's context as feedback.

### Exit codes and the feedback loop

- **0** — success; the hook passed, continue.
- **2** — blocking error; the agent sees the feedback and should react.
- **anything else** — non-blocking error; logged, but does not interrupt work.

On a blocking failure, stdout flows into the agent's context (in Claude Code via `additionalContext`, capped at 10,000 characters; other tools have similar mechanisms with their own limits). That is why the agent can self-correct: it sees the concrete message — missing type, unimported module, badly formatted line — not just "something failed".

The boundary: the agent reliably fixes **trivial** corrections on its own. When a test fails because of wrong business logic, the hook surfaces it but the agent may not diagnose the real cause — it says "something is off" and tries a trivial fix. If that does not resolve in one or two tries, the signal comes back to you, and the problem may deserve its own change-id with the full `/10x-new → /10x-research → /10x-plan → /10x-implement` workflow.

### Three local layers (plus CI)

| Layer | Catches | Timing |
| --- | --- | --- |
| Per-edit (agent hooks) | Formatting, simple type errors, failing unit tests on risk files. Only layer that feeds the agent mid-work. | ms–s |
| Pre-commit (git hooks) | What slipped past per-edit: manual edits, files changed outside the hook, checks too slow for per-edit. Operates on staged files. | s |
| Pre-push | Heavier checks before pushing to remote (full typecheck, broader test set). | s–min |
| CI | Integration problems, cross-module dependencies, checks needing infra unavailable locally. | min |

Local layers do **not** replace CI — CI stays the key verification for shared repo state and environments you don't control. But each local layer that catches an error is one fewer CI round-trip. You don't need all layers from day one: start with one per-edit hook (lint) and one commit gate, add layers as you see what escapes. The quality gates in `test-plan.md §4` decide which checks are worth automating and when; a plan may legitimately defer per-edit hooks if the cost/signal ratio isn't there yet.

### Key rules

- Keep per-edit hooks fast. If a check takes more than a few seconds, move it to commit, push, or CI — a slow per-edit hook blocks the agent loop on every edit. Lint/format are ideal per-edit; full typecheck is often a commit gate in larger projects.
- Run scoped tests, not the whole suite, per edit — only tests related to the edited file, and only when that file is a risk area in `test-plan.md`.
- `related` is a subcommand, not a flag (`vitest related`, not `--related`). Use `--run` so the hook terminates instead of entering watch mode.
- `PostToolUse` fires once per tool use; three edits in one turn fire it three times independently — there is no built-in aggregation.
- The git hook tool (Lefthook vs Husky+lint-staged) is an implementation detail; the rule is the same — run checks on staged files before commit. If Husky already works, don't migrate.
- **Context injection is not universal.** Claude Code, Cursor, Codex, and Copilot (in VS Code) can pass a hook's result to the agent; Windsurf cannot — it can block (exit 2) but can't tell the agent what went wrong.

### The same pattern in every tool

| Tool | Events | Handlers | Context injection | Config |
| --- | --- | --- | --- | --- |
| Claude Code | ~30 | command, http, mcp_tool, prompt, agent | yes | `.claude/settings.json` |
| Cursor | ~18 | command, prompt | yes | `.cursor/hooks.json` |
| Codex | 10 | command | yes | `.codex/hooks.json` |
| Windsurf | 12 | command | **no** | `.windsurf/hooks.json` |
| Copilot | ~13 | command, http, prompt | yes (VS Code) | `.github/hooks/*.json` |

### Lesson boundaries

- This lesson configures hooks and local quality layers only. The hook JSON, `lefthook.yml`, and the per-edit/commit/push layering are the scope.
- Do not write E2E tests, configure Playwright/MCP, or run browser scenarios. That is Lesson 4.
- Do not run the bug-to-fix-to-regression-test debugging workflow. That is Lesson 5.
- Do not change the risk strategy or quality-gate definitions. That is Lesson 1 (`/10x-test-plan`); read current state with `/10x-test-plan --status`.
- Do not write unit/integration test code from scratch here. That is Lesson 2 — hooks only *run* the tests those lessons produced.
- Do not author CI/CD pipelines. That is Module 1 Lesson 5 / Module 2 Lesson 5; hooks are the local layers in front of CI.

### Paths used by this lesson

- `.claude/settings.json` — hook configuration (`~/.claude/settings.json` global, `.claude/settings.json` project, `.claude/settings.local.json` local overrides). Other tools use their own config file (see the table).
- `lefthook.yml` — pre-commit git hook config (lint + typecheck + tests on `{staged_files}`).
- `context/foundation/test-plan.md` — §4 quality gates decide which checks to automate and at which layer; risk areas decide which edits warrant scoped tests.

<!-- END @przeprogramowani/10x-cli -->
