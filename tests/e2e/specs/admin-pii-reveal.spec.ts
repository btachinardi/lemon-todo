import { test, expect } from '@playwright/test';
import { API_BASE } from '../helpers/e2e.config';

/**
 * E2E: Admin Protected Data Reveal (break-the-glass) flow.
 *
 * Uses the dev-seeded SystemAdmin account to:
 * 1. Register a test user whose protected data we'll reveal
 * 2. Login as SystemAdmin
 * 3. Navigate to the admin users page
 * 4. Reveal the test user's protected data via the break-the-glass dialog
 * 5. Verify the plaintext protected data is shown
 */

const SYSADMIN_EMAIL = 'dev.sysadmin@lemondo.dev';
const SYSADMIN_PASSWORD = 'SysAdmin1234';

interface AuthResponse {
  accessToken: string;
  user: { id: string; email: string; displayName: string };
}

/** Extracts the refresh_token value from a response's Set-Cookie header. */
function extractRefreshToken(res: Response): string {
  const setCookieHeader = res.headers.get('set-cookie');
  if (!setCookieHeader) throw new Error('Response missing Set-Cookie header');
  const match = setCookieHeader.match(/refresh_token=([^;]+)/);
  if (!match) throw new Error(`No refresh_token in Set-Cookie: ${setCookieHeader}`);
  return match[1];
}

test.describe('Admin Protected Data Reveal', () => {
  let testUserEmail: string;
  const testUserDisplayName = 'Protected Data Reveal E2E User';

  test.beforeAll(async () => {
    // Register a unique test user whose protected data we'll reveal
    testUserEmail = `pd-e2e-${Date.now()}@lemondo.dev`;
    const res = await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: testUserEmail,
        password: 'TestPass123!',
        displayName: testUserDisplayName,
      }),
    });
    if (!res.ok) throw new Error(`Register failed: ${res.status} ${await res.text()}`);
  });

  test('system admin can reveal protected data via break-the-glass dialog', async ({ page }) => {
    // 1. Login as SystemAdmin via API + inject cookie
    const loginRes = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: SYSADMIN_EMAIL, password: SYSADMIN_PASSWORD }),
    });
    expect(loginRes.ok).toBeTruthy();

    const loginData = (await loginRes.json()) as AuthResponse;
    const refreshToken = extractRefreshToken(loginRes);

    // Inject refresh cookie into Playwright context
    await page.context().addCookies([
      {
        name: 'refresh_token',
        value: refreshToken,
        domain: 'localhost',
        path: '/api/auth',
        httpOnly: true,
        sameSite: 'Strict',
      },
    ]);

    // 2. Navigate to admin users page and wait for silent refresh
    const refreshPromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/auth/refresh'),
    );
    await page.goto('/admin/users');
    const refreshResponse = await refreshPromise;
    expect(refreshResponse.ok()).toBeTruthy();

    // 3. Wait for user management table to load
    await page.getByText('User Management').waitFor({ state: 'visible', timeout: 10000 });

    // 4. Find the test user row by their redacted email pattern
    // The redacted email will be something like "p***@lemondo.dev"
    const userRows = page.locator('table tbody tr');
    await expect(userRows.first()).toBeVisible({ timeout: 10000 });

    // 5. Find the row with our test user (search by exact email hash lookup)
    // Use the search input if available to narrow down
    const searchInput = page.getByPlaceholder(/search/i);
    if (await searchInput.isVisible()) {
      // Set up the response listener BEFORE triggering the search
      const searchResponsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/api/admin/users'),
      );
      await searchInput.fill(testUserEmail);
      // Wait for the table to update with filtered results
      await searchResponsePromise;
      await page.waitForTimeout(500);
    }

    // 6. Click the actions dropdown on the first user row
    const actionsButton = page.locator('table tbody tr').first().getByRole('button', { name: 'Actions' });
    await actionsButton.waitFor({ state: 'visible', timeout: 10000 });
    await actionsButton.click();

    // 7. Click "Reveal Protected Data" from the dropdown menu
    const revealMenuItem = page.getByRole('menuitem', { name: /Reveal Protected Data/i });
    await revealMenuItem.waitFor({ state: 'visible' });
    await revealMenuItem.click();

    // 8. Dialog should appear
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });

    // 9. Select reason
    await dialog.getByText('Select a reason').click();
    await page.getByText('Support Ticket').click();

    // 10. Enter password
    await dialog.getByLabel('Your Password').fill(SYSADMIN_PASSWORD);

    // 11. Submit
    await dialog.getByRole('button', { name: 'Reveal Protected Data' }).click();

    // 12. Verify revealed protected data is shown (the email and display name should now be unredacted)
    // The UI shows revealed values in amber color with a countdown timer
    await expect(page.getByText(testUserEmail)).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(testUserDisplayName)).toBeVisible();
  });
});
