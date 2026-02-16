import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { loginViaApi } from '../helpers/auth.helpers';
import { createTask, listTasks } from '../helpers/api.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';

test.describe.serial('Offline Banner', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
    await createTask({ title: 'Offline test task' });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('no offline banner when connected', async () => {
    await page.goto('/board');
    await expect(page.getByText('Offline test task')).toBeVisible();

    // No offline alert should be present
    await expect(page.getByRole('alert')).not.toBeVisible();
  });

  test('offline banner appears when disconnected', async () => {
    // Simulate going offline
    await context.setOffline(true);

    // Dispatch the offline event explicitly (Playwright's setOffline doesn't always fire the event)
    await page.evaluate(() => window.dispatchEvent(new Event('offline')));

    // Wait for the offline event to be detected
    await page.waitForTimeout(1000);

    // The offline banner should appear
    await expect(page.getByRole('alert')).toBeVisible({ timeout: 5000 });
  });

  test('offline banner disappears when reconnected', async () => {
    // Go back online
    await context.setOffline(false);

    // Dispatch the online event and wait for navigator.onLine to update
    await page.evaluate(() => {
      // Force navigator.onLine to be true (Playwright's setOffline should handle this,
      // but dispatch the event to ensure React's useSyncExternalStore picks it up)
      window.dispatchEvent(new Event('online'));
    });

    // The banner uses useSyncExternalStore(subscribe, () => navigator.onLine),
    // so it should update reactively. Give it time for the React render cycle.
    await page.waitForTimeout(2000);

    // Check navigator.onLine is actually true
    const isOnline = await page.evaluate(() => navigator.onLine);
    expect(isOnline).toBe(true);

    // The offline banner (role=alert) should no longer be visible
    await expect(page.getByRole('alert')).not.toBeVisible({ timeout: 5000 });
  });
});

test.describe.serial('Offline Read Support', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
    await createTask({ title: 'Cached offline task' });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('data loads while online', async () => {
    await page.goto('/board');
    await expect(page.getByText('Cached offline task')).toBeVisible();
  });

  test('cached data visible when going offline', async () => {
    // Now go offline — TanStack Query should still show cached data
    await context.setOffline(true);

    // Reload the page — service worker should serve cached responses
    // Note: full navigation may fail, so we test without reload
    await page.waitForTimeout(500);

    // The task should still be visible from React Query cache
    await expect(page.getByText('Cached offline task')).toBeVisible();
  });

  test('back to online restores full functionality', async () => {
    await context.setOffline(false);
    // Dispatch the online event explicitly
    await page.evaluate(() => window.dispatchEvent(new Event('online')));

    // Wait a moment for the network to stabilize
    await page.waitForTimeout(1000);

    // Reload the page — the AuthHydrationProvider will do a silent refresh
    const refreshPromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/auth/refresh'),
      { timeout: 15000 },
    ).catch(() => null);
    await page.reload();
    await refreshPromise;

    // Wait for the authenticated layout to load
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible', timeout: 10000 });
    await expect(page.getByText('Cached offline task')).toBeVisible({ timeout: 10000 });
  });
});

test.describe.serial('Offline Mutation Queue', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('create task offline queues mutation and syncs on reconnect', async () => {
    await page.goto('/board');
    // Wait for the board to fully render (empty board state)
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Go offline
    await context.setOffline(true);
    await page.evaluate(() => window.dispatchEvent(new Event('offline')));
    await page.waitForTimeout(500);

    // Type a task in the QuickAddForm and submit
    const input = page.getByLabel('New task title');
    await input.fill('Offline created task');
    await page.getByRole('button', { name: 'Add Task' }).click();

    // Wait for the offline enqueue toast (info toast: "Change saved offline")
    await expect(page.getByText('Change saved offline')).toBeVisible({ timeout: 5000 });

    // Go back online — the drain should replay the mutation
    await context.setOffline(false);
    await page.evaluate(() => window.dispatchEvent(new Event('online')));

    // Wait for the drain to complete and caches to invalidate
    await page.waitForTimeout(3000);

    // Verify the task now exists on the server via API helper
    const { items } = await listTasks();
    const synced = items.find((t) => t.title === 'Offline created task');
    expect(synced).toBeTruthy();

    // Reload to see the task on the board
    const refreshPromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/auth/refresh'),
      { timeout: 15000 },
    ).catch(() => null);
    await page.reload();
    await refreshPromise;

    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible', timeout: 10000 });
    await expect(page.getByText('Offline created task')).toBeVisible({ timeout: 10000 });
  });
});

test.describe.serial('SyncIndicator Pending Count', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('SyncIndicator shows pending count while offline', async () => {
    await page.goto('/board');
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Go offline
    await context.setOffline(true);
    await page.evaluate(() => window.dispatchEvent(new Event('offline')));
    await page.waitForTimeout(500);

    // Create a task while offline to enqueue a mutation
    const input = page.getByLabel('New task title');
    await input.fill('Sync indicator test task');
    await page.getByRole('button', { name: 'Add Task' }).click();

    // Wait for the enqueue toast
    await expect(page.getByText('Change saved offline')).toBeVisible({ timeout: 5000 });

    // The SyncIndicator (role="status") should display the pending count
    // It shows "1 change pending" when there is 1 queued mutation
    const syncStatus = page.getByRole('status');
    await expect(syncStatus).toBeVisible({ timeout: 5000 });
    await expect(syncStatus).toContainText('1');
    await expect(syncStatus).toContainText('pending');

    // Clean up: go back online so the mutation drains
    await context.setOffline(false);
    await page.evaluate(() => window.dispatchEvent(new Event('online')));
    await page.waitForTimeout(3000);
  });
});
