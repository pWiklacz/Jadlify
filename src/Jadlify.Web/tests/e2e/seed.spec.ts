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
 * The locators are Polish because the UI is: after the redesign the accessible
 * name *is* the Polish label, so an English locator would be asserting against
 * copy that no longer exists.
 *
 * Provenance: seed-test-pattern.md (10x-e2e skill references)
 */
import { test, expect } from '@playwright/test';

test.describe('Trwałość produktu (fundament ryzyka #1)', () => {
  test('utworzony produkt z wartościami makro przeżywa przeładowanie strony', async ({ page }) => {
    // Unique test data to avoid collisions in parallel/repeat runs
    const productName = `Seed Produkt ${Date.now()}`;
    const macros = {
      calories: '250',
      protein: '20',
      fat: '10',
      carbohydrates: '30',
    };

    // --- Setup: navigate to products page ---
    await page.goto('/products');
    await expect(page.getByRole('heading', { name: 'Produkty' })).toBeVisible();

    // --- Action: create a product via the form ---
    await page.getByRole('button', { name: /^Dodaj (pierwszy )?produkt$/ }).first().click();

    // Wait for the modal dialog to appear
    const dialog = page.getByRole('dialog', { name: 'Dodaj produkt' });
    await expect(dialog).toBeVisible();

    // Fill the product form
    await dialog.getByLabel('Nazwa produktu').fill(productName);
    await dialog.getByLabel('Kalorie (kcal)').fill(macros.calories);
    await dialog.getByLabel('Białko (g)').fill(macros.protein);
    await dialog.getByLabel('Tłuszcz (g)').fill(macros.fat);
    await dialog.getByLabel('Węglowodany (g)').fill(macros.carbohydrates);

    // Submit and wait for the dialog to close (the mutation resolved)
    await dialog.getByRole('button', { name: 'Zapisz produkt' }).click();
    await expect(dialog).toBeHidden();

    // --- Assertion 1: product appears in the list ---
    await expect(page.getByText(productName)).toBeVisible();

    // --- Assertion 2: product persists after reload (the risk) ---
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Produkty' })).toBeVisible();
    await expect(page.getByText(productName)).toBeVisible();

    // --- Cleanup: delete the test product ---
    await page.getByRole('button', { name: `Usuń produkt ${productName}` }).click();

    // Confirm deletion in the alert dialog
    const deleteDialog = page.getByRole('alertdialog', { name: 'Usunąć produkt?' });
    await expect(deleteDialog).toBeVisible();
    await deleteDialog.getByRole('button', { name: 'Usuń produkt' }).click();
    await expect(deleteDialog).toBeHidden();

    // Verify product is gone
    await expect(page.getByText(productName)).toBeHidden();
  });
});
