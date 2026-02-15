import type { Page } from '@playwright/test';

const API_BASE = 'http://localhost:5155/api';

export const E2E_USER = {
  email: 'e2e@lemondo.dev',
  password: 'E2eTestPass123!',
  displayName: 'E2E User',
};

interface AuthResponse {
  accessToken: string;
  user: { id: string; email: string; displayName: string };
}

let cachedToken: string | null = null;
let cachedCookie: string | null = null;

/** Registers the E2E test user (idempotent — ignores 409 Conflict). */
async function ensureRegistered(): Promise<void> {
  const res = await fetch(`${API_BASE}/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(E2E_USER),
  });

  // 200 = new user, 409 = already exists — both are fine
  if (!res.ok && res.status !== 409) {
    throw new Error(`Register failed: ${res.status} ${await res.text()}`);
  }
}

/** Logs in the E2E test user and returns the access token. Caches the result per test run. */
export async function getAuthToken(): Promise<string> {
  if (cachedToken) return cachedToken;

  await ensureRegistered();

  const res = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: E2E_USER.email, password: E2E_USER.password }),
  });

  if (!res.ok) throw new Error(`Login failed: ${res.status} ${await res.text()}`);

  // Extract refresh token cookie from Set-Cookie header
  const setCookieHeader = res.headers.get('set-cookie');
  if (setCookieHeader) {
    const match = setCookieHeader.match(/refresh_token=([^;]+)/);
    if (match) {
      cachedCookie = match[1];
    }
  }

  const data = (await res.json()) as AuthResponse;
  cachedToken = data.accessToken;
  return cachedToken;
}

/** Resets the cached token so the next call to getAuthToken() re-authenticates. */
export function clearCachedToken(): void {
  cachedToken = null;
  cachedCookie = null;
}

/**
 * Logs in via the API and injects the refresh cookie + access token into the page context.
 * The refresh cookie is set on the browser context so the React app can perform
 * silent refresh on page load via AuthHydrationProvider.
 */
export async function loginViaApi(page: Page): Promise<void> {
  const token = await getAuthToken();

  // Login again to get fresh tokens (cookie may have been consumed by previous refresh)
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: E2E_USER.email, password: E2E_USER.password }),
  });
  const data = (await res.json()) as AuthResponse;

  // Extract refresh cookie from login response
  const setCookieHeader = res.headers.get('set-cookie');
  let refreshTokenValue = '';
  if (setCookieHeader) {
    const match = setCookieHeader.match(/refresh_token=([^;]+)/);
    if (match) {
      refreshTokenValue = match[1];
    }
  }

  // Navigate to login page first (establishes the origin context)
  await page.goto('/login');

  // Inject the refresh token cookie into the browser context
  if (refreshTokenValue) {
    const context = page.context();
    await context.addCookies([
      {
        name: 'refresh_token',
        value: refreshTokenValue,
        domain: 'localhost',
        path: '/api/auth',
        httpOnly: true,
        sameSite: 'Strict',
      },
    ]);
  }

  // Navigate to the app — AuthHydrationProvider will do a silent refresh
  // which will set the access token in memory from the cookie
  await page.goto('/');
  // Wait for the hydration to complete (the app renders content)
  await page.waitForURL(/^(?!.*\/login).*$/);
}
