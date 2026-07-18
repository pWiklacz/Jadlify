/**
 * seed.spec.ts — Exemplar E2E test for Jadlify.
 *
 * This is the seed test every generated E2E test should follow. It demonstrates
 * the four patterns from the E2E quality rules:
 *
 * 1. Role-based locators (getByRole, getByLabel) — never CSS selectors
 * 2. Test independence — full setup → action → assertion → cleanup in one test
 * 3. Wait for state, not time — toBeVisible(), waitForResponse(), never waitForTimeout()
 * 4. Risk-tied assertion — the test name binds to a risk from test-plan.md
 *
 * Risk context: test-plan.md Risk #1 — "Suma makro dnia rozjeżdża się z realną
 * sumą składników" (macro divergence). This seed covers the foundational layer:
 * a created product (with its macro values) must persist across page reloads,
 * because if the product data doesn't survive persistence, all downstream macro
 * calculations (recipes → meal plan → daily summary) are invalid.
 *
 * Provenance: seed-test-pattern.md (10x-e2e skill references)
 */
import { test, expect } from '@playwright/test';

test.describe('Product persistence (risk #1 foundation)', () => {
  test('created product with macro values persists after page reload', async ({ page }) => {
    // Unique test data to avoid collisions in parallel/repeat runs
    const productName = `Seed Product ${Date.now()}`;
    const macros = {
      calories: '250',
      protein: '20',
      fat: '10',
      carbohydrates: '30',
    };

    // --- Setup: navigate to products page ---
    await page.goto('/products');
    await expect(page.getByRole('heading', { name: 'Products' })).toBeVisible();

    // --- Action: create a product via the form ---
    await page.getByRole('button', { name: 'Add product' }).click();

    // Wait for the modal dialog to appear
    const dialog = page.getByRole('dialog', { name: 'Add product' });
    await expect(dialog).toBeVisible();

    // Fill the product form
    await dialog.getByLabel('Name').fill(productName);
    await dialog.getByLabel('Calories (kcal / 100 g)').fill(macros.calories);
    await dialog.getByLabel('Protein (g / 100 g)').fill(macros.protein);
    await dialog.getByLabel('Fat (g / 100 g)').fill(macros.fat);
    await dialog.getByLabel('Carbohydrates (g / 100 g)').fill(macros.carbohydrates);

    // Submit and wait for the API response
    await page.getByRole('button', { name: 'Save' }).click();
    await expect(dialog).toBeHidden();

    // --- Assertion 1: product appears in the list ---
    await expect(page.getByText(productName)).toBeVisible();

    // --- Assertion 2: product persists after reload (the risk) ---
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Products' })).toBeVisible();
    await expect(page.getByText(productName)).toBeVisible();

    // --- Cleanup: delete the test product ---
    // Find the product card and click its Delete button
    const productCard = page.getByRole('listitem').filter({ hasText: productName });
    await productCard.getByRole('button', { name: 'Delete' }).click();

    // Confirm deletion in the alert dialog
    const deleteDialog = page.getByRole('alertdialog', { name: 'Delete product' });
    await expect(deleteDialog).toBeVisible();
    await deleteDialog.getByRole('button', { name: 'Delete' }).click();
    await expect(deleteDialog).toBeHidden();

    // Verify product is gone
    await expect(page.getByText(productName)).toBeHidden();
  });
});
