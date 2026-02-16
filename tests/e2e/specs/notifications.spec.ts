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
    // Close and reopen the dropdown to see updated state
    await page.keyboard.press('Escape');
    await page.waitForTimeout(500);

    const bellButton = page.locator('button').filter({ has: page.locator('svg.lucide-bell') });
    await bellButton.click();

    // Mark all read button should be gone now
    await expect(page.getByRole('button', { name: /Mark all read/i })).not.toBeVisible();
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
