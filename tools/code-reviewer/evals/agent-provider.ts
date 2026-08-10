import { runReview } from "../src/agent.js";

/**
 * promptfoo custom provider that runs the real review agent.
 *
 * Evaluating a detached copy of the prompt does not work here: the agent gets
 * its response shape from Output.object, while a bare prompt does not, so the
 * models answer with Markdown fences or a reasoning preamble and every
 * assertion fails on formatting rather than on review quality. Calling the
 * agent means the matrix compares what CI actually runs.
 *
 * The model comes from each provider entry's `config.model`, which is what
 * makes one config a side-by-side comparison of several models.
 */

interface ProviderOptions {
  id?: string;
  label?: string;
  config?: { model?: string };
}

interface CallContext {
  vars?: Record<string, unknown>;
}

export default class ReviewAgentProvider {
  private readonly providerId: string;
  private readonly model: string;

  constructor(options: ProviderOptions = {}) {
    this.model = options.config?.model ?? "anthropic/claude-haiku-4.5";
    this.providerId = options.id ?? `review-agent:${this.model}`;
  }

  id(): string {
    return this.providerId;
  }

  async callApi(prompt: string, context?: CallContext) {
    // The diff arrives as a test var; fall back to the rendered prompt so the
    // provider still works with a plain prompt file.
    const diff = typeof context?.vars?.diff === "string" ? context.vars.diff : prompt;

    try {
      const result = await runReview({ diff, model: this.model });
      return {
        output: JSON.stringify(result.review),
        tokenUsage: {
          prompt: result.inputTokens,
          completion: result.outputTokens,
          total: (result.inputTokens ?? 0) + (result.outputTokens ?? 0),
        },
        cost: result.costUsd,
      };
    } catch (error: unknown) {
      // Surfacing this as an error rather than throwing keeps one broken model
      // from aborting the whole matrix.
      return { error: error instanceof Error ? error.message : String(error) };
    }
  }
}
