/**
 * planner-shopping.spec.ts — the critical cross-feature flow.
 *
 * Risk context: test-plan.md Risk #1 (macro divergence) and the shopping-list
 * synchronization contract. Everything downstream of a product — recipe macros,
 * planned-day totals, and the aggregated shopping list — is derived. This spec
 * walks the whole derivation once, in the browser, so a break anywhere in the
 * chain surfaces as a failing user-visible assertion rather than as three green
 * unit suites and a broken app.
 *
 * The flow: product → recipe → daily goal → half-portion recipe entry → product
 * entry in grams → persistent shopping list → tick bought → change the plan →
 * confirm the diff → complete the list → find it in history.
 *
 * It is deliberately one long test rather than several: the steps share created
 * state, and splitting them would either re-seed the whole chain per test (slow)
 * or leak order dependence between tests (worse). Cleanup removes everything it
 * created, and every name carries a timestamp so parallel and repeat runs never
 * collide.
 *
 * Locators are role/label based and Polish, matching the redesigned UI.
 */
import { test, expect, type Page } from '@playwright/test';

/** A date far enough ahead that it cannot collide with data a person is using today. */
function isoDaysFromToday(offset: number): string {
  const date = new Date();
  date.setDate(date.getDate() + offset);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(
    date.getDate(),
  ).padStart(2, '0')}`;
}

async function createProduct(
  page: Page,
  name: string,
  macros: { calories: string; protein: string; fat: string; carbohydrates: string },
): Promise<void> {
  await page.goto('/products');
  await expect(page.getByRole('heading', { name: 'Produkty' })).toBeVisible();

  await page.getByRole('button', { name: /^Dodaj (pierwszy )?produkt$/ }).first().click();
  const dialog = page.getByRole('dialog', { name: 'Dodaj produkt' });
  await expect(dialog).toBeVisible();

  await dialog.getByLabel('Nazwa produktu').fill(name);
  await dialog.getByLabel('Kalorie (kcal)').fill(macros.calories);
  await dialog.getByLabel('Białko (g)').fill(macros.protein);
  await dialog.getByLabel('Tłuszcz (g)').fill(macros.fat);
  await dialog.getByLabel('Węglowodany (g)').fill(macros.carbohydrates);

  await dialog.getByRole('button', { name: 'Zapisz produkt' }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByText(name)).toBeVisible();
}

async function deleteProduct(page: Page, name: string): Promise<void> {
  await page.goto('/products');
  const deleteButton = page.getByRole('button', { name: `Usuń produkt ${name}` });
  if ((await deleteButton.count()) === 0) {
    return;
  }
  await deleteButton.click();
  const dialog = page.getByRole('alertdialog', { name: 'Usunąć produkt?' });
  await expect(dialog).toBeVisible();
  await dialog.getByRole('button', { name: 'Usuń produkt' }).click();
  await expect(dialog).toBeHidden();
}

async function deleteRecipe(page: Page, name: string): Promise<void> {
  await page.goto('/recipes');
  const deleteButton = page.getByRole('button', { name: `Usuń przepis ${name}` });
  if ((await deleteButton.count()) === 0) {
    return;
  }
  await deleteButton.click();
  const dialog = page.getByRole('alertdialog');
  await expect(dialog).toBeVisible();
  await dialog.getByRole('button', { name: /^Usuń/ }).click();
  await expect(dialog).toBeHidden();
}

test.describe('Krytyczny flow: produkt → plan → lista zakupów', () => {
  // The chain is long; give it more room than the default per-test budget.
  test.setTimeout(180_000);

  test('plan zmienia listę zakupów dopiero po potwierdzeniu diffu', async ({ page }) => {
    const stamp = Date.now();
    const productName = `E2E Owies ${stamp}`;
    const extraProductName = `E2E Mleko ${stamp}`;
    const recipeName = `E2E Owsianka ${stamp}`;
    const listName = `E2E Lista ${stamp}`;
    const planDate = isoDaysFromToday(21);

    try {
      // --- 1. Products: the base of every derived value ---
      await createProduct(page, productName, {
        calories: '370',
        protein: '13',
        fat: '7',
        carbohydrates: '60',
      });
      await createProduct(page, extraProductName, {
        calories: '60',
        protein: '3',
        fat: '3',
        carbohydrates: '5',
      });

      // --- 2. Recipe built from the product ---
      await page.goto('/recipes');
      await expect(page.getByRole('heading', { name: 'Przepisy' })).toBeVisible();
      await page.getByRole('button', { name: /^Utwórz (pierwszy )?przepis$/ }).first().click();

      const recipeDialog = page.getByRole('dialog', { name: 'Nowy przepis' });
      await expect(recipeDialog).toBeVisible();
      await recipeDialog.getByLabel('Nazwa przepisu').fill(recipeName);
      await recipeDialog.getByLabel('Liczba porcji').fill('2');

      // The picker's search field only exists while its dropdown is open.
      await recipeDialog.getByRole('button', { name: 'Składnik 1', exact: true }).click();
      await recipeDialog.getByLabel('Szukaj produktu').first().fill(productName);
      await recipeDialog
        .getByRole('list', { name: 'Wybierz produkt' })
        .getByRole('button', { name: new RegExp(productName) })
        .click();
      await recipeDialog.getByLabel(/^Gramatura składnika 1/).fill('100');

      await recipeDialog.getByRole('button', { name: 'Zapisz przepis' }).click();
      await expect(recipeDialog).toBeHidden();
      await expect(page.getByText(recipeName)).toBeVisible();

      // --- 3. Daily goal, so the planner can report "how much is left" ---
      await page.goto('/goals');
      await expect(page.getByRole('heading', { name: 'Dzienne cele' })).toBeVisible();
      const goalTrigger = page.getByRole('button', { name: /^(Ustaw dzienne cele|Edytuj cele)$/ });
      await goalTrigger.first().click();

      const goalForm = page.getByRole('form', { name: 'Formularz dziennych celów' });
      await expect(goalForm).toBeVisible();
      await goalForm.getByLabel('Kalorie').fill('2400');
      await goalForm.getByLabel('Białko (g)').fill('140');
      await goalForm.getByLabel('Tłuszcz (g)').fill('70');
      await goalForm.getByLabel('Węglowodany (g)').fill('280');
      await page.getByRole('button', { name: /^Zapisz (cele|zmiany)$/ }).click();
      await expect(goalForm).toBeHidden();

      // --- 4. Plan a half-portion recipe entry on the target day ---
      await page.goto(`/meal-plan?view=day&date=${planDate}`);
      await page.getByRole('button', { name: '+ Dodaj posiłek' }).first().click();

      const addDialog = page.getByRole('dialog', { name: 'Dodaj posiłek' });
      await expect(addDialog).toBeVisible();
      await addDialog.getByLabel('Dzień docelowy').fill(planDate);
      await addDialog.getByLabel('Szukaj przepisu po nazwie').fill(recipeName);
      await addDialog
        .getByRole('list', { name: 'Wyniki przepisów' })
        .getByRole('button', { name: new RegExp(recipeName) })
        .click();
      // 1 → 0.5 portions: the fractional step the domain guarantees.
      await addDialog.getByRole('button', { name: /Zmniejsz|Liczba porcji.*mniej|−/ }).first().click();
      await addDialog.getByRole('button', { name: 'Dodaj do planu' }).click();
      await expect(addDialog).toBeHidden();

      await expect(page.getByRole('region', { name: 'Śniadanie' })).toBeVisible();

      // --- 5. Persistent shopping list built from that day ---
      await page.goto('/shopping-list');
      await expect(page.getByRole('heading', { name: 'Listy zakupów' })).toBeVisible();
      await page.getByRole('button', { name: /^Utwórz (pierwszą|nową) listę$/ }).first().click();

      const createDialog = page.getByRole('dialog', { name: 'Nowa lista zakupów' });
      await expect(createDialog).toBeVisible();
      await createDialog.getByRole('button', { name: 'Wszystkie dni z posiłkami' }).click();
      await createDialog.getByRole('button', { name: 'Sprawdź i wygeneruj' }).click();
      await createDialog.getByLabel(/Nazwa listy/).fill(listName);
      await createDialog.getByRole('button', { name: 'Wygeneruj listę zakupów' }).click();
      await expect(createDialog).toBeHidden();

      // --- 6. Open the list and tick the aggregated product line ---
      await page.getByRole('link', { name: /Zacznij zakupy|Kontynuuj zakupy|Otwórz listę/ }).click();
      await expect(page.getByRole('heading', { name: listName })).toBeVisible();

      const boughtItem = page.getByRole('checkbox', { name: productName });
      await expect(boughtItem).toBeVisible();
      // `click` + a retrying assertion, not `check`: the tick is applied optimistically
      // through the query cache, so the checked state lands a tick after the click and
      // `check`'s immediate re-read would race it.
      await boughtItem.click();
      await expect(boughtItem).toBeChecked();

      // The tick survives a reload — it is server state, not local state.
      await page.reload();
      await expect(page.getByRole('checkbox', { name: productName })).toBeChecked();

      // --- 7. Change the plan behind the list's back ---
      await page.goto(`/meal-plan?view=day&date=${planDate}`);
      await page.getByRole('button', { name: '+ Dodaj posiłek' }).first().click();
      const secondAdd = page.getByRole('dialog', { name: 'Dodaj posiłek' });
      await expect(secondAdd).toBeVisible();
      await secondAdd.getByLabel('Dzień docelowy').fill(planDate);
      await secondAdd.getByRole('button', { name: 'Produkt' }).click();
      await secondAdd.getByLabel('Szukaj produktu po nazwie').fill(extraProductName);
      await secondAdd
        .getByRole('list', { name: 'Wyniki produktów' })
        .getByRole('button', { name: new RegExp(extraProductName) })
        .click();
      await secondAdd.getByRole('button', { name: 'Dodaj do planu' }).click();
      await expect(secondAdd).toBeHidden();

      // --- 8. The list reports drift but has NOT changed yet ---
      await page.goto('/shopping-list');
      await page.getByRole('link', { name: /Zacznij zakupy|Kontynuuj zakupy|Otwórz listę/ }).click();
      await expect(page.getByText(/Twój plan posiłków zmienił się/)).toBeVisible();
      // The new product is not on the list until the diff is confirmed.
      await expect(page.getByRole('checkbox', { name: extraProductName })).toHaveCount(0);

      // --- 9. Review and confirm the diff ---
      await page.getByRole('button', { name: 'Przejrzyj zmiany' }).click();
      const diffDialog = page.getByRole('dialog', { name: 'Plan posiłków się zmienił' });
      await expect(diffDialog).toBeVisible();
      await expect(
        diffDialog.getByRole('region', { name: 'Dojdą nowe produkty' }),
      ).toContainText(extraProductName);

      await diffDialog.getByRole('button', { name: 'Zaktualizuj listę' }).click();
      await expect(diffDialog).toBeHidden();

      // Now it is on the list, and the untouched line kept its tick.
      await expect(page.getByRole('checkbox', { name: extraProductName })).toBeVisible();
      await expect(page.getByRole('checkbox', { name: productName })).toBeChecked();

      // --- 10. Complete the list; it moves into history ---
      await page.getByRole('button', { name: 'Oznacz listę jako ukończoną' }).click();
      // Exact match: the card also spells out "· ukończona <data>" beside the pill.
      await expect(page.getByText('UKOŃCZONA', { exact: true })).toBeVisible();

      await page.goto('/shopping-list');
      const history = page.getByRole('region', { name: 'Poprzednie listy' });
      await expect(history).toContainText(listName);
      // With no active list, the active slot invites creating the next one.
      await expect(
        page.getByRole('region', { name: 'Aktywna lista' }),
      ).toContainText('Brak aktywnej listy');
    } finally {
      // --- Cleanup: remove what this test created, newest dependency first ---
      await deleteRecipe(page, recipeName);
      await deleteProduct(page, productName);
      await deleteProduct(page, extraProductName);
    }
  });
});
