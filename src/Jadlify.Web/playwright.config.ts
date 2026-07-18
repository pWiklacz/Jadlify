import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Jadlify E2E tests.
 *
 * The Vite dev server runs on port 5173 and proxies /api + /health to the
 * ASP.NET Core backend (http://localhost:5182). E2E tests hit the Vite dev
 * server directly — the same single-origin model as production.
 *
 * Auth: tests use a saved storageState so individual tests never go through
 * the login UI. Log in once via Playwright CLI, save the state, and every
 * test starts authenticated. See README.md § Local Auth for details.
 */
export default defineConfig({
  testDir: './tests/e2e',

  /* Run tests in files in parallel */
  fullyParallel: true,

  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,

  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,

  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,

  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',

  /* Shared settings for all the projects below. */
  use: {
    /* Base URL — Vite dev server. */
    baseURL: 'http://localhost:5173',

    /* Inject saved auth session so tests start logged in. */
    storageState: 'playwright/.auth/auth.json',

    /* Collect trace when retrying the failed test. */
    trace: 'on-first-retry',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  /* Start Vite dev server before tests (reuse if already running). */
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
  },
});
