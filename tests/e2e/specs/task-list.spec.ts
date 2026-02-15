import { test, expect } from '@playwright/test';
import { createTask, completeTask, deleteAllTasks } from '../helpers/api.helpers';

test.beforeEach(async () => {
  await deleteAllTasks();
});

test.describe('Task List', () => {
  test('empty state on fresh DB', async ({ page }) => {
    await page.goto('/list');
    await expect(page.getByText('No tasks yet')).toBeVisible();
    await expect(page.getByText('Add a task above to get started.')).toBeVisible();
  });

  test('tasks display with status chip in list', async ({ page }) => {
    await createTask({ title: 'List task' });

    await page.goto('/list');
    await expect(page.getByText('List task')).toBeVisible();
    await expect(page.getByText('To Do')).toBeVisible();
  });

  test('priority badge visible for non-None priorities', async ({ page }) => {
    await createTask({ title: 'High prio', priority: 'High' });

    await page.goto('/list');
    await expect(page.getByText('High prio')).toBeVisible();
    await expect(page.getByText('High', { exact: true })).toBeVisible();
  });

  test('completed tasks show line-through styling', async ({ page }) => {
    const task = await createTask({ title: 'Done task' });
    await completeTask(task.id);

    await page.goto('/list');
    const taskTitle = page.getByText('Done task');
    await expect(taskTitle).toBeVisible();
    await expect(taskTitle).toHaveCSS('text-decoration-line', 'line-through');
  });
});
