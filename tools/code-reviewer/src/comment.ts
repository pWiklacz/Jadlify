import { CRITERIA, CRITERIA_LABELS, type Review } from "./review-schema.js";

/**
 * Markdown rendering for the PR comment. Split out of review.ts so it can be
 * tested without importing the CLI entrypoint, which reads stdin on load.
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
