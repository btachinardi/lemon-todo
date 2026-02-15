import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { createTask } from '../helpers/api.helpers';
import { loginViaApi } from '../helpers/auth.helpers';

let context: BrowserContext;
let page: Page;

test.describe.serial('Navigation', () => {
  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    // Seed a task so the board renders columns instead of EmptyBoard
    await createTask({ title: 'Nav seed task' });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('board page loads at / with 3 column headings', async () => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'To Do' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'In Progress' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Done' })).toBeVisible();
  });

  test('list page loads at /list with quick-add form', async () => {
    await page.goto('/list');
    await expect(page.getByLabel('New task title')).toBeVisible();
    await expect(page.getByRole('button', { name: /add/i })).toBeVisible();
  });

  test('404 page for invalid route shows error', async () => {
    await page.goto('/some-nonexistent-route');
    await expect(page.getByRole('heading', { name: '404' })).toBeVisible();
    await expect(page.getByText('Page not found')).toBeVisible();
  });

  test('Go home link on 404 navigates back to /', async () => {
    await page.goto('/some-nonexistent-route');
    await page.getByRole('link', { name: /go home/i }).click();
    await expect(page).toHaveURL('/');
    // Seeded task ensures columns render (not EmptyBoard)
    await expect(page.getByRole('heading', { name: 'To Do' })).toBeVisible();
  });

  test('LemonDo header visible on board and list pages', async () => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'LemonDo' })).toBeVisible();

    await page.goto('/list');
    await expect(page.getByRole('heading', { name: 'LemonDo' })).toBeVisible();
  });
});
