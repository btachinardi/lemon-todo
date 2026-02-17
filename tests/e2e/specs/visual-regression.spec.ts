import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { createTask, completeTask } from '../helpers/api.helpers';
import { loginViaApi } from '../helpers/auth.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';

/**
 * Visual regression tests capture screenshots of key views in both light
 * and dark themes. Baseline screenshots are committed to the repo and
 * compared on subsequent runs via Playwright's toHaveScreenshot().
 *
 * Run `npx playwright test --update-snapshots` to regenerate baselines.
 */

// Deterministic viewport for consistent screenshots
const VIEWPORT = { width: 1280, height: 720 };

test.describe('Visual Regression — Dark Theme', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({
      viewport: VIEWPORT,
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
      viewport: VIEWPORT,
      colorScheme: 'light',
      reducedMotion: 'reduce',
    });
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('empty board — light', async () => {
    await page.goto('/board');
    await expect(page.getByText('Your board is empty')).toBeVisible();
    await expect(page).toHaveScreenshot('board-empty-light.png');
  });

  test('board with tasks — light', async () => {
    await createTask({ title: 'Morning standup', priority: 'Medium' });
    await createTask({ title: 'Sprint planning', priority: 'High', tags: ['team'] });

    await page.goto('/board');
    await expect(page.getByText('Morning standup')).toBeVisible();
    await expect(page).toHaveScreenshot('board-tasks-light.png');
  });

  test('list view — light', async () => {
    await page.goto('/list');
    await expect(page.getByText('Morning standup')).toBeVisible();
    await expect(page).toHaveScreenshot('list-view-light.png');
  });
});

test.describe('Visual Regression — Auth Pages', () => {
  test('login page', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: VIEWPORT,
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
      viewport: VIEWPORT,
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
      viewport: VIEWPORT,
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
      viewport: VIEWPORT,
      colorScheme: 'light',
      reducedMotion: 'reduce',
    });
    const page = await context.newPage();

    await page.goto('/');
    await expect(page.getByText('Your Rules.')).toBeVisible();
    await expect(page).toHaveScreenshot('landing-page-light.png');

    await context.close();
  });
});
