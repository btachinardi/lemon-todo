import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { loginViaApi } from '../helpers/auth.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';
import {
  listNotifications,
  getUnreadCount,
  markNotificationRead,
  markAllNotificationsRead,
} from '../helpers/notification.helpers';

test.describe.serial('Notifications', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    // Registration creates a Welcome notification automatically
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('welcome notification exists after registration', async () => {
    const result = await listNotifications();
    expect(result.totalCount).toBeGreaterThanOrEqual(1);

    const welcome = result.items.find((n) => n.type === 'Welcome');
    expect(welcome).toBeDefined();
    expect(welcome!.title).toBe('Welcome to Lemon.DO!');
    expect(welcome!.isRead).toBe(false);
  });

  test('unread count is at least 1', async () => {
    const result = await getUnreadCount();
    expect(result.count).toBeGreaterThanOrEqual(1);
  });

  test('notification bell shows unread badge', async () => {
    await page.goto('/board');

    // Bell button should be visible with a badge
    const bellButton = page.locator('button').filter({ has: page.locator('svg.lucide-bell') });
    await expect(bellButton).toBeVisible();

    // Badge should show the count
    const badge = bellButton.locator('span').first();
    await expect(badge).toBeVisible();
  });

  test('clicking bell opens notification dropdown', async () => {
    const bellButton = page.locator('button').filter({ has: page.locator('svg.lucide-bell') });
    await bellButton.click();

    // Dropdown content should appear
    await expect(page.getByText('Notifications').first()).toBeVisible();
    await expect(page.getByText('Welcome to Lemon.DO!')).toBeVisible();
  });

  test('mark all read button is visible when unread notifications exist', async () => {
    await expect(page.getByRole('button', { name: /Mark all read/i })).toBeVisible();
  });

  test('clicking mark all read clears unread badge', async () => {
    await page.getByRole('button', { name: /Mark all read/i }).click();

    // Wait for the mutation to complete
    await page.waitForTimeout(1000);

    // Verify via API
    const result = await getUnreadCount();
    expect(result.count).toBe(0);
  });

  test('empty state shows when no unread notifications', async () => {
    // Wait for the mark-all-read mutation to propagate, then reload to get fresh data
    await page.waitForTimeout(1000);

    // Close dropdown and reload to ensure fresh data from server
    await page.keyboard.press('Escape');
    await page.reload();
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Open the notification dropdown
    const bellButton = page.locator('button').filter({ has: page.locator('svg.lucide-bell') });
    await bellButton.click();

    // Mark all read button should be gone now
    await expect(page.getByRole('button', { name: /Mark all read/i })).not.toBeVisible({ timeout: 5000 });
  });
});

test.describe.serial('Notification API', () => {
  test.beforeAll(async ({ browser }) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    await loginViaApi(page);
    await context.close();
  });

  test('mark individual notification as read', async () => {
    const list = await listNotifications();
    if (list.items.length === 0) return;

    const notification = list.items[0];
    await markNotificationRead(notification.id);

    const updated = await listNotifications();
    const marked = updated.items.find((n) => n.id === notification.id);
    expect(marked?.isRead).toBe(true);
  });

  test('mark all notifications as read', async () => {
    await markAllNotificationsRead();
    const result = await getUnreadCount();
    expect(result.count).toBe(0);
  });
});

test.describe.serial('Notification - Click to Mark Read', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    // Registration creates a Welcome notification automatically (unread)
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('clicking an unread notification marks it as read', async () => {
    // Confirm the welcome notification is unread via API
    const before = await listNotifications();
    const welcomeBefore = before.items.find((n) => n.type === 'Welcome');
    expect(welcomeBefore).toBeDefined();
    expect(welcomeBefore!.isRead).toBe(false);

    await page.goto('/board');

    // Open the notification dropdown
    const bellButton = page.locator('button').filter({ has: page.locator('svg.lucide-bell') });
    await bellButton.click();
    await expect(page.getByText('Welcome to Lemon.DO!')).toBeVisible();

    // Click the unread notification item to mark it as read
    await page.getByText('Welcome to Lemon.DO!').click();

    // Wait for the mark-as-read mutation to complete
    await page.waitForTimeout(1000);

    // Verify the notification is now marked as read via API
    let after;
    for (let i = 0; i < 10; i++) {
      after = await listNotifications();
      const welcomeAfter = after.items.find((n) => n.type === 'Welcome');
      if (welcomeAfter?.isRead) break;
      await page.waitForTimeout(500);
    }
    const welcomeAfter = after!.items.find((n) => n.type === 'Welcome');
    expect(welcomeAfter?.isRead).toBe(true);
  });
});

test.describe.serial('Notification - Dropdown Closes on Outside Click', () => {
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

  test('notification dropdown closes when clicking outside', async () => {
    await page.goto('/board');
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Open the notification dropdown
    const bellButton = page.locator('button').filter({ has: page.locator('svg.lucide-bell') });
    await bellButton.click();

    // Verify dropdown is open — the "Notifications" heading should be visible
    await expect(page.getByText('Notifications').first()).toBeVisible();

    // Click outside the dropdown (on the main content area)
    await page.locator('main').click({ position: { x: 10, y: 10 } });

    // Dropdown should close — the "Notifications" heading inside the popover should disappear
    await expect(page.getByText('Notifications').first()).not.toBeVisible({ timeout: 5000 });
  });
});

test.describe.serial('Notification - List Scrollable', () => {
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

  test('notification list container is scrollable when content overflows', async () => {
    await page.goto('/board');
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Open the notification dropdown
    const bellButton = page.locator('button').filter({ has: page.locator('svg.lucide-bell') });
    await bellButton.click();

    // Verify the dropdown content area exists with overflow-y-auto (scrollable container)
    const scrollContainer = page.locator('.max-h-80.overflow-y-auto');
    await expect(scrollContainer).toBeVisible();

    // Verify the notification list renders at least the welcome notification
    await expect(page.getByText('Welcome to Lemon.DO!')).toBeVisible();
  });
});
