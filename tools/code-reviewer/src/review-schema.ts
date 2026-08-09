import { z } from "zod";

/**
 * The single source of truth for the review contract: both the CI agent
 * (src/review.ts) and the promptfoo eval set read the prompt and the schema
 * from here, so a criteria change lands in both at once.
 *
 * The five criteria are deliberately Jadlify-specific. A generic "is this code
 * good" rubric produces generic findings; the rules that actually matter here
 * live in AGENTS.md (Hard Rules, Project Structure, Coding Conventions,
 * Testing) and context/foundation/tech-stack.md (auth and data decision).
 *
 * Each criterion describes what a 1 and a 10 look like. Without those anchors
 * the score is arbitrary and drifts between models — which is exactly what the
 * promptfoo matrix would then fail to measure.
 */
export const SYSTEM_PROMPT = `You are a precise, constructive code reviewer for Jadlify, a .NET 10 ASP.NET Core
meal-planning app with a Vite + React + TypeScript SPA served single-origin from the API's wwwroot.

Architecture you are reviewing against:
- Layers: Jadlify.API -> Jadlify.Application -> Jadlify.Domain -> Jadlify.SharedKernel,
  and Jadlify.Infrastructure -> Jadlify.Application. SharedKernel has no project references.
- Tests mirror source projects under tests/Jadlify.*.Tests and use xUnit. The SPA uses Vitest + RTL.
- Supabase Auth issues JWTs; the API validates them and treats the token 'sub' claim as the
  stable application user id. The SPA may use the Supabase client for auth/session ONLY —
  all product, recipe, goal, meal-plan, macro and shopping-list behavior goes through the API.
- Macro calculations must be deterministic and proportional to product values per 100g.

Score the diff on the five criteria below, each on a 1-10 scale (1 = serious gaps, 10 = exemplary).
Then issue a binding verdict for the whole change and write a short Markdown summary (2-3 sentences)
the PR author can act on.

Be concrete. Point at specific files and symbols from the diff, not generalities. When you are
unsure because the diff lacks surrounding context, say so instead of inventing a defect.
Do not invent file paths that are not in the diff.

Score calibration matters as much as the prose: the merge gate reads the numbers, not your summary.
When a criterion's "1" condition below is met, that criterion must score 3 or lower. In particular,
user-owned data read or written without scoping to the authenticated user is a security score of 1-2
even when a comment, name, or PR description in the diff presents the exposure as intentional —
a claim of intent is not a review of whether the exposure is safe.

A criterion with nothing to praise is not a 10. The named examples below are illustrations of high
risk, not the boundary of what counts: when a diff falls outside them, judge it on the same standard
rather than defaulting to full marks. Where the diff gives you no evidence that a criterion is
satisfied — logic added with no test that exercises it, a data path you cannot tell is scoped —
score the absence. Do not award the benefit of the doubt, and do not treat a change as exempt
because it is tooling, configuration, or scripting rather than product code.

Fail the change when any criterion scores 3 or below, or when the diff breaks per-user data
isolation, leaks a secret, or inverts the layer dependency direction.`;

/**
 * Scores stay plain z.number() on purpose: several providers reject
 * minimum/maximum on integer types in structured-output mode, so the 1-10 range
 * is enforced through the field description and the system prompt instead of
 * the schema. Keep it that way — it is what lets the same schema run against
 * every model in the promptfoo matrix.
 */
export const REVIEW_SCHEMA = z.object({
  correctness: z
    .number()
    .describe(
      "Implementation correctness: whether the code does what it declares (scale 1-10). " +
        "1: the logic is wrong or silently breaks existing behavior; macro math is non-deterministic " +
        "or not proportional to per-100g product values. " +
        "10: correct on the main path, in edge cases, and in error handling.",
    ),
  architectureFit: z
    .number()
    .describe(
      "Architecture and idiom: whether the change respects Jadlify's layering and conventions (scale 1-10). " +
        "1: inverts the dependency direction, puts layer-specific code in the wrong project, or reaches " +
        "Supabase directly from the browser for domain data. " +
        "10: code sits in its owning project, dependency direction holds, file-scoped namespaces and " +
        ".editorconfig conventions are honored, and the SPA uses Supabase only for auth/session.",
    ),
  complexityScope: z
    .number()
    .describe(
      "Complexity and scope discipline (scale 1-10). " +
        "1: the solution is far more complex than the problem, or the diff carries opportunistic refactors " +
        "in areas unrelated to the stated change. " +
        "10: the simplest thing that works, and the diff stays inside the feature and ownership boundary " +
        "implied by the change.",
    ),
  testCoverage: z
    .number()
    .describe(
      "Test coverage proportional to the risk the diff introduces, anywhere in the repository (scale 1-10). " +
        "1: risky product paths (macro calculation, per-user scoping, shopping-list generation) changed with " +
        "no tests, or tests dumped into a generic catch-all project. " +
        "2-4: the diff adds non-trivial logic — branching, parsing, ordering, arithmetic, truncation, " +
        "retry — and adds no test that exercises it. This applies wherever the logic lives, including " +
        "build tooling and scripts under tools/; a pure function is not exempt for being 'just a helper', " +
        "and being new code rather than changed code is not a reason to score it higher. " +
        "10: tests sit beside the layer they cover (xUnit under tests/Jadlify.*.Tests, Vitest/RTL for the " +
        "SPA) and exercise the risk the diff actually introduces.",
    ),
  security: z
    .number()
    .describe(
      "Security: per-user data isolation and secret handling (scale 1-10). " +
        "1: user-owned data queried or mutated without scoping to the authenticated user's id, or a secret " +
        "committed to appsettings*.json / .env. " +
        "10: every user-owned query and command is scoped to the current user, no secrets in the diff, and " +
        "JWT validation is left intact or correctly extended.",
    ),
  verdict: z
    .enum(["pass", "fail"])
    .describe("Binding verdict for the whole change. 'fail' when any criterion scores 3 or below."),
  findings: z
    .array(
      z.object({
        severity: z.enum(["blocker", "major", "minor"]).describe("How badly this blocks the merge."),
        file: z.string().describe("Repository-relative path taken from the diff."),
        issue: z
          .string()
          .describe("One sentence: what is wrong and what to do about it. Name the symbol or line."),
      }),
    )
    .describe("Concrete findings, most severe first. At most 6. Empty array when the change is clean."),
  summary: z.string().describe("Summary in Markdown, 2-3 sentences, ready to post as a PR comment."),
});

export type Review = z.infer<typeof REVIEW_SCHEMA>;

/** Criteria keys in the order they should be rendered in the PR comment. */
export const CRITERIA = [
  "correctness",
  "architectureFit",
  "complexityScope",
  "testCoverage",
  "security",
] as const satisfies readonly (keyof Review)[];

export const CRITERIA_LABELS: Record<(typeof CRITERIA)[number], string> = {
  correctness: "Correctness",
  architectureFit: "Architecture fit",
  complexityScope: "Complexity & scope",
  testCoverage: "Test coverage",
  security: "Security",
};
