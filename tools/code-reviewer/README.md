# Jadlify code review agent

A scripted code reviewer built on the Vercel AI SDK with OpenRouter as the model
provider. It reads a git diff on stdin and returns a structured verdict: five
Jadlify-specific criteria scored 1-10, a binding pass/fail, concrete findings,
and a Markdown summary ready to post on a pull request.

Runs in two places:

- **CI** — `.github/workflows/ai-code-review.yml` on every PR to `main`.
- **Locally** — the same command, against any diff you can produce.

## Setup

Needs one secret, `OPENROUTER_API_KEY`, in both places:

- locally: export it, or drop it in `tools/code-reviewer/.env`
- in CI: repository *Settings → Secrets and variables → Actions*

```bash
cd tools/code-reviewer && npm ci
```

**Use npm 11.** `package-lock.json` is written by npm 11, and npm 10 refuses it:
the two disagree about optional peer dependencies, so npm 10 aborts with
`Missing: gcp-metadata@7.0.1 from lock file` on a lockfile that is otherwise
fine. Node 22 ships npm 10, which is why the workflow pins the major before
installing. If you regenerate the lockfile, do it with npm 11 as well.

## Running it

```bash
git diff main...HEAD | npm run --silent review
```

`--silent` is not optional when you redirect: without it npm prints its run
banner to stdout and corrupts the JSON.

Diagnostics go to stderr, the review JSON to stdout, so this works:

```bash
git diff main...HEAD | npm run --silent review > review.json
```

| Environment variable | Effect |
| --- | --- |
| `OPENROUTER_API_KEY` | Required. |
| `REVIEW_MODEL` | OpenRouter model id. Default: `anthropic/claude-haiku-4.5`. |
| `REVIEW_MAX_DIFF_CHARS` | Size cap for the diff sent to the model. Default 100 000. |
| `REVIEW_COMMENT_PATH` | Also write the rendered Markdown comment to this path. |
| `REVIEW_FAIL_ON_VERDICT` | `true` makes a `fail` verdict exit 1. Off by default locally; the workflow sets it so a `fail` shows up as a red job. Red is a signal, not a lock — blocking the merge additionally requires a branch protection rule that marks this check required. |
| `PR_TITLE`, `PR_BODY` | Extra context; the workflow fills these from the PR payload. |

## Layout

| Path | Role |
| --- | --- |
| `src/review-schema.ts` | The review contract: system prompt plus the zod schema. **The one file to edit when you change criteria.** |
| `src/agent.ts` | The agent itself — model, prompt assembly, `Output.object` enforcement. No CLI or CI concerns. |
| `src/review.ts` | CLI wrapper: stdin, environment, JSON out, comment rendering. |
| `src/diff.ts` | Diff filtering, review-priority ordering, size cap. |
| `evals/` | promptfoo fixtures and the provider that runs the agent. |

### Why the diff is reordered before review

The size cap is spent in file order, and on a large branch diff the planning
documents under `context/changes/` are big enough to consume the entire budget
before a single source file is reached — the review then grades the plan instead
of the code. `src/diff.ts` sorts `src/` and `tests/` first and prose last, so
truncation drops documentation rather than logic.

## Evals

`promptfooconfig.yaml` runs the agent against fixture diffs on three models side
by side, so "cheaper or pricier model" is answered with a matrix instead of a
hunch.

```bash
npm run eval        # run the matrix
npm run eval:view   # browse the results
```

Two fixtures, deliberately one of each kind:

- `evals/fixtures/user-scoping-leak.diff` — a shared-list endpoint that reads by
  id without scoping to the authenticated user. Must be caught and must fail.
- `evals/fixtures/clean-change.diff` — a small, correctly layered, tested change.
  Must pass. This one is the guard against a reviewer that just says "fail" to
  everything.

Notes for whoever touches this next:

- The providers call `src/agent.ts` through `evals/agent-provider.ts` rather than
  evaluating a copy of the prompt. Evaluating a detached prompt does not work
  here: without `Output.object` the models answer with Markdown fences or a
  reasoning preamble, and every assertion fails on formatting instead of on
  review quality.
- promptfoo is pinned to `0.120.19` because newer releases require Node
  >= 22.22.0. Raise the pin once the toolchain moves.
- The `.ts` provider needs a TypeScript loader, which is why the npm script sets
  `NODE_OPTIONS="--import tsx"`.
- **Scores vary between runs.** The same model on the same diff has produced
  `security` scores on both sides of the `<= 3` assertion. Before you change the
  default model on the strength of this matrix, run it a few times — a single
  green run is the sample size of one that the eval set exists to replace.
