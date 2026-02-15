import { test, expect } from '@playwright/test';
import { deleteAllTasks } from '../helpers/api.helpers';
import { loginViaStorage } from '../helpers/auth.helpers';

test.beforeEach(async () => {
  await deleteAllTasks();
});

test.describe('Navigation', () => {
  test('board page loads at / with 3 column headings', async ({ page }) => {
    await loginViaStorage(page);
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'To Do' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'In Progress' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Done' })).toBeVisible();
  });

  test('list page loads at /list with quick-add form', async ({ page }) => {
    await loginViaStorage(page);
    await page.goto('/list');
    await expect(page.getByLabel('New task title')).toBeVisible();
    await expect(page.getByRole('button', { name: /add/i })).toBeVisible();
  });

  test('404 page for invalid route shows error', async ({ page }) => {
    await loginViaStorage(page);
    await page.goto('/some-nonexistent-route');
    await expect(page.getByRole('heading', { name: '404' })).toBeVisible();
    await expect(page.getByText('Page not found')).toBeVisible();
  });

  test('Go home link on 404 navigates back to /', async ({ page }) => {
    await loginViaStorage(page);
    await page.goto('/some-nonexistent-route');
    await page.getByRole('link', { name: /go home/i }).click();
    await expect(page).toHaveURL('/');
    await expect(page.getByRole('heading', { name: 'To Do' })).toBeVisible();
  });

  test('LemonDo header visible on board and list pages', async ({ page }) => {
    await loginViaStorage(page);
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'LemonDo' })).toBeVisible();

    await page.goto('/list');
    await expect(page.getByRole('heading', { name: 'LemonDo' })).toBeVisible();
  });
});
