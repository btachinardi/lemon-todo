import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { loginViaApi } from '../helpers/auth.helpers';
import { loginAdminViaApi } from '../helpers/admin.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';
import { createTask } from '../helpers/api.helpers';
import { setThemeBeforeLoad, waitForTheme } from '../helpers/theme.helpers';

/**
 * WCAG color contrast tests using axe-core.
 *
 * Runs the `color-contrast` rule against every key page in both light and
 * dark themes. The app uses oklch colors via CSS custom properties, so a
 * real browser is required for accurate contrast calculation.
 *
 * Theme is persisted in localStorage under `lemondo-theme` (Zustand persist)
 * and applied as a class on `<html>` (`light` or `dark`).
 */

/** The result shape returned by `AxeBuilder.analyze()`. */
type AxeResults = Awaited<ReturnType<AxeBuilder['analyze']>>;

/** Formats axe-core color-contrast violations into a readable string for test output. */
function formatViolations(violations: AxeResults['violations']): string {
  if (violations.length === 0) return 'No violations';

  return violations
    .flatMap((v) =>
      v.nodes.map((node) => {
        const selector = node.target.map((t) => (Array.isArray(t) ? t.join(' > ') : t)).join(' > ');
        const message = node.failureSummary ?? 'No details';
        return `  - ${selector}\n    ${message.replace(/\n/g, '\n    ')}`;
      }),
    )
    .join('\n');
}

/**
 * Overrides all opacity/animation inline styles so axe-core evaluates final
 * intended colors. Framer Motion sets inline `opacity: 0` on elements that
 * haven't animated yet (e.g., below-fold elements gated by `useInView`).
 * This CSS !important override ensures every element is fully visible.
 */
async function disableMotionArtifacts(page: Page): Promise<void> {
  await page.evaluate(() => {
    const style = document.createElement('style');
    style.id = 'axe-motion-override';
    style.textContent = [
      '*, *::before, *::after {',
      '  opacity: 1 !important;',
      '  animation: none !important;',
      '  transition: none !important;',
      '}',
    ].join('\n');
    document.head.appendChild(style);
  });
  // Let the override cascade and browser reflow
  await page.waitForTimeout(100);
}

/**
 * Runs the axe-core color-contrast rule against the current page state.
 * Returns the violations array and asserts zero violations with a helpful message.
 */
async function assertNoContrastViolations(
  page: Page,
  label: string,
): Promise<void> {
  // Ensure all elements are fully visible (no animation artifacts)
  await disableMotionArtifacts(page);

  const results = await new AxeBuilder({ page })
    .withRules(['color-contrast'])
    .analyze();

  const violations = results.violations;

  expect(
    violations,
    `${label}: found ${violations.length} color-contrast violation(s):\n${formatViolations(violations)}`,
  ).toHaveLength(0);
}

// ---------------------------------------------------------------------------
// Public pages (no auth required)
// ---------------------------------------------------------------------------

test.describe('Color Contrast — Landing Page', () => {
  for (const theme of ['light', 'dark'] as const) {
    test(`landing page has no contrast violations — ${theme}`, async ({ browser }) => {
      const context = await browser.newContext({ colorScheme: theme, reducedMotion: 'reduce' });
      const page = await context.newPage();
      setThemeBeforeLoad(page, theme);

      await page.goto('/');
      await waitForTheme(page, theme);
      await expect(page.getByText('Your Rules.')).toBeVisible();

      await assertNoContrastViolations(page, `Landing (${theme})`);
      await context.close();
    });
  }
});

test.describe('Color Contrast — Login Page', () => {
  for (const theme of ['light', 'dark'] as const) {
    test(`login page has no contrast violations — ${theme}`, async ({ browser }) => {
      const context = await browser.newContext({ colorScheme: theme, reducedMotion: 'reduce' });
      const page = await context.newPage();
      setThemeBeforeLoad(page, theme);

      await page.goto('/login');
      await waitForTheme(page, theme);
      await expect(page.getByText('Welcome back')).toBeVisible();

      await assertNoContrastViolations(page, `Login (${theme})`);
      await context.close();
    });
  }
});

test.describe('Color Contrast — Register Page', () => {
  for (const theme of ['light', 'dark'] as const) {
    test(`register page has no contrast violations — ${theme}`, async ({ browser }) => {
      const context = await browser.newContext({ colorScheme: theme, reducedMotion: 'reduce' });
      const page = await context.newPage();
      setThemeBeforeLoad(page, theme);

      await page.goto('/register');
      await waitForTheme(page, theme);
      await expect(page.getByText('Create your account')).toBeVisible();

      await assertNoContrastViolations(page, `Register (${theme})`);
      await context.close();
    });
  }
});

// ---------------------------------------------------------------------------
// Authenticated pages (require login + onboarding)
// ---------------------------------------------------------------------------

test.describe('Color Contrast — Dashboard (Board)', () => {
  for (const theme of ['light', 'dark'] as const) {
    test.describe.serial(`dashboard — ${theme}`, () => {
      let context: BrowserContext;
      let page: Page;

      test.beforeAll(async ({ browser }) => {
        context = await browser.newContext({ colorScheme: theme, reducedMotion: 'reduce' });
        page = await context.newPage();
        await loginViaApi(page);
        await completeOnboarding();
        // Seed a task so the board renders columns and cards instead of EmptyBoard
        await createTask({ title: 'Contrast check task', priority: 'High', tags: ['test'] });
        // Apply theme after login (loginViaApi navigates to /board)
        setThemeBeforeLoad(page, theme);
      });

      test.afterAll(async () => {
        await context.close();
      });

      test(`board page has no contrast violations — ${theme}`, async () => {
        await page.goto('/board');
        await waitForTheme(page, theme);
        await expect(page.getByText('Contrast check task')).toBeVisible();

        await assertNoContrastViolations(page, `Board (${theme})`);
      });

      test(`list page has no contrast violations — ${theme}`, async () => {
        await page.goto('/list');
        await waitForTheme(page, theme);
        await expect(page.getByText('Contrast check task')).toBeVisible();

        await assertNoContrastViolations(page, `List (${theme})`);
      });
    });
  }
});

test.describe('Color Contrast — Admin Page', () => {
  for (const theme of ['light', 'dark'] as const) {
    test(`admin page has no contrast violations — ${theme}`, async ({ browser }) => {
      const context = await browser.newContext({ colorScheme: theme, reducedMotion: 'reduce' });
      const page = await context.newPage();
      await loginAdminViaApi(page);
      setThemeBeforeLoad(page, theme);

      await page.goto('/admin/users');
      await waitForTheme(page, theme);
      // Wait for the admin users table to render
      await expect(page.getByText('User Management')).toBeVisible({ timeout: 10_000 });

      await assertNoContrastViolations(page, `Admin (${theme})`);
      await context.close();
    });
  }
});
