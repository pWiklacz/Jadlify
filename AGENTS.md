# Repository Guidelines

Jadlify is a .NET 10 ASP.NET Core web-app MVP for planning meals, matching daily macro targets, and generating shopping lists from planned recipes. Treat `@context/foundation/prd.md` and `@context/foundation/tech-stack.md` as the product and stack source of truth.

## Hard Rules

Keep writes out of `context/archive/`; archived changes are immutable. Do not add meal, recipe, product, goal, or shopping-list behavior without preserving per-user data isolation from the PRD guardrails. Macro calculations must stay deterministic and proportional to product values per 100g.

## Project Structure

The solution is `@Jadlify.slnx`. Runtime code lives under `src/`: `Jadlify.API` is the ASP.NET Core entrypoint, `Jadlify.Application` depends on Domain, `Jadlify.Domain` depends on SharedKernel, `Jadlify.Infrastructure` depends on Application, and `Jadlify.SharedKernel` has no project references. Keep that dependency direction when adding references. Tests mirror source projects under `tests/Jadlify.*.Tests`.

## Commands

Run `dotnet build Jadlify.slnx` to compile the full solution. Run `dotnet test Jadlify.slnx` for all xUnit test projects. Run a single layer with `dotnet test tests/Jadlify.Domain.Tests/Jadlify.Domain.Tests.csproj` or the matching test project path. Start the API locally with `dotnet run --project src/Jadlify.API/Jadlify.API.csproj`.

## Coding And Naming

Use file-scoped namespaces matching the project namespace, as in `@src/Jadlify.Domain/AssemblyReference.cs`. Keep layer-specific code in its owning project; shared abstractions belong in `Jadlify.SharedKernel` only when they are truly cross-layer.

## Testing

Use xUnit; follow `@tests/Jadlify.Domain.Tests/Jadlify.Domain.Tests.csproj` as the reference test-project shape. Add tests beside the layer they cover, not in a generic catch-all project. Replace the scaffolded `UnitTest1.cs` files as real behavior lands.

## Foundation Docs

`context/foundation/shape-notes.md`, `prd.md`, and `tech-stack.md` are living docs. Update them in place when product or stack decisions change; put change-scoped plans and reviews under `context/changes/`.

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
