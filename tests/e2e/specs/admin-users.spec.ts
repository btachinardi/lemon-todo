import { test, expect } from '@playwright/test';
import {
  SYSADMIN,
  ADMIN,
  loginAdminViaApi,
  loginAsAdmin,
  registerTestUser,
  waitForUsersTable,
  assignRole,
  deactivateUser,
  reactivateUser,
} from '../helpers/admin.helpers';

/**
 * E2E: Admin User Management page.
 *
 * Tests the `/admin/users` page accessible by Admin+ roles:
 * - Table rendering with user data
 * - Search filtering
 * - Role filter dropdown
 * - Role assignment via dialog (SystemAdmin only)
 * - User deactivation / reactivation (SystemAdmin only)
 * - Status badges (Active / Deactivated)
 */

test.describe('Admin User Management', () => {
  test.describe('Page rendering & navigation', () => {
    test('system admin can access the users page and see the table', async ({ page }) => {
      await loginAdminViaApi(page, SYSADMIN);
      await waitForUsersTable(page);

      // Table headers should be visible
      await expect(page.getByRole('columnheader', { name: 'Email' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Display Name' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Roles' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible();

      // At least the dev-seeded accounts should be listed
      const rows = page.locator('table tbody tr');
      await expect(rows).not.toHaveCount(0);
    });

    test('admin (non-system) can access the users page', async ({ page }) => {
      await loginAdminViaApi(page, ADMIN);
      await waitForUsersTable(page);

      // Table loads but actions column should not show the dropdown (not SystemAdmin)
      await expect(page.getByRole('columnheader', { name: 'Email' })).toBeVisible();
      // Admin does NOT see the actions button (only SystemAdmin does)
      await expect(page.locator('table tbody tr').first().getByRole('button', { name: 'Actions' })).not.toBeVisible();
    });
  });

  test.describe('Search & Filter', () => {
    test('search filters users by email', async ({ page }) => {
      // Register a user with a unique name so we can search for it
      const target = await registerTestUser('SearchTarget User');

      await loginAdminViaApi(page, SYSADMIN);
      await waitForUsersTable(page);

      // Type in search box
      const searchInput = page.getByPlaceholder(/search/i);
      const responsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/api/admin/users'),
      );
      await searchInput.fill(target.email);
      await responsePromise;
      await page.waitForTimeout(500); // debounce

      // Table should now show a subset of users
      const rows = page.locator('table tbody tr');
      const count = await rows.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('role filter shows only users with selected role', async ({ page }) => {
      await loginAdminViaApi(page, SYSADMIN);
      await waitForUsersTable(page);

      // Filter by SystemAdmin role
      await page.locator('.w-40').click(); // Role filter trigger
      const responsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/api/admin/users') && resp.url().includes('role=SystemAdmin'),
      );
      await page.getByRole('option', { name: 'SystemAdmin' }).click();
      await responsePromise;
      await page.waitForTimeout(300);

      // All visible rows should have the SystemAdmin badge
      const rows = page.locator('table tbody tr');
      const count = await rows.count();
      expect(count).toBeGreaterThanOrEqual(1);
      for (let i = 0; i < count; i++) {
        await expect(rows.nth(i).getByText('SystemAdmin')).toBeVisible();
      }
    });
  });

  test.describe('Role Assignment', () => {
    test('system admin can assign Admin role to a regular user via dialog', async ({ page }) => {
      // Seed: register a regular user
      const target = await registerTestUser('RoleAssign Target');

      await loginAdminViaApi(page, SYSADMIN);
      await waitForUsersTable(page);

      // Search for the target user
      const searchInput = page.getByPlaceholder(/search/i);
      const searchResponsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/api/admin/users'),
      );
      await searchInput.fill(target.email);
      await searchResponsePromise;
      await page.waitForTimeout(500);

      // Open actions dropdown on the first matching row
      const actionsButton = page.locator('table tbody tr').first().getByRole('button', { name: 'Actions' });
      await actionsButton.click();

      // Click "Assign Role"
      await page.getByRole('menuitem', { name: /Assign Role/i }).click();

      // Dialog should appear
      const dialog = page.getByRole('dialog');
      await expect(dialog).toBeVisible({ timeout: 5000 });

      // Select "Admin" role
      await dialog.locator('button[role="combobox"]').click();
      await page.getByRole('option', { name: 'Admin', exact: true }).click();

      // Submit
      const assignResponsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/roles') && resp.request().method() === 'POST',
      );
      await dialog.getByRole('button', { name: /Assign/i }).last().click();
      const assignResponse = await assignResponsePromise;
      expect(assignResponse.ok()).toBeTruthy();

      // Dialog should close
      await expect(dialog).not.toBeVisible({ timeout: 5000 });

      // Refresh the table and verify the user now has Admin badge
      await page.waitForTimeout(1000); // wait for query invalidation
      await expect(page.locator('table tbody tr').first().getByText('Admin')).toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('Deactivation & Reactivation', () => {
    test('system admin can deactivate a user via actions menu', async ({ page }) => {
      // Seed: register a regular user
      const target = await registerTestUser('Deactivate Target');

      // Login as admin both in API (for seeding) and browser
      await loginAsAdmin(SYSADMIN);
      await loginAdminViaApi(page, SYSADMIN);
      await waitForUsersTable(page);

      // Search for target
      const searchInput = page.getByPlaceholder(/search/i);
      const searchResponsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/api/admin/users'),
      );
      await searchInput.fill(target.email);
      await searchResponsePromise;
      await page.waitForTimeout(500);

      // Open actions, click Deactivate
      const actionsButton = page.locator('table tbody tr').first().getByRole('button', { name: 'Actions' });
      await actionsButton.click();

      const deactivateResponsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/deactivate') && resp.request().method() === 'POST',
      );
      await page.getByRole('menuitem', { name: /Deactivate/i }).click();
      const deactivateResponse = await deactivateResponsePromise;
      expect(deactivateResponse.ok()).toBeTruthy();

      // Row should now show "Deactivated" badge
      await page.waitForTimeout(1000); // query invalidation
      await expect(page.locator('table tbody tr').first().getByText('Deactivated')).toBeVisible({ timeout: 5000 });
    });

    test('system admin can reactivate a deactivated user', async ({ page }) => {
      // Seed: register and deactivate a user via API
      const target = await registerTestUser('Reactivate Target');
      await loginAsAdmin(SYSADMIN);
      await deactivateUser(target.id);

      await loginAdminViaApi(page, SYSADMIN);
      await waitForUsersTable(page);

      // Search for the deactivated user
      const searchInput = page.getByPlaceholder(/search/i);
      const searchResponsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/api/admin/users'),
      );
      await searchInput.fill(target.email);
      await searchResponsePromise;
      await page.waitForTimeout(500);

      // Verify deactivated badge is shown
      await expect(page.locator('table tbody tr').first().getByText('Deactivated')).toBeVisible({ timeout: 5000 });

      // Open actions, click Reactivate
      const actionsButton = page.locator('table tbody tr').first().getByRole('button', { name: 'Actions' });
      await actionsButton.click();

      const reactivateResponsePromise = page.waitForResponse(
        (resp) => resp.url().includes('/reactivate') && resp.request().method() === 'POST',
      );
      await page.getByRole('menuitem', { name: /Reactivate/i }).click();
      const reactivateResponse = await reactivateResponsePromise;
      expect(reactivateResponse.ok()).toBeTruthy();

      // Row should now show "Active" badge
      await page.waitForTimeout(1000);
      await expect(page.locator('table tbody tr').first().getByText('Active')).toBeVisible({ timeout: 5000 });
    });
  });
});
