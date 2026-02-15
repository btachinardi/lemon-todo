import { test, expect } from '@playwright/test';
import { createTask, deleteAllTasks } from '../helpers/api.helpers';
import { loginViaStorage } from '../helpers/auth.helpers';

test.beforeEach(async () => {
  await deleteAllTasks();
});

test.describe('Task Board', () => {
  test('empty columns show "No tasks" text', async ({ page }) => {
    await loginViaStorage(page);
    await page.goto('/');
    const noTasksLabels = page.getByText('No tasks');
    await expect(noTasksLabels).toHaveCount(3);
  });

  test('quick-add form visible with input and button', async ({ page }) => {
    await loginViaStorage(page);
    await page.goto('/');
    await expect(page.getByLabel('New task title')).toBeVisible();
    await expect(page.getByRole('button', { name: /add/i })).toBeVisible();
  });

  test('task card appears in To Do column after API seeding', async ({ page }) => {
    await createTask({ title: 'Seeded task' });

    await loginViaStorage(page);
    await page.goto('/');
    await expect(page.getByText('Seeded task')).toBeVisible();
  });

  test('task card shows priority badge and tags', async ({ page }) => {
    await createTask({ title: 'Priority task', priority: 'High', tags: ['urgent', 'frontend'] });

    await loginViaStorage(page);
    await page.goto('/');
    await expect(page.getByText('Priority task')).toBeVisible();
    await expect(page.getByText('High')).toBeVisible();
    await expect(page.getByText('urgent')).toBeVisible();
    await expect(page.getByText('frontend')).toBeVisible();
  });

  test('multiple tasks render in correct order', async ({ page }) => {
    await createTask({ title: 'First task' });
    await createTask({ title: 'Second task' });
    await createTask({ title: 'Third task' });

    await loginViaStorage(page);
    await page.goto('/');
    const cards = page.locator('[data-slot="card-title"]');
    await expect(cards).toHaveCount(3);
  });
});
