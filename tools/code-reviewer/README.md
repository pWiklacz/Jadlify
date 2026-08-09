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
| `REVIEW_MODEL` | OpenRouter model id. Default: `anthropic/claude-sonnet-5`. |
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

Three fixtures, deliberately one of each kind:

- `evals/fixtures/user-scoping-leak.diff` — a shared-list endpoint that reads by
  id without scoping to the authenticated user. Must be caught and must fail.
- `evals/fixtures/clean-change.diff` — a small, correctly layered, tested change.
  Must pass: the guard against a reviewer that just says "fail" to everything.
  It is not spotless, though — its XML doc claims the constant is shared with the
  update validator, which the diff never touches — and the review must catch
  that. See the note on flatness below for why that assertion exists.
- `evals/fixtures/untested-new-logic.diff` — a non-trivial pure helper added with
  no test. `testCoverage` must drop below 5; the verdict is deliberately not
  asserted, because missing tests on a helper is a gap worth scoring, not a
  merge blocker. This one exists because of a real miss — see below.

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
- **Why the default is Sonnet 5 and not Haiku 4.5.** On PR #19 Haiku returned
  10/10 on every criterion with no findings, on a diff adding ~1400 lines of
  logic with no tests for any of it. Two causes, both now addressed: the
  `testCoverage` description enumerated product paths, so anything outside them
  had no anchor and defaulted to full marks, and nothing in the prompt said that
  an unevidenced criterion is not a 10. The prompt fix went in with the model
  change, and `untested-new-logic.diff` is the regression test for it.
- **A threshold-only eval hides the failure it was built to catch.** The first
  run of the four-model matrix was 100% green everywhere, which read as "no
  difference between models" — but the assertions only checked floors (`verdict`,
  `security <= 3`, `testCoverage >= 7`). Underneath, Haiku and DeepSeek were
  returning straight 10/10 with an empty findings array on `clean-change.diff`,
  while Opus and Sonnet found its real doc inconsistency. That flatness is
  exactly what made Haiku useless on PR #19. If you add a case here, ask what its
  assertion would let through, not just what it rejects.
- **The fixtures are small; the failure that motivated Sonnet was not.** Haiku
  passes every case in this set, because the fixtures are 1–1.3k tokens. It fell
  apart on a real 46k-character branch diff. Nothing here reproduces that yet, so
  do not read a green Haiku column as "Haiku is fine for CI".
- **Scores vary between runs.** The same model on the same diff has produced
  `security` scores on both sides of the `<= 3` assertion. Before you change the
  default model on the strength of this matrix, run it a few times — a single
  green run is the sample size of one that the eval set exists to replace.
