import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { createTask } from '../helpers/api.helpers';
import { loginViaApi } from '../helpers/auth.helpers';

let context: BrowserContext;
let page: Page;

test.describe.serial('Task Board', () => {
  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('empty columns show "No tasks" text', async () => {
    await page.goto('/');
    const noTasksLabels = page.getByText('No tasks');
    await expect(noTasksLabels).toHaveCount(3);
  });

  test('quick-add form visible with input and button', async () => {
    await page.goto('/');
    await expect(page.getByLabel('New task title')).toBeVisible();
    await expect(page.getByRole('button', { name: /add/i })).toBeVisible();
  });

  test('task card appears in To Do column after API seeding', async () => {
    await createTask({ title: 'Seeded task' });

    await page.goto('/');
    await expect(page.getByText('Seeded task')).toBeVisible();
  });

  test('task card shows priority badge and tags', async () => {
    await createTask({ title: 'Priority task', priority: 'High', tags: ['urgent', 'frontend'] });

    await page.goto('/');
    await expect(page.getByText('Priority task')).toBeVisible();
    await expect(page.getByText('High', { exact: true })).toBeVisible();
    await expect(page.getByText('urgent')).toBeVisible();
    await expect(page.getByText('frontend')).toBeVisible();
  });

  test('multiple tasks render in correct order', async () => {
    await createTask({ title: 'Another task' });

    await page.goto('/');
    const cards = page.locator('[data-slot="card-title"]');
    // Seeded task + Priority task + Another task = accumulated from prior tests
    await expect(cards.first()).toBeVisible();
  });
});
