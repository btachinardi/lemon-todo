/**
 * Admin E2E helpers for user management, role assignment, and audit log testing.
 *
 * Provides two categories of helpers:
 * - **Headless API helpers** — call admin endpoints directly (no browser needed).
 *   Require a prior {@link loginAsAdmin} call to store the admin token.
 * - **Browser helpers** — drive Playwright pages for full admin UI E2E tests.
 *   Require a Playwright `Page` object.
 *
 * The admin token is stored at module level and shared across all API helpers
 * in this file. Calling {@link loginAsAdmin} or {@link loginAdminViaApi}
 * overwrites any previously stored token.
 *
 * @module
 */

import type { Page } from '@playwright/test';
import { API_BASE } from './e2e.config';

// ---------------------------------------------------------------------------
// Dev-seeded account credentials (must match Program.cs seed data)
// ---------------------------------------------------------------------------

/**
 * SystemAdmin dev-seeded account. Has full admin access: can assign/remove
 * roles, deactivate/reactivate users, and access all admin endpoints.
 * Use this (the default) for most admin test setups.
 */
export const SYSADMIN = {
  email: 'dev.sysadmin@lemondo.dev',
  password: 'SysAdmin1234',
  displayName: 'Dev SysAdmin',
} as const;

/**
 * Admin dev-seeded account. Can view the user list and audit log but
 * **cannot** assign roles or deactivate users (those require SystemAdmin).
 * Use for testing permission boundaries.
 */
export const ADMIN = {
  email: 'dev.admin@lemondo.dev',
  password: 'Admin1234',
  displayName: 'Dev Admin',
} as const;

/**
 * Regular user dev-seeded account with no admin access. Use for testing
 * that non-admin users are correctly denied access to admin endpoints/UI.
 */
export const DEV_USER = {
  email: 'dev.user@lemondo.dev',
  password: 'User1234',
  displayName: 'Dev User',
} as const;

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface AuthResponse {
  accessToken: string;
  user: { id: string; email: string; displayName: string };
}

/**
 * Shape returned by the admin users list endpoint.
 *
 * @remarks
 * `roles` contains role name strings (e.g. `['Admin']`, `['SystemAdmin']`).
 * `createdAt` is an ISO 8601 timestamp string.
 */
export interface AdminUserResponse {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

interface AuditEntryResponse {
  id: string;
  timestamp: string;
  actorId: string | null;
  action: string;
  resourceType: string;
  resourceId: string | null;
  details: string | null;
  ipAddress: string | null;
}

// ---------------------------------------------------------------------------
// Token management (per-module, isolated from auth.helpers.ts)
// ---------------------------------------------------------------------------

let adminToken: string | null = null;

/** Extracts the refresh_token value from a response's Set-Cookie header. */
function extractRefreshToken(res: Response): string {
  const setCookieHeader = res.headers.get('set-cookie');
  if (!setCookieHeader) throw new Error('Response missing Set-Cookie header');
  const match = setCookieHeader.match(/refresh_token=([^;]+)/);
  if (!match) throw new Error(`No refresh_token in Set-Cookie: ${setCookieHeader}`);
  return match[1];
}

// ---------------------------------------------------------------------------
// API helpers (headless, no browser needed)
// ---------------------------------------------------------------------------

/**
 * Logs in as a dev-seeded admin account and stores the access token for
 * subsequent headless API helpers in this file (`listAdminUsers`,
 * `assignRole`, etc.).
 *
 * Defaults to {@link SYSADMIN} because most admin operations (role
 * assignment, deactivation) require SystemAdmin privileges.
 *
 * @param account - Credentials to log in with. Defaults to `SYSADMIN`.
 * @throws If login fails (non-2xx response).
 *
 * @example
 * test.beforeAll(async () => {
 *   await loginAsAdmin();           // SystemAdmin by default
 *   const user = await registerTestUser('Target User');
 *   await assignRole(user.id, 'Admin');
 * });
 */
export async function loginAsAdmin(
  account: { email: string; password: string } = SYSADMIN,
): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: account.email, password: account.password }),
  });
  if (!res.ok) throw new Error(`Admin login failed: ${res.status} ${await res.text()}`);
  const data = (await res.json()) as AuthResponse;
  adminToken = data.accessToken;
  return data;
}

