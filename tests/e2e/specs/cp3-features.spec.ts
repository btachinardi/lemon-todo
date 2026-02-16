import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { createTask } from '../helpers/api.helpers';
import { loginViaApi } from '../helpers/auth.helpers';

// ─── Task Detail Sheet ───────────────────────────────────────────────

test.describe.serial('Task Detail Sheet', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await createTask({ title: 'Detail sheet task', priority: 'High', tags: ['review'] });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('clicking a card opens the detail sheet with task title', async () => {
    await page.goto('/board');

    // Wait for the task card to be visible
    const card = page.locator('[aria-label="Task: Detail sheet task"]');
    await expect(card).toBeVisible();

    // Click the task card to open the detail sheet
    await card.click();

    // Sheet should open with the task title
    const sheet = page.locator('[data-slot="sheet-content"]');
    await expect(sheet).toBeVisible();
    await expect(sheet.getByText('Detail sheet task')).toBeVisible();
  });

  test('sheet shows priority selector and description textarea', async () => {
    const sheet = page.locator('[data-slot="sheet-content"]');
    await expect(sheet.getByLabel('Task priority')).toBeVisible();
    await expect(sheet.getByLabel('Description')).toBeVisible();
  });

  test('editing description persists after closing and reopening', async () => {
    const sheet = page.locator('[data-slot="sheet-content"]');
    const descriptionField = sheet.getByLabel('Description');

    // Type a description and blur to trigger save
    await descriptionField.fill('Updated description text');
    await descriptionField.blur();
    await page.waitForTimeout(500); // Allow mutation to settle

    // Close the sheet via Escape
    await page.keyboard.press('Escape');
    await expect(sheet).not.toBeVisible();

    // Reopen by clicking the card
    await page.locator('[aria-label="Task: Detail sheet task"]').click();
    await expect(sheet).toBeVisible();

    // Description should be persisted
    await expect(sheet.getByLabel('Description')).toHaveValue('Updated description text');
  });

  test('editing title inline persists', async () => {
    const sheet = page.locator('[data-slot="sheet-content"]');

    // Click the title to enter edit mode
    await sheet.locator('[data-slot="sheet-title"]').click();

    // The title input should appear
    const titleInput = sheet.getByLabel('Task title');
    await expect(titleInput).toBeVisible();

    // Edit the title
    await titleInput.fill('Renamed detail task');
    await titleInput.press('Enter');
    await page.waitForTimeout(500);

    // Title should update
    await expect(sheet.getByText('Renamed detail task')).toBeVisible();
  });

  test('delete task via sheet removes it from board', async () => {
    const sheet = page.locator('[data-slot="sheet-content"]');

    // Click Delete task button
    await sheet.getByRole('button', { name: 'Delete task' }).click();

    // Confirmation appears
    await expect(sheet.getByText('Delete this task?')).toBeVisible();
    await sheet.getByRole('button', { name: 'Confirm' }).click();

    // Sheet closes and task is removed from board
    await expect(sheet).not.toBeVisible();
    await expect(page.getByText('Renamed detail task')).not.toBeVisible();
  });
});

// ─── Filter & Search ─────────────────────────────────────────────────

test.describe.serial('Filter & Search', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);

    // Seed tasks for filtering
    await createTask({ title: 'Buy groceries', priority: 'Low', tags: ['personal'] });
    await createTask({ title: 'Code review PR', priority: 'High', tags: ['work'] });
    await createTask({ title: 'Buy birthday gift', priority: 'Medium', tags: ['personal'] });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('search filters tasks by title', async () => {
    await page.goto('/board');

    // All 3 tasks should be visible initially
    await expect(page.getByText('Buy groceries')).toBeVisible();
    await expect(page.getByText('Code review PR')).toBeVisible();
    await expect(page.getByText('Buy birthday gift')).toBeVisible();

    // Search for "Buy"
    await page.getByLabel('Search tasks').fill('Buy');
    // Wait for debounce (300ms) + re-render
    await page.waitForTimeout(500);

    // Only "Buy" tasks should be visible
    await expect(page.getByText('Buy groceries')).toBeVisible();
    await expect(page.getByText('Buy birthday gift')).toBeVisible();
    await expect(page.getByText('Code review PR')).not.toBeVisible();
  });

  test('clearing search restores all tasks', async () => {
    await page.getByLabel('Clear search').click();
    await page.waitForTimeout(500);

    await expect(page.getByText('Buy groceries')).toBeVisible();
    await expect(page.getByText('Code review PR')).toBeVisible();
    await expect(page.getByText('Buy birthday gift')).toBeVisible();
  });

  test('filter by priority shows matching tasks only', async () => {
    // Select "High" from the priority filter
    await page.getByLabel('Filter by priority').click();
    await page.getByRole('option', { name: 'High' }).click();

    // Only the High priority task should be visible
    await expect(page.getByText('Code review PR')).toBeVisible();
    await expect(page.getByText('Buy groceries')).not.toBeVisible();
    await expect(page.getByText('Buy birthday gift')).not.toBeVisible();
  });

  test('clear all filters restores all tasks', async () => {
    // Click "Clear" button (shows active filter count badge)
    await page.getByRole('button', { name: /Clear/i }).click();
    await page.waitForTimeout(300);

    await expect(page.getByText('Buy groceries')).toBeVisible();
    await expect(page.getByText('Code review PR')).toBeVisible();
    await expect(page.getByText('Buy birthday gift')).toBeVisible();
  });

  test('search with no results shows empty state', async () => {
    await page.getByLabel('Search tasks').fill('nonexistent task xyz');
    await page.waitForTimeout(500);

    await expect(page.getByText('No matching tasks')).toBeVisible();
  });
});

// ─── Theme Toggle ────────────────────────────────────────────────────

test.describe.serial('Theme Toggle', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('default theme applies dark class', async () => {
    await page.goto('/board');
    const htmlClass = await page.locator('html').getAttribute('class');
    expect(htmlClass).toContain('dark');
  });

  test('clicking theme toggle cycles through themes', async () => {
    // Default is dark. Cycle: dark(1) → system(2) → light(0) → dark(1)
    // Click once: dark → system (resolves to light in headless Chromium)
    await page.getByLabel('Dark theme').click();
    await page.waitForTimeout(100);

    // Click again: system → light
    await page.getByLabel('System theme').click();
    await page.waitForTimeout(100);

    const htmlClass = await page.locator('html').getAttribute('class');
    expect(htmlClass).toContain('light');
  });

  test('theme persists across navigation', async () => {
    await page.goto('/list');
    const htmlClass = await page.locator('html').getAttribute('class');
    expect(htmlClass).toContain('light');
  });
});
