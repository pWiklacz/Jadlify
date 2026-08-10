import { describe, expect, it } from "vitest";
import { renderComment } from "./comment.js";
import type { Review } from "./review-schema.js";

function review(overrides: Partial<Review> = {}): Review {
  return {
    correctness: 8,
    architectureFit: 8,
    complexityScope: 8,
    testCoverage: 8,
    security: 8,
    verdict: "pass",
    findings: [],
    summary: "Looks fine.",
    ...overrides,
  };
}

describe("renderComment", () => {
  it("renders a passing review with the pass badge and no findings", () => {
    const body = renderComment(review(), { model: "anthropic/claude-sonnet-5" });

    expect(body).toContain("## 🤖 AI Code Review — ✅ **PASS**");
    expect(body).toContain("_No findings._");
    expect(body).toContain("<sub>Model: `anthropic/claude-sonnet-5`</sub>");
  });

  it("renders a failing review with the fail badge and its findings", () => {
    const body = renderComment(
      review({
        verdict: "fail",
        testCoverage: 2,
        findings: [
          { severity: "major", file: "src/diff.ts", issue: "prepareDiff has no tests." },
          { severity: "minor", file: "src/review.ts", issue: "renderComment has no tests." },
        ],
      }),
      { model: "anthropic/claude-sonnet-5" },
    );

    expect(body).toContain("❌ **FAIL**");
    expect(body).toContain("- **major** `src/diff.ts` — prepareDiff has no tests.");
    expect(body).toContain("- **minor** `src/review.ts` — renderComment has no tests.");
    expect(body).not.toContain("_No findings._");
  });

  it("colours each score bar by band", () => {
    const body = renderComment(
      review({ correctness: 3, architectureFit: 4, complexityScope: 6, testCoverage: 7 }),
      { model: "m" },
    );

    expect(body).toContain("| Correctness | 🔴 3/10 |");
    expect(body).toContain("| Architecture fit | 🟡 4/10 |");
    expect(body).toContain("| Complexity & scope | 🟡 6/10 |");
    expect(body).toContain("| Test coverage | 🟢 7/10 |");
  });

  it("renders every criterion in the declared order", () => {
    const body = renderComment(review(), { model: "m" });
    const rows = body
      .split("\n")
      .filter((line) => line.startsWith("| ") && line.includes("/10 |"))
      .map((line) => line.split(" | ")[0]?.replace("| ", ""));

    expect(rows).toEqual([
      "Correctness",
      "Architecture fit",
      "Complexity & scope",
      "Test coverage",
      "Security",
    ]);
  });

  it("appends the billed cost only when the provider reported one", () => {
    expect(renderComment(review(), { model: "m", costUsd: 0.1508 })).toContain(
      "<sub>Model: `m` · $0.1508</sub>",
    );
    expect(renderComment(review(), { model: "m" })).toContain("<sub>Model: `m`</sub>");
  });
});