/** Builds Authorization headers using the stored admin token. */
function adminHeaders(): Record<string, string> {
  if (!adminToken) throw new Error('No admin token — call loginAsAdmin() first');
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${adminToken}`,
  };
}

/**
 * Registers a unique test user via API for use as a target in admin tests.
 * Returns the user's `id`, `email`, and `displayName` for subsequent
 * operations like {@link assignRole} or {@link deactivateUser}.
 *
 * Does **not** store a token — the new user is a regular User with no admin
 * access. To use API helpers as this user, you would need a separate login.
 *
 * @param displayName - Display name for the user. Use a unique, descriptive
 *   name (e.g. `'SearchTarget User'`) when your test searches the user list.
 * @throws If registration fails (non-2xx response).
 */
export async function registerTestUser(
  displayName = 'Admin E2E Target',
): Promise<{ id: string; email: string; displayName: string }> {
  const email = `admin-e2e-${Date.now()}-${Math.random().toString(36).slice(2, 6)}@lemondo.dev`;
  const res = await fetch(`${API_BASE}/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password: 'TestPass123!', displayName }),
  });
  if (!res.ok) throw new Error(`Register failed: ${res.status} ${await res.text()}`);
  const data = (await res.json()) as AuthResponse;
  return { id: data.user.id, email, displayName };
}

/**
 * Fetches the admin user list with optional filters. Requires an Admin or
 * SystemAdmin token via {@link loginAsAdmin}.
 *
 * @param params.search - Filter by email or display name substring.
 * @param params.role - Filter by role name (e.g. `'Admin'`, `'SystemAdmin'`).
 * @param params.page - Page number (1-indexed).
 * @param params.pageSize - Items per page.
 * @throws If the API returns a non-2xx response.
 */
export async function listAdminUsers(
  params?: { search?: string; role?: string; page?: number; pageSize?: number },
): Promise<PagedResponse<AdminUserResponse>> {
  const query = new URLSearchParams();
  if (params?.search) query.set('search', params.search);
  if (params?.role) query.set('role', params.role);
  if (params?.page) query.set('page', String(params.page));
  if (params?.pageSize) query.set('pageSize', String(params.pageSize));
  const qs = query.toString();
  const res = await fetch(`${API_BASE}/admin/users${qs ? `?${qs}` : ''}`, {
    headers: adminHeaders(),
  });
  if (!res.ok) throw new Error(`List admin users failed: ${res.status} ${await res.text()}`);
  return res.json();
}

/**
 * Assigns a role to a user. Requires a **SystemAdmin** token.
 *
 * @param roleName - Role to assign: `'Admin'` or `'SystemAdmin'`.
 * @throws If the API returns a non-2xx response (e.g. 403 if token is not SystemAdmin).
 */
export async function assignRole(userId: string, roleName: string): Promise<void> {
  const res = await fetch(`${API_BASE}/admin/users/${userId}/roles`, {
    method: 'POST',
    headers: adminHeaders(),
    body: JSON.stringify({ roleName }),
  });
  if (!res.ok) throw new Error(`Assign role failed: ${res.status} ${await res.text()}`);
}

/**
 * Removes a role from a user. Requires a **SystemAdmin** token.
 *
 * @param roleName - Role to remove: `'Admin'` or `'SystemAdmin'`.
 * @throws If the API returns a non-2xx response.
 */
export async function removeRole(userId: string, roleName: string): Promise<void> {
  const res = await fetch(`${API_BASE}/admin/users/${userId}/roles/${roleName}`, {
    method: 'DELETE',
    headers: adminHeaders(),
  });
  if (!res.ok) throw new Error(`Remove role failed: ${res.status} ${await res.text()}`);
}

/**
 * Deactivates a user account, preventing them from logging in. Requires a
 * **SystemAdmin** token.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function deactivateUser(userId: string): Promise<void> {
  const res = await fetch(`${API_BASE}/admin/users/${userId}/deactivate`, {
    method: 'POST',
    headers: adminHeaders(),
  });
  if (!res.ok) throw new Error(`Deactivate user failed: ${res.status} ${await res.text()}`);
}

/**
 * Reactivates a previously deactivated user account. Requires a
 * **SystemAdmin** token.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function reactivateUser(userId: string): Promise<void> {
  const res = await fetch(`${API_BASE}/admin/users/${userId}/reactivate`, {
    method: 'POST',
    headers: adminHeaders(),
  });
  if (!res.ok) throw new Error(`Reactivate user failed: ${res.status} ${await res.text()}`);
}

/**
 * Searches the audit log with optional filters. Requires an Admin or
 * SystemAdmin token via {@link loginAsAdmin}.
 *
 * @param params.action - Filter by action type (e.g. `'UserCreated'`).
 * @param params.resourceType - Filter by resource type (e.g. `'User'`).
 * @param params.actorId - Filter by the actor (user) who performed the action.
 * @param params.page - Page number (1-indexed).
 * @param params.pageSize - Items per page.
 * @throws If the API returns a non-2xx response.
 */
