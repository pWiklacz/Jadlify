---
title: "E2E tests: Playwright, MCP, and multimodal scenarios"
course: "10xdevs-3"
language: "en"
source: "Przeprogramowani.pl"
exported: "2026-06-10"
format: "markdown"
---

![cover](https://images.przeprogramowani.pl/images/2026/06/KAYdaqWeFfu3nsEP-omYeA.jpeg)

The hook triggered the linter, the typecheck passed, related tests turned green. Full automation from editing to commit.

Except that the user has just generated flashcards, sees them on the screen, refreshes the page — and the deck is empty. The data did not persist because somewhere along the route auth → API → database something got lost.

No hook saw this, because hooks operate on source code. They don’t start the server, query the database, or check if the data will survive the full user path.

To catch problems that cross multiple system boundaries, you need tests that run the entire system end-to-end, along with an agent with access to a browser.

But the tool alone is not enough. The agent can generate an E2E test that passes. But does this test really protect the system from risk? Will it survive tomorrow's UI refactor?

Besides tools, you need two control mechanisms: a test seed that shows the agent a pattern of a good E2E test, and rules that limit what the agent produces.

## How the agent sees the application

For the agent to be able to test the application end-to-end, it must interact with it like a user would, through a browser. When you open a page, you see pixels: colors, fonts, buttons, the layout of elements. By default, the agent does not look at pixels; it looks at the accessibility tree.

The accessibility tree is a structured map of the entire page. Each element has its role (button, textbox, heading), name (what the screen reader would read aloud), and state (disabled, checked, expanded). The browser automatically builds this tree from your HTML and ARIA.

For the agent, this map is better than a screenshot. It is deterministic, compact, and does not require a vision model. The agent receives a snapshot in YAML format with references to elements, for example `e5`, `e15`, `e21`. Then it issues commands: "click `e15`", "enter text in `e21`". It receives a new snapshot and continues.

![Diagram](https://images.przeprogramowani.pl/images/2026/06/PwYdat_jLeSznsEPgq6auA4.jpeg)

In this loop, the agent does not guess CSS selectors and does not try to recognize buttons on the screenshot. It navigates the semantic structure of the page, just like a screen reader.

This directly affects the tests that the agent generates. Since it sees roles and element names, it should naturally produce tests based on `getByRole`, not on CSS selectors. We will return to this with the seed test.

Since you already know what the agent sees, it's time to connect it to the browser.

### Playwright CLI as an agent interface

Playwright CLI (`@playwright/cli`) is a tool optimized for programming agents. Instead of loading over 30 MCP tools into the agent context, the CLI provides shell commands and saves snapshots to disk as YAML files. The agent reads only what it needs.

Installation and first run:

```bash
npm install -g @playwright/cli@latest
```

![Obraz](/content/lessons10xDevs3/pl/assets/playwright-cli-open.png)

After opening the page, the CLI displays a snapshot with references to elements. The interaction looks like this:

```bash
playwright-cli click e15
```

Each command returns the path to the updated snapshot. The agent decides on its own whether to read it or to proceed.

Here you can clearly see the difference between the CLI and MCP in token usage: MCP can use up to 4x more tokens than the Playwright CLI in the same test scenario.

In the prework, the token budget was an abstract limitation. Here you see how it translates into a specific engineering decision, and that is exactly why we use Playwright CLI by default instead of MCP.

The agent receives a dedicated browser session and can explore your application as part of the work.

### Session without login

The agent explores your application, but most of the interesting paths are behind the login. Without this, every scenario starts from the login form. This wastes tokens, adds fragile selectors, and makes the test dependent on the login UI rather than what we actually want to verify in the given test. Of course, it is worth having two dedicated scenarios checking the login and registration path, but they should not be a dependency for the other tests.

Most E2E frameworks allow you to save the session state (cookies, tokens) and inject it into subsequent runs. Playwright solves this through [storageState](https://playwright.dev/docs/auth). You log in once, save the session state to a JSON file, and inject it into each subsequent session. If you use a different framework, check the current documentation via Context7\. Injecting saved session state is a standard pattern.

```bash
# Log in once via CLI
```

In the Playwright Test Runner configuration, the same file looks like this:

```typescript
// playwright.config.ts
```

Each test starts immediately in a logged-in state. The `auth.json` file contains sensitive data, so add it to `.gitignore`.

## From Risks to E2E Tests

You have an agent that can run an application and interact with it. Now the question is: what should it test?

You have `context/foundation/test-plan.md` from previous lessons. It contains business risks, organized by impact and likelihood. Some of them require E2E tests because they concern user flows rather than isolated functions.

How do you know which risks are E2E risks? A short heuristic: if the risk crosses multiple system boundaries (auth, routing, API, database) or exists only in the rendered UI, it requires E2E. If it can be checked on an isolated function, a unit test from the lesson about them (M3L2) is sufficient.

E2E does not mean "zero mocking". Internal boundaries (auth, routing, database) should be real, because integration risks lie within them. But expensive or nondeterministic external APIs (LLM models, payment gateways) are worth mocking at the network level. In 10xCards, the E2E flashcard generation test mocks OpenRouter on the HTTP layer, keeping real auth, API, and database. This is a pragmatic pattern, not an exception.

Key principle: **you do not generate E2E tests from scratch**. You open `context/foundation/test-plan.md`, select the top 1-3 risks that require coverage at the browser level, and provide them to the agent as input. If your application has no frontend, E2E can mean API scenarios: request → routing → logic → database, without a browser. Quality control mechanisms (seed test, rules, review) work the same way. Only the interaction layer changes.

Remember to be conservative with the number of E2E tests: they are the slowest and most fragile among all types of tests. At the same time, they provide the best feedback on the stability of the entire system along specific user paths.

### `/10x-e2e`: connects with `/10x-implement` and `/10x-tdd`

You have risks selected from `test-plan.md` and an agent that can run the application. The coverage of these risks at the browser level is handled by the `/10x-e2e` skill. This is not a separate test generator alongside the process for unit tests. It is a **skill aware of `/10x-implement` and `/10x-tdd`** from the lesson about unit tests (M3L2). It reads the same `context/changes/<change-id>/plan.md`, uses the same `## Progress` section, follows the same phases, and finishes them with the same commit ritual. It only changes the internal loop: instead of writing production code (`/10x-implement`) or first a failing unit test (`/10x-tdd`), it generates and solidifies a browser-level test against a running application.

A single-phase loop looks like this:

```text
PLAN     → select risk, map the path (Playwright planner or prompt template)
```

Download the skill as an artifact package for this lesson:

```bash
npx @przeprogramowani/10x-cli@latest get m3l4
```

An example implementation of an algorithm that solves the Tower of Hanoi:

```ts
import type { TaskSolveFn } from "@przeprogramowani/10x-core";
export const taskSolve: TaskSolveFn = (task, ctx) => {
  const { n, a, b, c } = task.input;
  const answer: [string, string][] = [];

  const hanoi = (n: number, a: string, b: string, c: string) => {
    if (n === 0) {
      return;
    }
    hanoi(n - 1, a, c, b);
    answer.push([a, c]);
    hanoi(n - 1, b, a, c);
  };

  hanoi(n, a, b, c);

  return {
    answer,
  };
};

```

How the algorithm works:

- The `n` parameter specifies the number of disks to move.
- The `a`, `b`, `c` parameters represent the names of the three pegs.
- The recursive `hanoi` function moves `n - 1` disks to the auxiliary peg, then moves the largest disk to the target peg, and finally moves the `n - 1` disks from the auxiliary peg to the target peg.
- The `answer` result is an array of peg pairs that indicate the successive moves.

This package installs the `/10x-e2e` skill, which is a single source for the entire E2E workflow (quality rules, five anti-patterns, test seed pattern, and prompt template in its references), as well as a thin indicator `CLAUDE-m3l4` appended to your `CLAUDE.md`: a few hard rules and a reference to the skill that the agent reads automatically before generating tests.

If you want not only to run it but also to understand it: before launching `/10x-e2e`, direct the skill-explainer prompt from the lesson From Chatbot to Agent: tech stack, skills, and metaprompting (M1L2) at it. It will break down the skill into mechanics and design decisions so that you know exactly what drives your E2E flow. The same prompt works for every new skill in the course.

Not every phase of the plan deserves an E2E test. Before `/10x-e2e` generates anything, it checks three things for a given phase: whether the risk is really at the browser level (it crosses many system boundaries, i.e., auth, routing, API, database, or it exists only in the rendered UI), whether the tested functionality is already built and the application runs, and whether there isn't already a green E2E test for this risk.

If the risk can be proven by an isolated function, the skill redirects the phase to `/10x-tdd` or `/10x-implement`. E2E is the slowest and most fragile layer, so you only reach for it where a cheaper test would lie. If the functionality does not yet exist, the skill stops: the browser has nothing to drive, so first you build the feature through `/10x-implement`, and after E2E you return.

Thanks to the shared `## Progress` section, you mix modes within a single plan, exactly like `/10x-tdd` and `/10x-implement` in the lesson on unit testing:

```text
/10x-implement <change-id> phase 1   # build functionality
```

You do not create a second task list and do not split the change into parallel processes. All three skills record progress in the same `## Progress`, so you simply choose the appropriate mode for the specific phase.

In the PLAN step, you have two paths to the same contract. The first is when you want the agent to explore the application on its own, which is the planner→generator flow, introduced by Playwright in version 1.56 as three specialized test agents:

- **Planner** — explores the application and generates a test plan in Markdown. It describes scenarios, steps, and expected outcomes.
- **Generator** — transforms the plan into executable TypeScript code, validating selectors on the live application.
- **Healer** — analyzes the UI state of failing tests and suggests selector fixes.

You do not assemble this flow manually. `/10x-e2e` handles it for you and knows that to explore the application it can use the Playwright CLI from the previous section. On your side, only one thing remains: the seed test (`seed.spec.ts`), which shows the agent what a correct test looks like in your project.

![Diagram](https://images.przeprogramowani.pl/images/2026/06/WQYdaq_GIM-1xN8P14uT6QY.jpeg)

A concrete example from 10xCards: in `test-plan.md` the highest E2E risk is "loss of generated flashcards after refreshing the page." This scenario goes through OpenRouter (mocked at the HTTP layer), the save API, the database, and server-side rendering, so no unit test will cover it. The second risk, "unauthenticated user sees protected resources," on the other hand, is a full path through the authorization gateway: middleware, cookie passing, and redirection. You provide these risks to the skill, and it handles the rest: the Planner explores the application through snapshots, generates a plan, and the Generator turns it into a test. Sounds great, but that's only halfway there.

### Seed test and quality rules

In the GENERATE step `/10x-e2e` transforms the mapped path into a test, but not out of thin air. It is driven by two quality levers: the test seed and E2E rules. In the unit testing lesson (M3L2) you learned to provide the agent with specific, risk-related input and enforce behavioral assertions. In E2E these two levers serve exactly that role.

The seed test (`seed.spec.ts`) is not an empty ritual. Playwright documentation states clearly: "Planner will also use this seed test as an example of all the generated tests." If the seed uses `getByRole`, the Generator will too. If the seed has `page.waitForTimeout(2000)`, the Generator will replicate this antipattern in every generated test. What you show is what you get.

A good seed test demonstrates four patterns to the agent:

**Role-based selectors.** `getByRole('button', { name: 'Add flashcard' })` is resistant to changes in CSS classes, DOM structure, and component refactoring. Playwright documentation recommends: "Prefer user-facing attributes to XPath or CSS selectors." These are exactly the pieces of information that the agent sees in accessibility snapshots. And a seed that uses `page.locator('.btn-primary')`? It teaches the agent to replicate fragile selectors.

**Test independence.** The agent likes to generate tests where test B assumes that test A has created a deck. Problem? Playwright runs tests in parallel, in a random order. The documentation is clear here: "Each test should be completely isolated from another test and should run independently." The seed must demonstrate this: a full cycle of setup, action, assertion, cleanup in one test.

**Waiting for state, not for time.** "Never wait for timeout in production. Tests that wait for time are inherently flaky." Seed should use `expect(locator).toBeVisible()`, `page.waitForURL()`, or `page.waitForResponse()`. A specific application state instead of arbitrary time.

**Assertions related to risk.** The test name should clearly link it to the risk from `context/foundation/test-plan.md`: `test('flashcard data persists after page reload', ...)` instead of `test('test 1', ...)`.

```typescript
// seed.spec.ts
```

Notice the `Date.now()` in the deck name. These are unique identifiers, which we will discuss more shortly.

Besides the test seed, the agent needs testing rules: locator hierarchy (`getByRole` before CSS/XPath), prohibition of `page.waitForTimeout()`, test independence, assertions on business outcomes, cleanup, and `storageState` for authentication. The full set of E2E generation rules and five anti-patterns are loaded into the `/10x-e2e` skill via references to the files: `e2e-quality-rules.md` and `e2e-anti-patterns.md`.

If you are using a framework other than Playwright, adapt the rules to its idioms. The skill records mappings for Cypress, WebdriverIO, and Selenium. The rules from the Playwright documentation and my experience are a starting point, not dogma.

Seed test and rules are the two strongest levers of quality in E2E. Without them? The agent produces tests that pass today but break at the first refactor or block the parallel pipeline execution.

### Prompt template for E2E

The planner→generator flow is one way in the PLAN step. The second, simpler way is to generate a single test directly from the prompt, useful when you want one test without initializing Playwright agents. You do not fill this template manually: `/10x-e2e` keeps it referenced in `e2e-prompt-template.md` and fills it in for you with four fields: risk from `test-plan.md`, research anchor, business scenario (a single observable behavior that becomes an assertion), and real versus mocked boundaries. The template file itself remains unchanged, and the skill creates a new file with a prompt for a specific risk.

The contract is the same regardless of the path. You do not repeat in the prompt what is already in the seed test and rules: the seed shapes what the Generator produces, and the rules automatically constrain the agent. The prompt adds only what the seed and rules do not know: specific risk, path, and boundaries.

Compare with the unit test from the lesson on unit tests (M3L2). There you pointed to a specific function and the risk it is meant to protect against. Here you provide the entire user path and explicitly separate the boundaries between real and mocked. In E2E the system boundaries _are_ the test. But the "business scenario" serves the same role as there: it enforces an assertion related to risk, not to implementation. Regardless of whether you go through a prompt or through the pipeline planner→generator, the same contract applies: risk, research anchor, business scenario, boundaries, risk-related assertion.

### Five E2E Anti-Patterns from the Agent

The REVIEW step of the loop is exactly that checklist. You have rules and a seed test, but what if the agent still produces a brittle test? In the unit tests lesson (M3L2), you learned to recognize three anti-patterns: mirror of the implementation, happy-path-only, and missing edge cases. E2E tests have their own set of problems. `/10x-e2e` runs every generated test through this five-item checklist (the full version is in its reference `e2e-anti-patterns.md`):

**1\. Naive assertion.** The agent generates a test that:

1. Creates a deck
2. Adds a flashcard
3. Refreshes the page
4. Checks... that the page title contains "Dashboard"

The test passes. However, it does not check whether the flashcard survived the refresh. The assertion is naive: syntactically correct, but it actually gives us a false sense of security.

Control question: **will this assertion fail if the risk from `context/foundation/test-plan.md` materializes?** If the answer is no, the assertion is naive. The corrected version specifically checks whether the flashcard still exists in the deck after refreshing.

**2\. Fragile selector.** The agent generates `page.locator('div.card-container > div:nth-child(3) > button')` instead of `page.getByRole('button', { name: 'Delete' })`. The first selector breaks with every layout change. If your test seed and rules enforce `getByRole`, the Generator will repeat that pattern. If not, you will get a test that sooner or later will need to be updated, even if the user path remains the same.

**3\. Shared state between tests.** The agent generates a suite in which the "edit flashcard" test assumes that the "add flashcard" test has already been executed. Tests with shared state pass once, but fail randomly in subsequent runs. A classic flaky test.

**4\. `waitForTimeout` instead of waiting for a state.** The agent does not know how much time your backend needs. Instead of waiting for a specific response, it inserts `await page.waitForTimeout(3000)`. The test passes on your laptop but fails in CI, where the server responds more slowly. Fix: `await page.waitForResponse('**/api/decks')` or `await expect(element).toBeVisible()`.

**5\. No cleanup.** The agent creates test data (deck, flashcards) but does not clean up after itself. Everything works during the first run. On the second, there's a `unique constraint violation` because the deck "TypeScript Basics" already exists in the database.

Each of these patterns has the same source: the agent optimizes for "the test passes now," not for "the test will be stable tomorrow." This is not a flaw of a specific tool. It is a fundamental feature of code generation by LLMs, which you work with throughout the course.

### Re-prompting: how to improve the E2E test

The same discipline you applied in unit tests (M3L2) applies in E2E: do not say “fix this test.” Point out the anti-pattern by name (from the `e2e-anti-patterns.md` reference in the `/10x-e2e` skill), explain why the test does not protect against risk (or why it produces false failures), and provide the target pattern.

One example, for a naive assertion:

```text
The final assertion checks the page title instead of verifying that
```

Ready re-prompts for brittle selectors and `waitForTimeout` are in the anti-patterns reference, so you don't have to rewrite them from memory; just name the problem. Each re-prompt has the same three elements: what is wrong, why it doesn't mitigate risk (or causes false failures), and which pattern replaces it. It's the same rhythm as in the unit testing lesson (M3L2), transferred to the E2E layer.

### VERIFY: green is not enough

The last step of the loop is VERIFY, and it doesn't end with a green result because the test with the naive assertion is also green. The control question from the first anti-pattern ("will this assertion fail if the risk materializes?") must be checked, not assumed. That is why `/10x-e2e`, after running the test and seeing green, deliberately introduces a break: it reverses or weakens in the production code exactly the behavior related to the risk and checks whether the test fails red. If despite breaking the protected behavior the test remains green, the assertion is not guarding anything and returns to GENERATE.

This is the same discipline as verifying assertions by intentionally breaking them from the unit testing lesson (M3L2), applied to the E2E layer, with one difference: the skill immediately reverts the breakage after checking and never commits it. The red of intentional breaking is a checkpoint, not a commit.

The fifth anti-pattern (lack of cleanup) deserves a separate discussion because it is the source of the most common false failures in E2E.

The agent generates tests that create data in the application: deck, flashcards, settings. Without an isolation strategy, the next run will encounter conflicts: duplicates, stale state, full limits.

Two approaches that work:

**Unique identifiers.** Each test generates a unique prefix: `test-${Date.now()}-deck`. Tests do not conflict with each other because they operate on different data. It works natively with parallel execution and is the easiest for the agent to generate.

**Cleanup per test.** Each test cleans up after itself in the cleanup section or `test.afterEach`. Simple but fragile: if the test fails before cleanup, the data remains. Therefore, it is worth adding `test.afterAll` as a safety net.

The best practice is to combine both: unique identifiers prevent collisions, per-test cleanup prevents accumulation.

If your application uses Supabase, remember about Row-Level Security: the client teardown must be logged into the same account that created the data, or use the service role key. More about teardown as a Playwright function in Deep Dive.

Therefore, test state isolation should be included in your E2E rules. Written once, it increases the chance that the model will consider it from the first iteration, rather than waiting for you to catch it during review.

## MCP and vision mode

So far we have been working with the CLI because it is economical with tokens and sufficient for most scenarios. But is MCP even necessary?

Yes, but in different situations. MCP provides access to over 30 tools: network mocking (`browser_route`), cookie and localStorage management, trace recording, tab control. The CLI performs the same operations as shell commands, but MCP maintains the full session context in the agent's memory.

| CLI                     | MCP                                  |                              |
| ----------------------- | ------------------------------------ | ---------------------------- |
| **Tokens per scenario** | \~27K                                | \~114K                       |
| **Interaction model**   | Shell commands, disk snapshots       | MCP tools in context         |
| **Best use case**       | Coding agent with a large repository | Dedicated browser automation |
| **Default mode**        | Headless                             | Headed                       |

Decision heuristic: if your agent simultaneously edits code, runs tests, and navigates files, the CLI is a better choice because it saves context. If you have a dedicated session whose sole task is browser exploration (e.g., a long test scenario, scraping, monitoring), MCP offers a richer set of tools.

MCP configures modes via the `--caps` flag:

```bash
npx @playwright/mcp@latest --caps=vision,network,storage
```

By default, MCP operates in snapshot mode (accessibility tree). The `vision` flag enables the vision mode.

### Vision mode: when the DOM is not enough

A common frontend bug is overlapping components, which only becomes apparent at a specific browser window resolution. The accessibility tree describes the structure and semantics but not the rendered user interface itself. Overlapping elements, incorrect z-index, truncated text, broken animations: these issues exist in the browser window, not in the ARIA tree.

Playwright MCP has a vision mode (`--caps=vision`), in which the agent takes a screenshot and operates on coordinates instead of element references. This provides two types of verification:

**Interaction by coordinates.** The agent clicks on the point (x, y) on the screenshot instead of on the `e15` element. Useful for canvas elements, custom widgets, and components not visible in the accessibility tree.

**Visual verification by the model.** The agent takes a screenshot, sends it to the visual model, and asks the question: "Do the flashcard cards overlap? Is the button visible?" The model responds with a structured JSON with the evaluation.

![Diagram](https://images.przeprogramowani.pl/images/2026/06/dwYdasOAE6H0nsEP4bWX2QI.jpeg)

This is the moment when multimodality really does the job: vision verifies what the accessibility tree does not express, that is, whether the rendered layout actually looks correct, and not just whether the elements exist in the DOM.

But vision is not the default testing mode. It costs time and money, and vision models can be unreliable: they can shift coordinates towards the center of the screen or report a problem that does not exist. Therefore, we treat them as a supplement, not as the first choice.

Practical rule:

- **DOM (snapshot)** — by default for functional verification: whether the element exists, whether the form works, whether the data was saved.
- **Vision** — as a supplement for: layout regression, visual states (color, animation, z-index), elements invisible in the accessibility tree.
- **Deterministic tools** (Playwright `toMatchSnapshot`, Argos, Lost Pixel) — for pixel-level visual regression, because that is their specialty and they do it cheaper and faster than a vision model.

In testing, vision answers a narrow, verifiable question: is this specific element in place and does it look as it should. The broader, open question "does this layout look correct?", along with the entire machinery of choosing a vision model, its cost, and categories (frontier, budget, open-weight), belongs to debugging, not testing. We return to it in the lesson Debugging with AI: from stack trace to finished fix (M3L5), where vision is a diagnostic signal when searching for the cause of a bug. In E2E tests, you will usually rely on deterministic tools (`toMatchSnapshot`) anyway, because they provide a stable, repeatable result.

## E2E in the quality pipeline

The question remains, when to run them. In this module, we built a layered quality system:

![Diagram](https://images.przeprogramowani.pl/images/2026/06/kAYdaoTUNNXknsEPsrLR0QQ.jpeg)

E2E tests have a different rhythm than hooks. Hooks run on every edit, in milliseconds. You run E2E in CI because one full run takes minutes, not milliseconds.

You already have a complete CI/CD pipeline from previous modules, so now you just need to add an E2E test run locally through `/10x-e2e`, and in CI link it with the existing pipeline.

Over time these tests will fail and a third agent from Playwright Test Agents appears, which we have only mentioned so far: the healer. This is a local tool. When an E2E test fails because a selector has changed (e.g., after a component refactor), the healer can fix it automatically. It reviews the snapshot, identifies the new selector, and suggests a fix.

What does the healer struggle with? With changes in business logic. When a test fails because the backend changed the API response format, the healer adapts the assertion to the new (incorrect) behavior. Instead of catching the bug... it masks it.

This is the boundary between automatic repair and debugging. When the problem is in the selector, the healer helps. When the problem is in the logic, the healer harms. And it is exactly those more difficult cases, when the E2E test fails for an unclear reason and you have to get from the stack trace to the fix, that is the topic of the next lesson: Debugging with AI (M3L5).

## 🧑🏻‍💻 Practical tasks

### Seed test and testing rules

Before the agent starts generating E2E tests:

1. Write `seed.spec.ts`, which is a sample test demonstrating your E2E test writing conventions: `getByRole` as the default selector, waiting for state instead of time, unique identifiers in test data, cleanup, test name linked to the risk from `context/foundation/test-plan.md`.
2. Download the skill `/10x-e2e` (`npx @przeprogramowani/10x-cli@latest get m3l4`). It brings in E2E rules and five anti-patterns as a single source that the agent reads automatically.
3. Run the agent with the command to generate a single test and check if the result respects the seed and the rules.

### Application Exploration via CLI

Install Playwright CLI and open your application:

```bash
npm install -g @playwright/cli@latest
```

Navigate the application using CLI commands. Pay attention to the snapshot with references to elements. Try clicking several elements by reference, filling out the form, and navigating between pages.

### Configuration of storageState

Log in to the application through the CLI, save the session state, and verify that a new CLI session starts in a logged-in state:

```bash
playwright-cli open http://localhost:3000/login --headed
```

Add `playwright/.auth/` to `.gitignore`. Configure `storageState` in `playwright.config.ts`.

### E2E Scenarios from the Risk Map

1. Open your `context/foundation/test-plan.md` and select the 2 highest risks requiring E2E coverage.
2. Run `/10x-e2e` in the plan phase with this risk (`/10x-e2e <change-id> phase N`) or point it directly to the risk. The skill will go through the PLAN→GENERATE→REVIEW→VERIFY loop based on your test seed and rules. In the PLAN step, it has two paths to the same contract: a prompt template (when you want one test without initiating Playwright agents) or planner→generator (when you want the agent to explore the application itself). You provide the risk and review the result.
3. **Review:** review each test for five anti-patterns. For each assertion, ask: "Will this assertion fail if the risk from `context/foundation/test-plan.md` materializes?" Check selectors, test independence, waits, and cleanup. If you find an anti-pattern, use a re-prompt: specify the exact flaw by name and the expected pattern.
4. **Deliberate breaking:** reverse or weaken in production code the behavior related to the risk and confirm that the test fails. If it stays green, the assertion protects nothing, so go back to GENERATE. Finally, revert the breakage and do not commit it.

### Test Data Isolation

Make sure that your E2E tests can run multiple times without conflicts:

1. Check if each test uses unique identifiers (e.g. `Date.now()` in the deck name).
2. Check if the tests clean up after themselves (cleanup in the test or `afterEach`).
3. Run the suite twice in a row and verify that all tests pass each time.

### (Optional) Customize `/10x-e2e` to your context

You get the Skill tailored for Playwright — treat it as a starting point and adapt its references to your reality:

- **Another stack.** For Cypress, WebdriverIO, or Selenium, rewrite the rules and test seed using the idioms of your tool: its equivalent of `getByRole`, waiting for state, data isolation. The principles transfer, the syntax changes.
- **Team conventions.** Enter your locator rules, test naming, and paths in the repo so that the generated tests look like the rest of the suite.
- **No UI.** For APIs without an interface, map risks from `test-plan.md` to HTTP scenarios, and anti-patterns to the request and data layer.

Run the customized skill on one real risk and confirm that the result respects your rules. How `/10x-e2e` works internally and when such a skill is worthwhile is explained in the Deep Dive “E2E Workflow as a skill”.

## Claim your badge

After completing this lesson, claim your badge in the [10xDevs Mission Log](https://platforma.przeprogramowani.pl/10xdevs-3/mission-log) section and then show off your achievement!

## 🔎 Deep Dive

This section contains additional in-depth knowledge on selected topics related to the lesson. In this Deep Dive you will find:

- **browser.bind() and shared sessions** — how a single browser instance handles CLI, MCP, and test runner simultaneously
- **Stagehand as an alternative** — another approach to browser automation with AI
- **Composable fixtures vs Page Object Model** — why the POM pattern from the second edition is becoming obsolete
- **Teardown as a Playwright project** — full data cleanup configuration including Supabase RLS
- **E2E Workflow as a skill** — when to package this flow into a skill, how it relates to rules and test seed, and why a stack other than Playwright benefits the most (related to an optional task)

This section of the lesson is not mandatory, but it is worth reviewing if you want to become an expert.

### browser.bind() and shared sessions

Since Playwright 1.59, the `browser.bind()` API allows you to launch a browser that can be connected to by various clients simultaneously: CLI, MCP, and test runner.

In practice, it looks like this: the encoding agent launches the browser through the CLI, writes a test, and then runs it through the test runner on the same instance. When the test fails, the agent analyzes the trace using `npx playwright trace` without opening the GUI.

Dashboard (`playwright-cli show`) displays all active sessions with a live preview. Useful when you have several sessions running in parallel, e.g., one for tests and another for exploration.

`page.screencast` (also since v1.59) records a video of the session with annotations on actions. The agent can generate the recording as evidence of the scenario's correctness.

### Stagehand as an alternative

Playwright CLI and MCP are not the only approaches to browser automation with AI. Stagehand (browserbase/stagehand) offers a different model: a hybrid of code and natural language.

Stagehand provides three main methods: `act()` (action in natural language), `extract()` (data extraction from the page via a Zod schema), and `agent()` (multi-step tasks). In version 3, it switched from Playwright to CDP (Chrome DevTools Protocol). Repo: [browserbase/stagehand](https://github.com/browserbase/stagehand).

Difference in approach: Playwright provides deterministic control through the accessibility tree, while Stagehand focuses on natural interaction with AI as the default mode. In this course, where we value stability, documentation, and broad support, Playwright with its extensive ecosystem is the natural choice. Stagehand is worth knowing as an alternative, especially for scraping and automating sites you don't control.

### Composable fixtures vs Page Object Model

In the second edition of the course, we taught the [Page Object Model](https://playwright.dev/docs/pom) pattern: classes representing pages, methods representing interactions, inheritance hierarchy. In 2026, this pattern is replaced by composable fixtures from Playwright.

Fixtures provide the same isolation and reusability as POM but without the ceremony of classes, constructors, and method chains. A fixture that sets up a page with specific test data is easier to understand than a `ProductPage` class with twenty methods. The result: about 30% less code, with no class instantiation in test files.

When does POM still make sense? With large suites (200+ tests), where several engineers need a common vocabulary for interactions with pages. For smaller projects and when working with an agent, fixtures win.

Comparison on a minimal example:

```typescript
// POM — class with methods
```

The seed test from the main part of the lesson naturally pushes the agent towards fixtures: the agent sees the setup/teardown pattern in one function and replicates it in the generated tests. (The example is shortened: we omitted `import { test as base } from '@playwright/test'`, and the teardown assumes that the created deck is selected and visible.)

Practical rule: start with fixtures. Extract page objects only when duplicated interaction logic becomes an obvious cost.

### Teardown as a Playwright project

In the core lesson, we discussed unique identifiers and cleanup per test. For more complex cases, Playwright offers a dedicated mechanism: project teardown.

Configuration requires two steps. First, the setup project declares its teardown:

```typescript
// playwright.config.ts
```

Teardown runs after all dependent tests. In `global.teardown.ts` you can, for example, clean up the test database.

With Supabase, remember Row-Level Security: the client teardown must be logged into the same account that created the data, or use the service role key bypassing RLS. Without this, the teardown will see empty tables despite existing records.

An alternative approach is "teardown-before-setup." Each test starts by removing the data it may have created in the previous run. This guarantees a clean starting state even after a crash, without a dedicated teardown.

### E2E Workflow as a Skill

The `/10x-e2e` skill you received in this lesson is already a packaged flow: the PLAN→GENERATE→REVIEW→VERIFY loop, a phase qualification gate, and seed and rules as levers, all under a single command.

When is it worth it? A single prompt and rule file are enough as long as you run the workflow sporadically. The skill wins when you repeat the same chain multiple times, in several projects or within a team. It provides three things that a prompt does not have: a contract on disk, triggering by name, and progressive disclosure. The body of `SKILL.md` is loaded only upon invocation, and the seed and rules as `references/` are fetched from disk on demand, not burdening the context between runs.

How to build it:

1. **Define the contract.** Input: one risk from `context/foundation/test-plan.md`. Output: a reviewed E2E test that fails when the risk materializes. The same sentence goes into the skill's `description`, as the agent recognizes when to trigger it based on it.
2. **Outline the framework.** Use `skill-creator` to generate `SKILL.md` or write it manually. Keep to a limit of about 500 lines and progressive disclosure, which you already know.
3. **Include `references/`.** Test seed pattern, E2E rules (role-based selectors, waiting for state, independence, cleanup) and prompt template fields, i.e., resources loaded on demand, not the body of `SKILL.md`.
4. **Encode the workflow in `SKILL.md`:** phase qualification gateway (whether the risk is at the browser level and if the feature exists) → PLAN (select risk, map the path) → GENERATE according to the seed and rules → REVIEW against the five anti-patterns with re-prompting → VERIFY through intentional breaking. This is the same loop run by `/10x-e2e`, just adapted to the idioms of your tool.
5. **Run by name** and audit your own `SKILL.md` just like you previously audited other skills: security and progressive disclosure.

The Skill orchestrates the rules and seed, it does not replace them. It is a layer above the quality levers, not instead of them.

## 📚 Additional materials

- [Playwright CLI](https://playwright.dev/agent-cli/introduction) — official CLI documentation for agents: commands, snapshots, sessions, integration with skills
- [Playwright for Coding Agents](https://playwright.dev/docs/getting-started-cli) — getting started: setup CLI with Claude Code, Copilot, Cursor
- [Playwright MCP](https://github.com/microsoft/playwright-mcp) — MCP server repository: modes (snapshot, vision), capabilities, configuration
- [Playwright Test Agents](https://playwright.dev/docs/test-agents) — official planner/generator/healer documentation: initialization, seed test, workflow
- [Playwright Best Practices](https://playwright.dev/docs/best-practices) — official recommendations: selectors, test isolation, auto-waiting, web-first assertions
- [Playwright Authentication](https://playwright.dev/docs/auth) — storageState pattern: session saving, global setup, multi-role testing
- [Playwright Release Notes](https://playwright.dev/docs/release-notes) — browser.bind(), page.screencast, CLI debugger, aria snapshots with bounding boxes
- [How I Used AI to Fix Our E2E Test Architecture](https://dev.to/debs%5Fobrien/how-i-used-ai-to-fix-our-e2e-test-architecture-444a) — Debbie O'Brien (Microsoft/Playwright), rules-file approach for AI + E2E
- [WebTestBench](https://arxiv.org/html/2603.25226) — research on agents in testing: F1 scores, bottleneck checklists, "competent executors, unreliable planners"
- [VLM Visual Testing](https://zakelfassi.com/vlm-visual-testing-chrome-extension) — Zak El Fassi, practical visual testing with a local model: 32 tests, zero dollars
- [State of Playwright AI Ecosystem 2026](https://currents.dev/posts/state-of-playwright-ai-ecosystem-in-2026) — Currents.dev, ecosystem overview: MCP vs CLI, adoption, unresolved issues
- [browserbase/stagehand](https://github.com/browserbase/stagehand) — alternative browser automation framework: act/extract/agent, CDP, hybrid of code and AI
- [Playwright Best Practices 2026](https://getautonoma.com/blog/playwright-best-practices-2026) — composable fixtures, getByRole, snapshot mode as default
- [Why You Shouldn't Use waitForTimeout](https://www.browserstack.com/guide/playwright-waitfortimeout) — BrowserStack, before/after examples for replacing hardcoded waits
- [How to Write E2E Tests for Full Parallelization](https://www.qawolf.com/blog/how-to-write-tests-for-full-parallelization) — QA Wolf, unique identifiers and cleanup patterns
- Prework [\[3.1\]](https://platforma.przeprogramowani.pl/external/10xdevs-3-prework/pl/09) _LLMs and their impact on the daily work of a programmer_ — token budgets and context degradation, here applied to the CLI vs MCP decision
- Prework [\[4.1\]](https://platforma.przeprogramowani.pl/external/10xdevs-3-prework/pl/14) _Tech Stack Overview_ — Playwright in the recommended stack, now operationalized as an agent-browser interface