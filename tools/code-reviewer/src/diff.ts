/**
 * Diff preparation. Two jobs, both about cost: drop files whose content the
 * reviewer cannot say anything useful about, and cap what is left so a large
 * PR cannot turn one CI run into an unbounded bill.
 */

/**
 * Generated or vendored paths. Reviewing them burns tokens for no signal —
 * a lockfile diff is thousands of lines the model has no opinion worth having about.
 */
const IGNORED_PATHS = [
  /(^|\/)package-lock\.json$/,
  /(^|\/)pnpm-lock\.yaml$/,
  /(^|\/)yarn\.lock$/,
  /(^|\/)node_modules\//,
  /(^|\/)dist\//,
  /(^|\/)bin\//,
  /(^|\/)obj\//,
  /(^|\/)wwwroot\//,
  /(^|\/)playwright-report\//,
  /(^|\/)test-results\//,
  /\.snap$/,
  /\.min\.(js|css)$/,
  /\.(png|jpe?g|gif|webp|ico|svg|pdf|woff2?|ttf|eot)$/i,
];

export const DEFAULT_MAX_DIFF_CHARS = 100_000;

export interface PreparedDiff {
  text: string;
  /** Files kept after filtering, highest review priority first. */
  includedFiles: string[];
  /** Files dropped as generated/vendored — never worth review tokens. */
  skippedFiles: string[];
  /** Files dropped only because the size cap was reached. */
  omittedFiles: string[];
  /** True when the diff hit the character cap and was cut short. */
  truncated: boolean;
}

/**
 * Review priority, lowest number first. This exists because the size cap is
 * spent in order: on a large branch diff the planning documents under
 * context/changes/ are big enough to consume the entire budget before a single
 * source file is reached, and the review then grades the plan instead of the
 * code. Sorting puts source first so truncation drops prose, not logic.
 */
function reviewPriority(path: string): number {
  if (/^(src|tests)\//.test(path)) return 0;
  if (/^(\.github|supabase|\.scripts)\//.test(path)) return 1;
  if (/^(docs|context)\//.test(path) || /\.(md|mdx|txt)$/i.test(path)) return 3;
  return 2;
}

/**
 * Splits a unified diff into per-file chunks. Each chunk starts at a
 * `diff --git` header; anything before the first header (rare, but possible
 * with some git configurations) is discarded.
 */
function splitByFile(diff: string): { path: string; chunk: string }[] {
  const chunks: { path: string; chunk: string }[] = [];
  const lines = diff.split("\n");

  let currentPath: string | null = null;
  let current: string[] = [];

  const flush = () => {
    if (currentPath !== null) {
      chunks.push({ path: currentPath, chunk: current.join("\n") });
    }
  };

  for (const line of lines) {
    if (line.startsWith("diff --git ")) {
      flush();
      current = [line];
      // `diff --git a/path/to/file b/path/to/file` — take the b-side, which is
      // the post-change path (and the a-side for deletions, which is fine).
      const match = /^diff --git a\/(.+?) b\/(.+)$/.exec(line);
      currentPath = match?.[2] ?? match?.[1] ?? "(unknown)";
    } else if (currentPath !== null) {
      current.push(line);
    }
  }
  flush();

  return chunks;
}

export function prepareDiff(raw: string, maxChars = DEFAULT_MAX_DIFF_CHARS): PreparedDiff {
  const chunks = splitByFile(raw);

  // No `diff --git` headers at all: the caller piped something that is not a
  // git diff (or a pre-formatted excerpt). Pass it through, capped.
  if (chunks.length === 0) {
    const truncated = raw.length > maxChars;
    return {
      text: truncated ? raw.slice(0, maxChars) : raw,
      includedFiles: [],
      skippedFiles: [],
      omittedFiles: [],
      truncated,
    };
  }

  const includedFiles: string[] = [];
  const skippedFiles: string[] = [];
  const omittedFiles: string[] = [];
  const kept: string[] = [];
  let budget = maxChars;
  let truncated = false;

  const reviewable = chunks.filter(({ path }) => {
    if (IGNORED_PATHS.some((pattern) => pattern.test(path))) {
      skippedFiles.push(path);
      return false;
    }
    return true;
  });

  // Stable sort: same priority keeps the original diff order.
  const ordered = reviewable
    .map((entry, index) => ({ entry, index }))
    .sort(
      (a, b) =>
        reviewPriority(a.entry.path) - reviewPriority(b.entry.path) || a.index - b.index,
    )
    .map(({ entry }) => entry);

  for (const { path, chunk } of ordered) {
    if (budget <= 0) {
      truncated = true;
      omittedFiles.push(path);
      continue;
    }
    if (chunk.length > budget) {
      kept.push(`${chunk.slice(0, budget)}\n... [diff truncated at the size cap]`);
      includedFiles.push(path);
      budget = 0;
      truncated = true;
      continue;
    }
    kept.push(chunk);
    includedFiles.push(path);
    budget -= chunk.length;
  }

  return { text: kept.join("\n"), includedFiles, skippedFiles, omittedFiles, truncated };
}

export async function readStdin(): Promise<string> {
  const chunks: Buffer[] = [];
  for await (const chunk of process.stdin) chunks.push(chunk as Buffer);
  return Buffer.concat(chunks).toString("utf8");
}
