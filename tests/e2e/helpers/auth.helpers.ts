import type { Page } from '@playwright/test';

const API_BASE = 'http://localhost:5155/api';

export const E2E_USER = {
  email: 'e2e@lemondo.dev',
  password: 'E2eTestPass123!',
  displayName: 'E2E User',
};

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: { id: string; email: string; displayName: string };
}

let cachedToken: string | null = null;

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

  const data = (await res.json()) as AuthResponse;
  cachedToken = data.accessToken;
  return cachedToken;
}

/** Resets the cached token so the next call to getAuthToken() re-authenticates. */
export function clearCachedToken(): void {
  cachedToken = null;
}

/**
 * Injects the auth store into the page's localStorage so the React app
 * considers the user authenticated without going through the login form.
 */
export async function loginViaStorage(page: Page): Promise<void> {
  const token = await getAuthToken();

  // Login again to get full user profile for the store
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: E2E_USER.email, password: E2E_USER.password }),
  });
  const data = (await res.json()) as AuthResponse;

  const authState = {
    state: {
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      user: data.user,
      isAuthenticated: true,
    },
    version: 0,
  };

  // Navigate to a blank page first to set localStorage on the correct origin
  await page.goto('/login');
  await page.evaluate((state) => {
    localStorage.setItem('lemondo-auth', JSON.stringify(state));
  }, authState);
}
