import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { loginViaApi } from '../helpers/auth.helpers';
import { createTask } from '../helpers/api.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';

test.describe.serial('Language Switching', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
    await createTask({ title: 'Language test task' });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('default language is English', async () => {
    await page.goto('/board');
    await expect(page.getByLabel('New task title')).toBeVisible();
    await expect(page.getByText('Language test task')).toBeVisible();
  });

  test('switch to Spanish changes UI text', async () => {
    // Open language switcher dropdown (use exact match to avoid matching task cards)
    await page.getByRole('button', { name: 'Language', exact: true }).click();
    // Wait for dropdown menu to render
    await expect(page.getByText('Español')).toBeVisible({ timeout: 5000 });
    await page.getByText('Español').click();

    // Wait for i18n to update — check a known translated element
    await expect(page.getByLabel('Título de la nueva tarea')).toBeVisible({ timeout: 5000 });
  });

  test('Spanish persists across navigation', async () => {
    await page.goto('/list');
    // The list view should still be in Spanish — check the search placeholder or other elements
    await expect(page.getByLabel('Título de la nueva tarea')).toBeVisible();
  });

  test('switch to Portuguese changes UI text', async () => {
    // Open language switcher dropdown (in Spanish, sr-only text is still "Language")
    await page.getByRole('button', { name: 'Language', exact: true }).click();
    await page.getByText('Português (BR)').click();

    await expect(page.getByLabel('Título da nova tarefa')).toBeVisible({ timeout: 5000 });
  });

  test('switch back to English restores original text', async () => {
    // In pt-BR, sr-only text is "Idioma"
    await page.getByRole('button', { name: 'Idioma', exact: true }).click();
    await page.getByText('English').click();

    await expect(page.getByLabel('New task title')).toBeVisible({ timeout: 5000 });
  });
});

test.describe.serial('Language Persistence', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('language persists across page reload', async () => {
    await page.goto('/board');
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Switch to Spanish
    await page.getByRole('button', { name: 'Language', exact: true }).click();
    await expect(page.getByText('Español')).toBeVisible({ timeout: 5000 });
    await page.getByText('Español').click();

    // Verify Spanish is active
    await expect(page.getByLabel('Título de la nueva tarea')).toBeVisible({ timeout: 5000 });

    // Reload the page
    const refreshPromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/auth/refresh'),
      { timeout: 15000 },
    ).catch(() => null);
    await page.reload();
    await refreshPromise;

    // After reload, i18n restores Spanish from localStorage.
    // The nav aria-label is translated to "Cambiar vista" in Spanish.
    await page.getByRole('navigation', { name: 'Cambiar vista' }).waitFor({ state: 'visible', timeout: 10000 });

    // After reload, the UI should still be in Spanish (persisted via localStorage)
    await expect(page.getByLabel('Título de la nueva tarea')).toBeVisible({ timeout: 5000 });
  });
});

test.describe.serial('User Content Language Independence', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
    await createTask({ title: 'Buy groceries' });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('user-created content stays in original language after switching', async () => {
    await page.goto('/board');
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Verify the task is visible in English
    await expect(page.getByText('Buy groceries')).toBeVisible();

    // Switch to Spanish
    await page.getByRole('button', { name: 'Language', exact: true }).click();
    await expect(page.getByText('Español')).toBeVisible({ timeout: 5000 });
    await page.getByText('Español').click();

    // Wait for the UI to switch to Spanish
    await expect(page.getByLabel('Título de la nueva tarea')).toBeVisible({ timeout: 5000 });

    // The user-created task title should remain unchanged (not translated)
    await expect(page.getByText('Buy groceries')).toBeVisible();
  });
});
