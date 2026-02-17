import { test, expect } from '@playwright/test';
import {
  SYSADMIN,
  ADMIN,
  DEV_USER,
  loginAdminViaApi,
} from '../helpers/admin.helpers';
import { loginViaApi } from '../helpers/auth.helpers';
import { API_BASE } from '../helpers/e2e.config';

/**
 * E2E: Admin Route Guard.
 *
 * Tests the `AdminRoute` component that protects `/admin/*` routes:
 * - Unauthenticated users are redirected to `/login`
 * - Authenticated regular users are redirected to `/board`
 * - Admin and SystemAdmin users can access admin pages
 */

test.describe('Admin Route Guard', () => {
  test('unauthenticated user is redirected to login when visiting /admin/users', async ({ page }) => {
    await page.goto('/admin/users');
    await expect(page).toHaveURL(/\/login/);
  });

  test('unauthenticated user is redirected to login when visiting /admin/audit', async ({ page }) => {
    await page.goto('/admin/audit');
    await expect(page).toHaveURL(/\/login/);
  });

  test('regular user is redirected to /board when visiting /admin/users', async ({ page }) => {
    // Log in as the dev-seeded regular user by manually injecting the refresh cookie
    const res = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: DEV_USER.email, password: DEV_USER.password }),
    });
    if (!res.ok) throw new Error(`DEV_USER login failed: ${res.status} ${await res.text()}`);

    const setCookieHeader = res.headers.get('set-cookie');
    const match = setCookieHeader?.match(/refresh_token=([^;]+)/);
    const refreshToken = match![1];

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

    // Navigate and wait for silent refresh to complete
    const refreshPromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/auth/refresh'),
    );
    await page.goto('/admin/users');
    await refreshPromise;

    // Regular user should be redirected to /board
    await expect(page).toHaveURL(/\/board/);
  });

  test('admin user can access /admin/users', async ({ page }) => {
    await loginAdminViaApi(page, ADMIN);
    await expect(page.getByText('User Management')).toBeVisible({ timeout: 10_000 });
  });

  test('system admin can access /admin/audit', async ({ page }) => {
    await loginAdminViaApi(page, SYSADMIN, '/admin/audit');
    await expect(page.getByRole('heading', { name: 'Audit Log' })).toBeVisible({ timeout: 10_000 });
  });
});
