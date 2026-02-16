import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { loginViaApi } from '../helpers/auth.helpers';
import { getOnboardingStatus, completeOnboarding } from '../helpers/onboarding.helpers';

test.describe.serial('Onboarding Tour', () => {
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

  test('new user sees onboarding tour step 1', async () => {
    // Verify onboarding is not completed for new users
    const status = await getOnboardingStatus();
    expect(status.completed).toBe(false);

    await page.goto('/board');

    // Step 1 tooltip should be visible
    await expect(page.getByText('Create your first task')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('Type a title in the box above')).toBeVisible();

    // Step indicator shows 1 of 3
    await expect(page.locator('[aria-label="Step 1 of 3"]')).toBeVisible();

    // Skip button is available
    await expect(page.getByRole('button', { name: 'Skip tour' })).toBeVisible();
  });

  test('creating a task auto-advances to step 2', async () => {
    const input = page.getByLabel('New task title');
    await input.fill('My onboarding task');
    await input.press('Enter');

    // Step 2 tooltip should appear
    await expect(page.getByText('Complete your task')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[aria-label="Step 2 of 3"]')).toBeVisible();
  });

  test('completing task auto-advances to step 3', async () => {
    // Click the checkbox to complete the task
    await page.getByRole('button', { name: 'Mark as complete' }).first().click();

    // Step 3 tooltip should appear
    await expect(page.getByText('Explore your board!')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[aria-label="Step 3 of 3"]')).toBeVisible();

    // Finish button visible (not Skip)
    await expect(page.getByRole('button', { name: 'Got it!' })).toBeVisible();
  });

  test('clicking Finish completes onboarding with celebration', async () => {
    await page.getByRole('button', { name: 'Got it!' }).click();

    // Wait for celebration and completion
    await page.waitForTimeout(2000);

    // Tour tooltip should be gone
    await expect(page.getByText('Create your first task')).not.toBeVisible();
    await expect(page.getByText('Explore your board!')).not.toBeVisible();

    // Verify server-side completion
    const status = await getOnboardingStatus();
    expect(status.completed).toBe(true);
    expect(status.completedAt).not.toBeNull();
  });

  test('tour does not appear on reload after completion', async () => {
    await page.reload();
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Wait a bit to ensure tour doesn't pop up
    await page.waitForTimeout(2000);
    await expect(page.getByText('Create your first task')).not.toBeVisible();
  });
});

test.describe.serial('Onboarding Skip', () => {
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

  test('skip button dismisses tour and completes onboarding', async () => {
    await page.goto('/board');

    // Step 1 should be visible
    await expect(page.getByText('Create your first task')).toBeVisible({ timeout: 10000 });

    // Click skip
    await page.getByRole('button', { name: 'Skip tour' }).click();

    // Tour should disappear
    await expect(page.getByText('Create your first task')).not.toBeVisible();

    // Server should mark as complete
    const status = await getOnboardingStatus();
    expect(status.completed).toBe(true);
  });
});
