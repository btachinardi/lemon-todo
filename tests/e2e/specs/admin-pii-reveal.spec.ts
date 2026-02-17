import { test, expect } from '@playwright/test';
import {
  SYSADMIN,
  loginAdminViaApi,
  registerTestUser,
  waitForUsersTable,
} from '../helpers/admin.helpers';

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

test.describe('Admin Protected Data Reveal', () => {
  let testUserEmail: string;
  const testUserDisplayName = 'Protected Data Reveal E2E User';

  test.beforeAll(async () => {
    const user = await registerTestUser(testUserDisplayName);
    testUserEmail = user.email;
  });

  test('system admin can reveal protected data via break-the-glass dialog', async ({ page }) => {
    await loginAdminViaApi(page, SYSADMIN);
    await waitForUsersTable(page);

    // Search for the test user by email
    const searchInput = page.getByPlaceholder(/search/i);
    if (await searchInput.isVisible()) {
      const searchResponsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/api/admin/users'),
      );
      await searchInput.fill(testUserEmail);
      await searchResponsePromise;
      await page.waitForTimeout(500);
    }

    // Click the actions dropdown on the first user row
    const actionsButton = page.locator('table tbody tr').first().getByRole('button', { name: 'Actions' });
    await actionsButton.waitFor({ state: 'visible', timeout: 10_000 });
    await actionsButton.click();

    // Click "Reveal Protected Data" from the dropdown menu
    const revealMenuItem = page.getByRole('menuitem', { name: /Reveal Protected Data/i });
    await revealMenuItem.waitFor({ state: 'visible' });
    await revealMenuItem.click();

    // Dialog should appear
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });

    // Select reason
    await dialog.getByText('Select a reason').click();
    await page.getByText('Support Ticket').click();

    // Enter password
    await dialog.getByLabel('Your Password').fill(SYSADMIN.password);

    // Submit
    await dialog.getByRole('button', { name: 'Reveal Protected Data' }).click();

    // Verify revealed protected data is shown (the email and display name should now be unredacted)
    // The UI shows revealed values in amber color with a countdown timer
    await expect(page.getByText(testUserEmail)).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(testUserDisplayName)).toBeVisible();
  });
});