export async function searchAuditLog(
  params?: { action?: string; resourceType?: string; actorId?: string; page?: number; pageSize?: number },
): Promise<PagedResponse<AuditEntryResponse>> {
  const query = new URLSearchParams();
  if (params?.action) query.set('action', params.action);
  if (params?.resourceType) query.set('resourceType', params.resourceType);
  if (params?.actorId) query.set('actorId', params.actorId);
  if (params?.page) query.set('page', String(params.page));
  if (params?.pageSize) query.set('pageSize', String(params.pageSize));
  const qs = query.toString();
  const res = await fetch(`${API_BASE}/admin/audit${qs ? `?${qs}` : ''}`, {
    headers: adminHeaders(),
  });
  if (!res.ok) throw new Error(`Search audit log failed: ${res.status} ${await res.text()}`);
  return res.json();
}

// ---------------------------------------------------------------------------
// Browser helpers (Playwright page required)
// ---------------------------------------------------------------------------

/**
 * Logs into the browser as an admin account and navigates to an admin page.
 *
 * 1. Calls login API to get tokens
 * 2. Injects refresh cookie into Playwright context
 * 3. Navigates to `navigateTo` and waits for silent refresh
 * 4. Stores the admin token for headless API helpers
 *
 * Also overwrites the module-level `adminToken`, so headless helpers
 * (`listAdminUsers`, `assignRole`, etc.) operate as this account afterward.
 *
 * @param page - Playwright page to drive.
 * @param account - Credentials to log in with. Defaults to `SYSADMIN`.
 * @param navigateTo - Path to navigate after login. Must be an authenticated
 *   route that triggers AuthHydrationProvider's silent refresh.
 * @throws If login or silent refresh fails.
 *
 * @example
 * test.beforeAll(async ({ browser }) => {
 *   const context = await browser.newContext();
 *   const page = await context.newPage();
 *   await loginAdminViaApi(page, SYSADMIN);
 *   await waitForUsersTable(page);
 * });
 */
export async function loginAdminViaApi(
  page: Page,
  account: { email: string; password: string } = SYSADMIN,
  navigateTo = '/admin/users',
): Promise<void> {
  const loginRes = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: account.email, password: account.password }),
  });
  if (!loginRes.ok) throw new Error(`Admin login failed: ${loginRes.status} ${await loginRes.text()}`);

  const loginData = (await loginRes.json()) as AuthResponse;
  adminToken = loginData.accessToken;
  const refreshToken = extractRefreshToken(loginRes);

  // Inject refresh cookie
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

  // Navigate and wait for silent refresh
  const refreshPromise = page.waitForResponse(
    (resp) => resp.url().includes('/api/auth/refresh'),
  );
  await page.goto(navigateTo);
  const refreshResponse = await refreshPromise;

  if (!refreshResponse.ok()) {
    const body = await refreshResponse.text().catch(() => '(no body)');
    throw new Error(`Silent refresh failed: ${refreshResponse.status()} ${body}`);
  }
}

/**
 * Waits for the admin users table to fully load past the skeleton state.
 * Use after {@link loginAdminViaApi} to ensure the table has real rows
 * before interacting with user entries.
 *
 * @throws If the table does not appear within 10 seconds.
 */
export async function waitForUsersTable(page: Page): Promise<void> {
  await page.getByText('User Management').waitFor({ state: 'visible', timeout: 10_000 });
  // Wait for at least one real row (not skeleton)
  await page.locator('table tbody tr').first().waitFor({ state: 'visible', timeout: 10_000 });
}

/**
 * Waits for the audit log heading to appear, indicating the page has loaded.
 * Use after navigating to `/admin/audit` via {@link loginAdminViaApi}.
 *
 * @throws If the heading does not appear within 10 seconds.
 */
export async function waitForAuditTable(page: Page): Promise<void> {
  await page.getByRole('heading', { name: 'Audit Log' }).waitFor({ state: 'visible', timeout: 10_000 });
}
