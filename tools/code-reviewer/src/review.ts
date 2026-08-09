import { writeFile } from "node:fs/promises";
import { readStdin, DEFAULT_MAX_DIFF_CHARS } from "./diff.js";
import { runReview, DEFAULT_MODEL } from "./agent.js";
import { CRITERIA, CRITERIA_LABELS, type Review } from "./review-schema.js";

/**
 * Reads a git diff on stdin and prints a structured review as JSON on stdout.
 * Diagnostics go to stderr so `... | npm run --silent review > review.json`
 * stays valid JSON.
 *
 *   git diff main...HEAD | npm run --silent review
 *
 * Environment:
 *   OPENROUTER_API_KEY     required
 *   REVIEW_MODEL           OpenRouter model id (default: see agent.ts)
 *   REVIEW_MAX_DIFF_CHARS  size cap for the diff sent to the model
 *   REVIEW_COMMENT_PATH    when set, also writes the rendered Markdown comment there
 *   REVIEW_FAIL_ON_VERDICT when "true", exits 1 on a 'fail' verdict (merge gate)
 */

/** Renders the review as the Markdown body of a PR comment. */
export function renderComment(review: Review, meta: { model: string; costUsd?: number }): string {
  const badge = review.verdict === "pass" ? "✅ **PASS**" : "❌ **FAIL**";

  const scores = CRITERIA.map((key) => {
    const score = review[key] as number;
    const bar = score <= 3 ? "🔴" : score <= 6 ? "🟡" : "🟢";
    return `| ${CRITERIA_LABELS[key]} | ${bar} ${score}/10 |`;
  }).join("\n");

  const findings =
    review.findings.length === 0
      ? "_No findings._"
      : review.findings.map((f) => `- **${f.severity}** \`${f.file}\` — ${f.issue}`).join("\n");

  const cost = meta.costUsd !== undefined ? ` · $${meta.costUsd.toFixed(4)}` : "";

  return [
    `## 🤖 AI Code Review — ${badge}`,
    "",
    review.summary,
    "",
    "| Criterion | Score |",
    "| --- | --- |",
    scores,
    "",
    "### Findings",
    findings,
    "",
    `<sub>Model: \`${meta.model}\`${cost}</sub>`,
  ].join("\n");
}

async function main(): Promise<void> {
  const raw = await readStdin();
  if (raw.trim().length === 0) {
    throw new Error(
      "Empty diff on stdin. In CI this almost always means the checkout was shallow — " +
        "actions/checkout needs `fetch-depth: 0` for `git diff origin/<base>...HEAD` to resolve.",
    );
  }

  const parsedCap = Number(process.env.REVIEW_MAX_DIFF_CHARS ?? DEFAULT_MAX_DIFF_CHARS);

  const result = await runReview({
    diff: raw,
    model: process.env.REVIEW_MODEL ?? DEFAULT_MODEL,
    prTitle: process.env.PR_TITLE,
    prBody: process.env.PR_BODY,
    maxDiffChars: Number.isFinite(parsedCap) ? parsedCap : DEFAULT_MAX_DIFF_CHARS,
  });

  const { review, prepared, model, costUsd } = result;

  console.error(
    `[review] model=${model} files=${prepared.includedFiles.length} ` +
      `generated=${prepared.skippedFiles.length} over-cap=${prepared.omittedFiles.length} ` +
      `chars=${prepared.text.length}${prepared.truncated ? " (truncated)" : ""}`,
  );
  console.error(
    `[review] verdict=${review.verdict} tokens=${result.inputTokens ?? "?"} in / ` +
      `${result.outputTokens ?? "?"} out` +
      (costUsd !== undefined ? ` cost=$${costUsd.toFixed(4)}` : ""),
  );

  const commentPath = process.env.REVIEW_COMMENT_PATH;
  if (commentPath) {
    await writeFile(commentPath, renderComment(review, { model, costUsd }), "utf8");
    console.error(`[review] comment written to ${commentPath}`);
  }

  console.log(JSON.stringify(review, null, 2));

  if (process.env.REVIEW_FAIL_ON_VERDICT === "true" && review.verdict === "fail") {
    console.error("[review] REVIEW_FAIL_ON_VERDICT=true and the verdict is 'fail' — exiting 1.");
    process.exit(1);
  }
}

main().catch((error: unknown) => {
  console.error(`[review] ${error instanceof Error ? error.message : String(error)}`);
  process.exit(1);
});
