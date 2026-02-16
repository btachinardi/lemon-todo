import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { loginViaApi } from '../helpers/auth.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';

test.describe.serial('PWA Configuration', () => {
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

  test('manifest link is present in HTML', async () => {
    await page.goto('/board');

    // Check for the manifest link tag
    const manifestLink = page.locator('link[rel="manifest"]');
    await expect(manifestLink).toHaveCount(1);
  });

  test('theme-color meta tag is present', async () => {
    const themeColor = page.locator('meta[name="theme-color"]');
    await expect(themeColor).toHaveCount(1);
  });

  test('manifest is valid JSON with required fields', async () => {
    await page.goto('/board');

    // Fetch the manifest URL from the link tag
    const manifestHref = await page.locator('link[rel="manifest"]').getAttribute('href');
    expect(manifestHref).toBeTruthy();

    // Fetch and parse the manifest
    const response = await page.goto(manifestHref!);
    expect(response).not.toBeNull();
    expect(response!.status()).toBe(200);

    const manifest = await response!.json();
    expect(manifest.name).toBeTruthy();
    expect(manifest.short_name).toBeTruthy();
    expect(manifest.icons).toBeInstanceOf(Array);
    expect(manifest.icons.length).toBeGreaterThan(0);
    expect(manifest.display).toBe('standalone');
    expect(manifest.theme_color).toBeTruthy();
  });

  test('service worker is registered', async () => {
    await page.goto('/board');
    await page.waitForTimeout(2000);

    // Check if a service worker is registered
    const swRegistered = await page.evaluate(async () => {
      if (!('serviceWorker' in navigator)) return false;
      const registrations = await navigator.serviceWorker.getRegistrations();
      return registrations.length > 0;
    });

    expect(swRegistered).toBe(true);
  });
});
