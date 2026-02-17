import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { createTask, completeTask } from '../helpers/api.helpers';
import { loginViaApi } from '../helpers/auth.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';
import { setThemeBeforeLoad, waitForTheme } from '../helpers/theme.helpers';

/**
 * Visual regression tests capture screenshots of key views in both light
 * and dark themes. Baseline screenshots are committed to the repo and
 * compared on subsequent runs via Playwright's toHaveScreenshot().
 *
 * Run `npx playwright test --update-snapshots` to regenerate baselines.
 */

// Deterministic viewports for consistent screenshots
const DESKTOP = { width: 1280, height: 720 };
const MOBILE = { width: 375, height: 667 }; // iPhone SE

test.describe('Visual Regression — Dark Theme', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({
      viewport: DESKTOP,
      colorScheme: 'dark',
      reducedMotion: 'reduce',
    });
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('empty board — dark', async () => {
    await page.goto('/board');
    await expect(page.getByText('Your board is empty')).toBeVisible();
    await expect(page).toHaveScreenshot('board-empty-dark.png');
  });

  test('board with tasks — dark', async () => {
    await createTask({ title: 'Design review', priority: 'High', tags: ['frontend'] });
    await createTask({ title: 'Fix login bug', priority: 'Critical', tags: ['backend', 'urgent'] });
    await createTask({ title: 'Write docs', priority: 'Low' });

    await page.goto('/board');
    await expect(page.getByText('Design review')).toBeVisible();
    await expect(page).toHaveScreenshot('board-tasks-dark.png');
  });

  test('list view — dark', async () => {
    await page.goto('/list');
    await expect(page.getByText('Design review')).toBeVisible();
    await expect(page).toHaveScreenshot('list-view-dark.png');
  });
});

test.describe('Visual Regression — Light Theme', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({
      viewport: DESKTOP,
      colorScheme: 'light',
      reducedMotion: 'reduce',
    });
    page = await context.newPage();
    setThemeBeforeLoad(page, 'light');
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('empty board — light', async () => {
    await page.goto('/board');
    await waitForTheme(page, 'light');
    await expect(page.getByText('Your board is empty')).toBeVisible();
    await expect(page).toHaveScreenshot('board-empty-light.png');
  });

  test('board with tasks — light', async () => {
    await createTask({ title: 'Morning standup', priority: 'Medium' });
    await createTask({ title: 'Sprint planning', priority: 'High', tags: ['team'] });

    await page.goto('/board');
    await waitForTheme(page, 'light');
    await expect(page.getByText('Morning standup')).toBeVisible();
    await expect(page).toHaveScreenshot('board-tasks-light.png');
  });

  test('list view — light', async () => {
    await page.goto('/list');
    await waitForTheme(page, 'light');
    await expect(page.getByText('Morning standup')).toBeVisible();
    await expect(page).toHaveScreenshot('list-view-light.png');
  });
});

test.describe('Visual Regression — Auth Pages', () => {
  test('login page', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: DESKTOP,
      colorScheme: 'dark',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();

    await page.goto('/login');
    await expect(page.getByText('Welcome back')).toBeVisible();
    await expect(page).toHaveScreenshot('login-page-dark.png');

    await context.close();
  });

  test('register page', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: DESKTOP,
      colorScheme: 'dark',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();

    await page.goto('/register');
    await expect(page.getByText('Create your account')).toBeVisible();
    await expect(page).toHaveScreenshot('register-page-dark.png');

    await context.close();
  });
});

test.describe('Visual Regression — Landing Page', () => {
  test('landing page — dark', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: DESKTOP,
      colorScheme: 'dark',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();

    await page.goto('/');
    await expect(page.getByText('Your Rules.')).toBeVisible();
    await expect(page).toHaveScreenshot('landing-page-dark.png');

    await context.close();
  });

  test('landing page — light', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: DESKTOP,
      colorScheme: 'light',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();
    setThemeBeforeLoad(page, 'light');

    await page.goto('/');
    await waitForTheme(page, 'light');
    await expect(page.getByText('Your Rules.')).toBeVisible();
    await expect(page).toHaveScreenshot('landing-page-light.png');

    await context.close();
  });
});

// ---------------------------------------------------------------------------
// Mobile viewport tests (375 x 667 — iPhone SE)
// ---------------------------------------------------------------------------

test.describe('Visual Regression — Mobile Dark', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({
      viewport: MOBILE,
      colorScheme: 'dark',
      reducedMotion: 'reduce',
    });
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
    await createTask({ title: 'Mobile dark task', priority: 'High', tags: ['ui'] });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('board — mobile dark', async () => {
    await page.goto('/board');
    await expect(page.getByText('Mobile dark task')).toBeVisible();
    await expect(page).toHaveScreenshot('mobile-board-dark.png');
  });

  test('list view — mobile dark', async () => {
    await page.goto('/list');
    await expect(page.getByText('Mobile dark task')).toBeVisible();
    await expect(page).toHaveScreenshot('mobile-list-dark.png');
  });
});

test.describe('Visual Regression — Mobile Light', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({
      viewport: MOBILE,
      colorScheme: 'light',
      reducedMotion: 'reduce',
    });
    page = await context.newPage();
    setThemeBeforeLoad(page, 'light');
    await loginViaApi(page);
    await completeOnboarding();
    await createTask({ title: 'Mobile light task', priority: 'Medium' });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('board — mobile light', async () => {
    await page.goto('/board');
    await waitForTheme(page, 'light');
    await expect(page.getByText('Mobile light task')).toBeVisible();
    await expect(page).toHaveScreenshot('mobile-board-light.png');
  });

  test('list view — mobile light', async () => {
    await page.goto('/list');
    await waitForTheme(page, 'light');
    await expect(page.getByText('Mobile light task')).toBeVisible();
    await expect(page).toHaveScreenshot('mobile-list-light.png');
  });
});

test.describe('Visual Regression — Mobile Auth', () => {
  test('login — mobile', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: MOBILE,
      colorScheme: 'dark',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();

    await page.goto('/login');
    await expect(page.getByText('Welcome back')).toBeVisible();
    await expect(page).toHaveScreenshot('mobile-login-dark.png');

    await context.close();
  });

  test('register — mobile', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: MOBILE,
      colorScheme: 'dark',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();

    await page.goto('/register');
    await expect(page.getByText('Create your account')).toBeVisible();
    await expect(page).toHaveScreenshot('mobile-register-dark.png');

    await context.close();
  });
});

test.describe('Visual Regression — Mobile Landing', () => {
  test('landing — mobile dark', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: MOBILE,
      colorScheme: 'dark',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();

    await page.goto('/');
    await expect(page.getByText('Your Rules.')).toBeVisible();
    await expect(page).toHaveScreenshot('mobile-landing-dark.png');

    await context.close();
  });

  test('landing — mobile light', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: MOBILE,
      colorScheme: 'light',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();
    setThemeBeforeLoad(page, 'light');

    await page.goto('/');
    await waitForTheme(page, 'light');
    await expect(page.getByText('Your Rules.')).toBeVisible();
    await expect(page).toHaveScreenshot('mobile-landing-light.png');

    await context.close();
  });
});
