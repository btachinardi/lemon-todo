import type { Page } from '@playwright/test';
import { API_BASE } from './e2e.config';

// ---------------------------------------------------------------------------
// Dev-seeded account credentials (must match Program.cs seed data)
// ---------------------------------------------------------------------------

export const SYSADMIN = {
  email: 'dev.sysadmin@lemondo.dev',
  password: 'SysAdmin1234',
  displayName: 'Dev SysAdmin',
} as const;

export const ADMIN = {
  email: 'dev.admin@lemondo.dev',
  password: 'Admin1234',
  displayName: 'Dev Admin',
} as const;

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
 * Logs in as the given account via API and stores the access token for
 * subsequent admin API calls. Returns the login response data.
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
 * Registers a unique test user via API. Returns the created user's id + email.
 * Does NOT set the admin token — the new user is a regular User.
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

/** Lists admin users (requires admin token). */
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

/** Assigns a role to a user (requires SystemAdmin token). */
export async function assignRole(userId: string, roleName: string): Promise<void> {
  const res = await fetch(`${API_BASE}/admin/users/${userId}/roles`, {
    method: 'POST',
    headers: adminHeaders(),
    body: JSON.stringify({ roleName }),
  });
  if (!res.ok) throw new Error(`Assign role failed: ${res.status} ${await res.text()}`);
}

/** Removes a role from a user (requires SystemAdmin token). */
export async function removeRole(userId: string, roleName: string): Promise<void> {
  const res = await fetch(`${API_BASE}/admin/users/${userId}/roles/${roleName}`, {
    method: 'DELETE',
    headers: adminHeaders(),
  });
  if (!res.ok) throw new Error(`Remove role failed: ${res.status} ${await res.text()}`);
}

/** Deactivates a user account (requires SystemAdmin token). */
export async function deactivateUser(userId: string): Promise<void> {
  const res = await fetch(`${API_BASE}/admin/users/${userId}/deactivate`, {
    method: 'POST',
    headers: adminHeaders(),
  });
  if (!res.ok) throw new Error(`Deactivate user failed: ${res.status} ${await res.text()}`);
}

/** Reactivates a user account (requires SystemAdmin token). */
export async function reactivateUser(userId: string): Promise<void> {
  const res = await fetch(`${API_BASE}/admin/users/${userId}/reactivate`, {
    method: 'POST',
    headers: adminHeaders(),
  });
  if (!res.ok) throw new Error(`Reactivate user failed: ${res.status} ${await res.text()}`);
}

/** Searches the audit log (requires Admin+ token). */
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
 * Logs into the browser as the given admin account.
 *
 * 1. Calls login API to get tokens
 * 2. Injects refresh cookie into Playwright context
 * 3. Navigates to the target path and waits for silent refresh
 * 4. Waits for the admin UI to render
 *
 * Also stores the admin token for API helpers.
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
 * Waits for the admin users table to be visible and loaded (non-skeleton).
 */
export async function waitForUsersTable(page: Page): Promise<void> {
  await page.getByText('User Management').waitFor({ state: 'visible', timeout: 10_000 });
  // Wait for at least one real row (not skeleton)
  await page.locator('table tbody tr').first().waitFor({ state: 'visible', timeout: 10_000 });
}

/**
 * Waits for the audit log table to be visible and loaded.
 */
export async function waitForAuditTable(page: Page): Promise<void> {
  await page.getByRole('heading', { name: 'Audit Log' }).waitFor({ state: 'visible', timeout: 10_000 });
}
