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
    // Open language switcher dropdown
    await page.getByRole('button', { name: /Language/i }).click();
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
    // Open language switcher dropdown (now shows "Idioma" in PT-BR)
    await page.getByRole('button', { name: /Language|Idioma/i }).click();
    await page.getByText('Português (BR)').click();

    await expect(page.getByLabel('Título da nova tarefa')).toBeVisible({ timeout: 5000 });
  });

  test('switch back to English restores original text', async () => {
    await page.getByRole('button', { name: /Idioma/i }).click();
    await page.getByText('English').click();

    await expect(page.getByLabel('New task title')).toBeVisible({ timeout: 5000 });
  });
});
