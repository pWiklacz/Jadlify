import { ToolLoopAgent, Output, isStepCount } from "ai";
import { createOpenRouter } from "@openrouter/ai-sdk-provider";
import { prepareDiff, DEFAULT_MAX_DIFF_CHARS, type PreparedDiff } from "./diff.js";
import { REVIEW_SCHEMA, SYSTEM_PROMPT, type Review } from "./review-schema.js";

/**
 * The reviewer itself, with no CLI or CI concerns attached. Both the command
 * line (src/review.ts) and the promptfoo provider (evals/agent-provider.ts)
 * call this, so the eval matrix measures the agent that actually runs on pull
 * requests rather than a prose imitation of it.
 */

/**
 * Overridable because picking the model is what the promptfoo matrix decides.
 *
 * Sonnet 5 rather than Haiku 4.5: Haiku returned straight 10/10 with no findings
 * on a diff that added ~1400 lines of untested logic, which is not a review. At
 * $2/M input it is twice Haiku's rate and still under Sonnet 4.6 — on a review
 * that costs cents either way, the accuracy is worth more than the difference.
 */
export const DEFAULT_MODEL = "anthropic/claude-sonnet-5";

export interface RunReviewOptions {
  diff: string;
  model?: string;
  prTitle?: string;
  prBody?: string;
  maxDiffChars?: number;
  apiKey?: string;
}

export interface RunReviewResult {
  review: Review;
  prepared: PreparedDiff;
  model: string;
  inputTokens?: number;
  outputTokens?: number;
  costUsd?: number;
}

function requireApiKey(explicit?: string): string {
  const key = explicit ?? process.env.OPENROUTER_API_KEY;
  if (!key) {
    throw new Error(
      "OPENROUTER_API_KEY is not set. Locally: put it in tools/code-reviewer/.env or export it. " +
        "In CI: add it as a repository secret and pass it through the workflow's env.",
    );
  }
  return key;
}

export function buildPrompt(diff: PreparedDiff, context: { title?: string; body?: string }): string {
  const parts: string[] = [];

  if (context.title) parts.push(`Pull request title: ${context.title}`);
  if (context.body?.trim()) parts.push(`Pull request description:\n${context.body.trim()}`);

  if (diff.skippedFiles.length > 0) {
    parts.push(
      `Note: ${diff.skippedFiles.length} generated or vendored file(s) were excluded from this diff ` +
        "and must not be commented on.",
    );
  }
  if (diff.omittedFiles.length > 0) {
    parts.push(
      `Note: this diff exceeded the size cap, so ${diff.omittedFiles.length} further changed file(s) ` +
        "are not shown. Judge only what you can see, say in the summary that the review is partial, " +
        "and do not treat a missing test file as absent — it may simply be outside the shown range.",
    );
  } else if (diff.truncated) {
    parts.push(
      "Note: the diff was truncated at a size cap. Judge only what you can see, and say in the " +
        "summary that the review is partial.",
    );
  }

  parts.push(`Review this diff:\n\n${diff.text}`);
  return parts.join("\n\n");
}

/**
 * OpenRouter reports the billed cost under providerMetadata when usage
 * accounting is enabled. Read it defensively — a provider-side shape change
 * should cost us the cost line, not the whole review.
 */
function extractCost(providerMetadata: unknown): number | undefined {
  const cost = (providerMetadata as { openrouter?: { usage?: { cost?: unknown } } } | undefined)
    ?.openrouter?.usage?.cost;
  return typeof cost === "number" ? cost : undefined;
}

export async function runReview(options: RunReviewOptions): Promise<RunReviewResult> {
  const model = options.model ?? DEFAULT_MODEL;
  const prepared = prepareDiff(options.diff, options.maxDiffChars ?? DEFAULT_MAX_DIFF_CHARS);

  const openrouter = createOpenRouter({ apiKey: requireApiKey(options.apiKey) });

  const reviewer = new ToolLoopAgent({
    model: openrouter(model),
    instructions: SYSTEM_PROMPT,
    tools: {},
    output: Output.object({ schema: REVIEW_SCHEMA }),
    // One step to read the diff and reason, one to emit the structured output.
    // The v6 examples call this `stepCountIs`; it is the same function.
    stopWhen: isStepCount(2),
    // Opt in to OpenRouter's usage accounting so the response carries the
    // actually billed cost rather than only token counts.
    providerOptions: { openrouter: { usage: { include: true } } },
  });

  const { output, totalUsage, providerMetadata } = await reviewer.generate({
    prompt: buildPrompt(prepared, { title: options.prTitle, body: options.prBody }),
  });

  return {
    review: output,
    prepared,
    model,
    inputTokens: totalUsage.inputTokens,
    outputTokens: totalUsage.outputTokens,
    costUsd: extractCost(providerMetadata),
  };
}
