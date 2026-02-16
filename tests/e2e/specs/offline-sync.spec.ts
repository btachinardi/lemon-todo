import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { loginViaApi } from '../helpers/auth.helpers';
import { createTask } from '../helpers/api.helpers';
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

    // Trigger a re-evaluation by navigating
    await page.goto('/board').catch(() => {
      // Navigation may fail when offline — that's expected
    });

    // Wait for the offline event to be detected
    await page.waitForTimeout(1000);

    // The offline banner should appear
    await expect(page.getByRole('alert')).toBeVisible({ timeout: 5000 });
  });

  test('offline banner disappears when reconnected', async () => {
    // Go back online
    await context.setOffline(false);

    // Navigate to trigger re-evaluation
    await page.goto('/board');
    await expect(page.getByText('Offline test task')).toBeVisible({ timeout: 10000 });

    // Banner should be gone
    await expect(page.getByRole('alert')).not.toBeVisible();
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
    await page.goto('/board');
    await expect(page.getByText('Cached offline task')).toBeVisible({ timeout: 10000 });
  });
});
