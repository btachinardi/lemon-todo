import { test, expect } from '@playwright/test';
import {
  SYSADMIN,
  loginAdminViaApi,
  loginAsAdmin,
  registerTestUser,
  assignRole,
  waitForUsersTable,
  waitForAuditTable,
} from '../helpers/admin.helpers';

/**
 * E2E: Admin Role Management.
 *
 * Tests role management operations on the `/admin/users` page (SystemAdmin only):
 * - Remove Role via actions dropdown
 * - Audit log reflects role assignment events
 * - Assign Role dialog shows "all roles assigned" message when no roles remain
 */

test.describe('Admin Role Management', () => {
  test('system admin can remove Admin role from a user via actions menu', async ({ page }) => {
    // Seed: register a regular user and assign the Admin role via API
    const target = await registerTestUser('RemoveRole Target');
    await loginAsAdmin(SYSADMIN);
    await assignRole(target.id, 'Admin');

    // Login in the browser as SystemAdmin and navigate to the users page
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
    await actionsButton.waitFor({ state: 'visible', timeout: 10_000 });
    await actionsButton.click();

    // Wait for the DELETE /roles/Admin response and click "Remove Role"
    const removeRoleResponsePromise = page.waitForResponse(
      (resp) => resp.url().includes('/roles/Admin') && resp.request().method() === 'DELETE',
    );
    await page.getByRole('menuitem', { name: /Remove Role/i }).click();
    const removeRoleResponse = await removeRoleResponsePromise;
    expect(removeRoleResponse.ok()).toBeTruthy();

    // Wait for query invalidation then verify the Admin badge is gone
    await page.waitForTimeout(1000);
    const firstRow = page.locator('table tbody tr').first();
    await expect(firstRow.getByText('Admin', { exact: true })).not.toBeVisible({ timeout: 5000 });

    // The user should still show the base "User" badge
    await expect(firstRow.getByText('User', { exact: true })).toBeVisible({ timeout: 5000 });
  });

  test('role changes are reflected in audit log', async ({ page }) => {
    // Seed: register a user and assign Admin role via API
    const target = await registerTestUser('AuditLog RoleChange Target');
    await loginAsAdmin(SYSADMIN);
    await assignRole(target.id, 'Admin');

    // Login in the browser and navigate directly to the audit log page
    await loginAdminViaApi(page, SYSADMIN, '/admin/audit');
    await waitForAuditTable(page);

    // The audit log should contain at least one "Role Assigned" entry from the API seed above
    await expect(page.getByText('Role Assigned').first()).toBeVisible({ timeout: 10_000 });
  });

  test('user with all roles assigned shows all roles assigned message in assign dialog', async ({ page }) => {
    // Seed: register a user and assign both Admin and SystemAdmin via API
    const target = await registerTestUser('AllRoles Target');
    await loginAsAdmin(SYSADMIN);
    await assignRole(target.id, 'Admin');
    await assignRole(target.id, 'SystemAdmin');

    // Login in the browser and navigate to the users page
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
    await actionsButton.waitFor({ state: 'visible', timeout: 10_000 });
    await actionsButton.click();

    // Click "Assign Role" to open the dialog
    await page.getByRole('menuitem', { name: /Assign Role/i }).click();

    // Dialog should appear
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });

    // The dialog should indicate that all roles are already assigned — either via a
    // text message or a disabled submit button (no roles remain in the select)
    const allRolesMessage = dialog.getByText(/all roles/i);
    const submitButton = dialog.getByRole('button', { name: /Assign/i }).last();

    const allRolesMessageVisible = await allRolesMessage.isVisible();
    if (allRolesMessageVisible) {
      await expect(allRolesMessage).toBeVisible({ timeout: 5000 });
    } else {
      // Fallback: submit button should be disabled when no role can be selected
      await expect(submitButton).toBeDisabled({ timeout: 5000 });
    }
  });
});
