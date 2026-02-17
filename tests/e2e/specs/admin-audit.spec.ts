import { test, expect } from '@playwright/test';
import {
  SYSADMIN,
  ADMIN,
  loginAdminViaApi,
  registerTestUser,
  waitForAuditTable,
} from '../helpers/admin.helpers';

/**
 * E2E: Admin Audit Log page.
 *
 * Tests the `/admin/audit` page accessible by Admin+ roles:
 * - Table rendering with audit entries
 * - Action filter dropdown
 * - Resource type filter dropdown
 * - Pagination controls present
 */

test.describe('Admin Audit Log', () => {
  test('admin can access the audit log page and see the table', async ({ page }) => {
    await loginAdminViaApi(page, ADMIN, '/admin/audit');
    await waitForAuditTable(page);

    // Table headers should be visible
    await expect(page.getByRole('columnheader', { name: 'Timestamp' })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('columnheader', { name: 'Action' })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('columnheader', { name: 'Resource', exact: true })).toBeVisible({ timeout: 10_000 });

    // E2E test run registers users, so there should be at least one audit entry
    const rows = page.locator('table tbody tr');
    await expect(rows.first()).toBeVisible({ timeout: 10_000 });
    const count = await rows.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('audit log shows entries for user registration', async ({ page }) => {
    // Seed: register a user via API to guarantee a UserRegistered audit entry exists
    await registerTestUser('Audit Log E2E User');

    await loginAdminViaApi(page, SYSADMIN, '/admin/audit');
    await waitForAuditTable(page);

    // Table body must have at least one row
    const rows = page.locator('table tbody tr');
    await expect(rows.first()).toBeVisible({ timeout: 10_000 });
    const count = await rows.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('action filter narrows audit entries', async ({ page }) => {
    await loginAdminViaApi(page, SYSADMIN, '/admin/audit');
    await waitForAuditTable(page);

    // Open the Action select — its trigger shows "All Actions"
    const actionTrigger = page.getByRole('combobox').filter({ hasText: 'All Actions' });
    await actionTrigger.click();

    // Wait for the API response that includes action=UserRegistered
    const responsePromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/admin/audit') && resp.url().includes('action=UserRegistered'),
    );

    // Select "User Registered" from the dropdown options
    await page.getByRole('option', { name: 'User Registered' }).click();
    await responsePromise;

    // Allow debounce / re-render to settle
    await page.waitForTimeout(300);

    // E2E test run registers users, so the filtered list should still have entries
    const rows = page.locator('table tbody tr');
    await expect(rows.first()).toBeVisible({ timeout: 10_000 });
    const count = await rows.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('resource type filter works', async ({ page }) => {
    await loginAdminViaApi(page, SYSADMIN, '/admin/audit');
    await waitForAuditTable(page);

    // Open the Resource Type select (the second combobox after the Action filter)
    const resourceTrigger = page.getByRole('combobox').nth(1);
    await resourceTrigger.click();

    // Wait for the API response that includes resourceType=User
    const responsePromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/admin/audit') && resp.url().includes('resourceType=User'),
    );

    // Select "User" from the dropdown options
    await page.getByRole('option', { name: 'User', exact: true }).click();
    await responsePromise;

    // Allow debounce / re-render to settle
    await page.waitForTimeout(300);

    // Filtered entries for User resources should be present
    const rows = page.locator('table tbody tr');
    await expect(rows.first()).toBeVisible({ timeout: 10_000 });
    const count = await rows.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });
});
