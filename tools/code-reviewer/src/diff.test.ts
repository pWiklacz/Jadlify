import { describe, expect, it } from "vitest";
import { prepareDiff } from "./diff.js";

/**
 * Builds a realistic single-file chunk. `padding` lets a test grow one file
 * past the size cap without hand-writing thousands of characters.
 */
function fileDiff(path: string, padding = 0): string {
  return [
    `diff --git a/${path} b/${path}`,
    "index 0000000..1111111 100644",
    `--- a/${path}`,
    `+++ b/${path}`,
    "@@ -0,0 +1 @@",
    `+${"x".repeat(padding || 10)}`,
  ].join("\n");
}

function joinDiffs(...chunks: string[]): string {
  return chunks.join("\n");
}

describe("prepareDiff — filtering", () => {
  it("drops generated and vendored files without spending review budget on them", () => {
    const raw = joinDiffs(
      fileDiff("src/Jadlify.API/Program.cs"),
      fileDiff("package-lock.json"),
      fileDiff("src/Jadlify.Web/dist/app.js"),
      fileDiff("assets/logo.png"),
    );

    const result = prepareDiff(raw);

    expect(result.includedFiles).toEqual(["src/Jadlify.API/Program.cs"]);
    expect(result.skippedFiles).toEqual([
      "package-lock.json",
      "src/Jadlify.Web/dist/app.js",
      "assets/logo.png",
    ]);
    // The point of skipping is that the content never reaches the model.
    expect(result.text).not.toContain("package-lock.json");
    expect(result.truncated).toBe(false);
  });

  it("passes through input that carries no diff headers, still capped", () => {
    const raw = "not a git diff, just prose";

    expect(prepareDiff(raw)).toMatchObject({
      text: raw,
      includedFiles: [],
      truncated: false,
    });

    const capped = prepareDiff(raw, 10);
    expect(capped.text).toBe(raw.slice(0, 10));
    expect(capped.truncated).toBe(true);
  });
});

describe("prepareDiff — priority ordering", () => {
  it("puts source ahead of config, config ahead of prose", () => {
    // Deliberately authored in reverse priority order.
    const raw = joinDiffs(
      fileDiff("context/changes/some-change/plan.md"),
      fileDiff("Directory.Build.props"),
      fileDiff(".github/workflows/ci.yml"),
      fileDiff("src/Jadlify.API/Program.cs"),
    );

    expect(prepareDiff(raw).includedFiles).toEqual([
      "src/Jadlify.API/Program.cs",
      ".github/workflows/ci.yml",
      "Directory.Build.props",
      "context/changes/some-change/plan.md",
    ]);
  });

  it("keeps the original diff order when priorities tie", () => {
    const raw = joinDiffs(fileDiff("src/b.ts"), fileDiff("src/a.ts"), fileDiff("tests/c.ts"));

    expect(prepareDiff(raw).includedFiles).toEqual(["src/b.ts", "src/a.ts", "tests/c.ts"]);
  });

  it("spends the budget on source and drops prose when the cap bites", () => {
    const prose = fileDiff("docs/long-design-note.md", 400);
    const source = fileDiff("src/Jadlify.API/Program.cs");
    const raw = joinDiffs(prose, source);

    // Enough room for the source file alone — prose would have eaten it all
    // had ordering not moved source to the front.
    const result = prepareDiff(raw, source.length);

    expect(result.includedFiles).toEqual(["src/Jadlify.API/Program.cs"]);
    expect(result.omittedFiles).toEqual(["docs/long-design-note.md"]);
    expect(result.truncated).toBe(true);
  });
});

describe("prepareDiff — size cap arithmetic", () => {
  it("truncates a single file that is larger than the whole budget", () => {
    const raw = fileDiff("src/Huge.cs", 5_000);
    const cap = 200;

    const result = prepareDiff(raw, cap);

    expect(result.includedFiles).toEqual(["src/Huge.cs"]);
    expect(result.omittedFiles).toEqual([]);
    expect(result.truncated).toBe(true);
    expect(result.text).toContain("... [diff truncated at the size cap]");
    // Everything before the marker is exactly the budget, never more.
    expect(result.text.split("\n... [diff truncated")[0]).toHaveLength(cap);
  });

  it("marks files that no longer fit as omitted rather than silently dropping them", () => {
    const first = fileDiff("src/a.ts");
    const second = fileDiff("src/b.ts");

    // Exactly enough for the first chunk, leaving zero budget for the second.
    const result = prepareDiff(joinDiffs(first, second), first.length);

    expect(result.includedFiles).toEqual(["src/a.ts"]);
    expect(result.omittedFiles).toEqual(["src/b.ts"]);
    expect(result.truncated).toBe(true);
    expect(result.text).not.toContain("src/b.ts");
  });

  it("reports nothing as truncated when everything fits", () => {
    const raw = joinDiffs(fileDiff("src/a.ts"), fileDiff("src/b.ts"));

    const result = prepareDiff(raw, raw.length + 1_000);

    expect(result.truncated).toBe(false);
    expect(result.omittedFiles).toEqual([]);
    expect(result.includedFiles).toHaveLength(2);
  });
});
