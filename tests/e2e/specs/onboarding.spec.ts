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

    // Wait for celebration animation and completion mutation
    await page.waitForTimeout(2000);

    // Tour tooltip should be gone
    await expect(page.getByText('Create your first task')).not.toBeVisible();
    await expect(page.getByText('Explore your board!')).not.toBeVisible();

    // Verify server-side completion (retry because celebration + mutation takes time)
    let status;
    for (let i = 0; i < 10; i++) {
      status = await getOnboardingStatus();
      if (status.completed) break;
      await page.waitForTimeout(500);
    }
    expect(status!.completed).toBe(true);
    expect(status!.completedAt).not.toBeNull();
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

    // Server should mark as complete (retry because mutation takes time)
    let status;
    for (let i = 0; i < 10; i++) {
      status = await getOnboardingStatus();
      if (status.completed) break;
      await page.waitForTimeout(500);
    }
    expect(status!.completed).toBe(true);
  });
});

test.describe.serial('Onboarding - Existing Users', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    // Complete onboarding via API before navigating
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('existing users who completed onboarding do not see tour', async () => {
    await page.goto('/board');
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Wait to ensure tour doesn't pop up
    await page.waitForTimeout(2000);

    // Step 1 tooltip should NOT be visible
    await expect(page.getByText('Create your first task')).not.toBeVisible();
    await expect(page.locator('[aria-label="Step 1 of 3"]')).not.toBeVisible();
  });
});

test.describe.serial('Onboarding - List View', () => {
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

  test('new user navigating to /list sees onboarding tour step 1', async () => {
    await page.goto('/list');

    // Step 1 tooltip should be visible on list view too
    await expect(page.getByText('Create your first task')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('Type a title in the box above')).toBeVisible();

    // Step indicator shows 1 of 3
    await expect(page.locator('[aria-label="Step 1 of 3"]')).toBeVisible();
  });
});

test.describe.serial('Onboarding - Step Indicators', () => {
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

  test('step indicators update from 1 to 2 to 3 as user progresses', async () => {
    await page.goto('/board');

    // Step 1 indicator visible
    await expect(page.locator('[aria-label="Step 1 of 3"]')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[aria-label="Step 2 of 3"]')).not.toBeVisible();
    await expect(page.locator('[aria-label="Step 3 of 3"]')).not.toBeVisible();

    // Create a task to advance to step 2
    const input = page.getByLabel('New task title');
    await input.fill('Step indicator test task');
    await input.press('Enter');

    // Step 2 indicator visible, step 1 gone
    await expect(page.locator('[aria-label="Step 2 of 3"]')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[aria-label="Step 1 of 3"]')).not.toBeVisible();
    await expect(page.locator('[aria-label="Step 3 of 3"]')).not.toBeVisible();

    // Complete the task to advance to step 3
    await page.getByRole('button', { name: 'Mark as complete' }).first().click();

    // Step 3 indicator visible, steps 1 and 2 gone
    await expect(page.locator('[aria-label="Step 3 of 3"]')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[aria-label="Step 1 of 3"]')).not.toBeVisible();
    await expect(page.locator('[aria-label="Step 2 of 3"]')).not.toBeVisible();
  });
});
