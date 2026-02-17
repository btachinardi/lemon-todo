import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { createTask, completeTask } from '../helpers/api.helpers';
import { loginViaApi } from '../helpers/auth.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';

let context: BrowserContext;
let page: Page;

test.describe.serial('Task List', () => {
  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('empty state on fresh DB', async () => {
    await page.goto('/list');
    await expect(page.getByText('No tasks yet')).toBeVisible();
    await expect(page.getByText('Add a task above to get started.')).toBeVisible();
  });

  test('tasks display with status chip in list', async () => {
    await createTask({ title: 'List task' });

    await page.goto('/list');
    await expect(page.getByText('List task')).toBeVisible();
    await expect(page.getByText('To Do')).toBeVisible();
  });

  test('priority badge visible for non-None priorities', async () => {
    await createTask({ title: 'High prio', priority: 'High' });

    await page.goto('/list');
    await expect(page.getByText('High prio')).toBeVisible();
    await expect(page.getByText('High', { exact: true })).toBeVisible();
  });

  test('completed tasks show line-through styling', async () => {
    const task = await createTask({ title: 'Done task' });
    await completeTask(task.id);

    await page.goto('/list');
    const taskTitle = page.getByText('Done task');
    await expect(taskTitle).toBeVisible();
    await expect(taskTitle).toHaveCSS('text-decoration-line', 'line-through');
  });
});
