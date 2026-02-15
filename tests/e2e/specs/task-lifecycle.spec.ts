import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { loginViaApi } from '../helpers/auth.helpers';

let context: BrowserContext;
let page: Page;

test.describe.serial('Task Lifecycle', () => {
  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('create task via form click -> appears on board, input clears', async () => {
    await page.goto('/');

    const input = page.getByLabel('New task title');
    await input.fill('Buy groceries');
    await page.getByRole('button', { name: /add/i }).click();

    await expect(page.getByText('Buy groceries')).toBeVisible();
    await expect(input).toHaveValue('');
  });

  test('create task via Enter key -> appears on board', async () => {
    await page.goto('/');

    const input = page.getByLabel('New task title');
    await input.fill('Walk the dog');
    await input.press('Enter');

    await expect(page.getByText('Walk the dog')).toBeVisible();
  });

  test('empty title -> Add button disabled', async () => {
    await page.goto('/');
    await expect(page.getByRole('button', { name: /add/i })).toBeDisabled();
  });

  test('complete task -> button changes to Mark as incomplete', async () => {
    await page.goto('/');

    // Create a task first
    const input = page.getByLabel('New task title');
    await input.fill('Complete me');
    await input.press('Enter');
    await expect(page.getByText('Complete me')).toBeVisible();

    // Complete it
    await page.getByRole('button', { name: 'Mark as complete' }).first().click();
    await expect(page.getByRole('button', { name: 'Mark as incomplete' })).toBeVisible();
  });

  test('uncomplete task -> button changes back to Mark as complete', async () => {
    // The "Complete me" task from prior test is still completed
    await page.goto('/');

    // Uncomplete it
    await page.getByRole('button', { name: 'Mark as incomplete' }).first().click();
    await expect(page.getByRole('button', { name: 'Mark as complete' }).first()).toBeVisible();
  });

  test('create + complete task in list view', async () => {
    await page.goto('/list');

    // Create a task
    const input = page.getByLabel('New task title');
    await input.fill('List lifecycle task');
    await input.press('Enter');
    await expect(page.getByText('List lifecycle task')).toBeVisible();

    // Complete it — scope to the task row (div[role="button"] with aria-label)
    const taskRow = page.locator('[role="button"][aria-label="Task: List lifecycle task"]');
    await taskRow.getByRole('button', { name: 'Mark as complete' }).click();
    await expect(taskRow.getByRole('button', { name: 'Mark as incomplete' })).toBeVisible();
  });
});
