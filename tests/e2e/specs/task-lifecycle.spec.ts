import { test, expect } from '@playwright/test';
import { deleteAllTasks } from '../helpers/api.helpers';
import { loginViaApi } from '../helpers/auth.helpers';

test.beforeEach(async () => {
  await deleteAllTasks();
});

test.describe('Task Lifecycle', () => {
  test('create task via form click -> appears on board, input clears', async ({ page }) => {
    await loginViaApi(page);
    await page.goto('/');

    const input = page.getByLabel('New task title');
    await input.fill('Buy groceries');
    await page.getByRole('button', { name: /add/i }).click();

    await expect(page.getByText('Buy groceries')).toBeVisible();
    await expect(input).toHaveValue('');
  });

  test('create task via Enter key -> appears on board', async ({ page }) => {
    await loginViaApi(page);
    await page.goto('/');

    const input = page.getByLabel('New task title');
    await input.fill('Walk the dog');
    await input.press('Enter');

    await expect(page.getByText('Walk the dog')).toBeVisible();
  });

  test('empty title -> Add button disabled', async ({ page }) => {
    await loginViaApi(page);
    await page.goto('/');
    await expect(page.getByRole('button', { name: /add/i })).toBeDisabled();
  });

  test('complete task -> button changes to Mark as incomplete', async ({ page }) => {
    await loginViaApi(page);
    await page.goto('/');

    // Create a task first
    const input = page.getByLabel('New task title');
    await input.fill('Complete me');
    await input.press('Enter');
    await expect(page.getByText('Complete me')).toBeVisible();

    // Complete it
    await page.getByRole('button', { name: 'Mark as complete' }).click();
    await expect(page.getByRole('button', { name: 'Mark as incomplete' })).toBeVisible();
  });

  test('uncomplete task -> button changes back to Mark as complete', async ({ page }) => {
    await loginViaApi(page);
    await page.goto('/');

    // Create and complete a task
    const input = page.getByLabel('New task title');
    await input.fill('Toggle me');
    await input.press('Enter');
    await expect(page.getByText('Toggle me')).toBeVisible();

    await page.getByRole('button', { name: 'Mark as complete' }).click();
    await expect(page.getByRole('button', { name: 'Mark as incomplete' })).toBeVisible();

    // Uncomplete it
    await page.getByRole('button', { name: 'Mark as incomplete' }).click();
    await expect(page.getByRole('button', { name: 'Mark as complete' })).toBeVisible();
  });

  test('create + complete task in list view', async ({ page }) => {
    await loginViaApi(page);
    await page.goto('/list');

    // Create a task
    const input = page.getByLabel('New task title');
    await input.fill('List lifecycle task');
    await input.press('Enter');
    await expect(page.getByText('List lifecycle task')).toBeVisible();

    // Complete it
    await page.getByRole('button', { name: 'Mark as complete' }).click();
    await expect(page.getByRole('button', { name: 'Mark as incomplete' })).toBeVisible();
  });
});
