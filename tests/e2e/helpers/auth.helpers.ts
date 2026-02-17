/**
 * Auth helpers for creating isolated test users and logging into the browser.
 *
 * Each E2E describe block should call one of these in `beforeAll` to get its
 * own user with a clean board. The stored token is shared with API helpers
 * in other files (e.g. `api.helpers.ts`, `notification.helpers.ts`) via
 * {@link getAuthToken}.
 *
 * @module
 */

import type { Page } from '@playwright/test';
import { API_BASE } from './e2e.config';

interface AuthResponse {
  accessToken: string;
  user: { id: string; email: string; displayName: string };
}

const E2E_PASSWORD = 'E2eTestPass123!';
let userCounter = 0;
let currentToken: string | null = null;

/** Generates a unique email for test isolation — each describe block gets its own user. */
function uniqueEmail(): string {
  return `e2e-${Date.now()}-${++userCounter}@lemondo.dev`;
}

/** Extracts the refresh_token value from a response's Set-Cookie header. Throws if missing. */
function extractRefreshToken(res: Response): string {
  const setCookieHeader = res.headers.get('set-cookie');
  if (!setCookieHeader) {
    throw new Error('Response missing Set-Cookie header');
  }

  const match = setCookieHeader.match(/refresh_token=([^;]+)/);
  if (!match) {
    throw new Error(`No refresh_token in Set-Cookie: ${setCookieHeader}`);
  }

  return match[1];
}

/**
 * Creates a unique test user via the register endpoint and stores the access
 * token for headless API helpers (`createTask`, `getDefaultBoard`, etc.).
 *
 * Use in `beforeAll` for describe blocks that only need API access (no browser).
 * Each call creates a fresh user with an empty board — no cleanup needed.
 *
 * @throws If registration fails (non-2xx response).
 *
 * @example
 * test.describe('card ordering', () => {
 *   test.beforeAll(async () => {
 *     await createTestUser();
 *     // API helpers are now authenticated as this user
 *     await createTask({ title: 'First task' });
 *   });
 * });
 */
export async function createTestUser(): Promise<void> {
  const email = uniqueEmail();
  const res = await fetch(`${API_BASE}/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password: E2E_PASSWORD, displayName: 'E2E User' }),
  });

  if (!res.ok) throw new Error(`Register failed: ${res.status} ${await res.text()}`);

  const data = (await res.json()) as AuthResponse;
  currentToken = data.accessToken;
}

/**
 * Returns the access token for the current test user. Used internally by all
 * API helper files to authenticate requests.
 *
 * @throws If no user has been created — call {@link createTestUser} or {@link loginViaApi} in `beforeAll` first.
 */
export async function getAuthToken(): Promise<string> {
  if (!currentToken) {
    throw new Error('No active auth session — call createTestUser() or loginViaApi() in beforeAll');
  }
  return currentToken;
}

/**
 * Creates a unique test user and logs into the browser for full E2E tests.
 *
 * 1. Registers a fresh user via API (unique email per call)
 * 2. Injects the refresh cookie into the Playwright browser context
 * 3. Navigates to `/board` — AuthHydrationProvider performs silent refresh
 * 4. Waits for the refresh response and verifies it succeeded
 * 5. Waits for the authenticated UI (nav bar) to render
 *
 * Also stores the access token for headless API helpers (`createTask`, etc.)
 * so they operate against the same user's data.
 *
 * **Side effect**: Navigates the page to `/board`. If your test needs a
 * different starting URL, navigate after this call returns.
 *
 * @throws If registration or silent refresh fails.
 *
 * @example
 * test.describe('task management', () => {
 *   let page: Page;
 *   test.beforeAll(async ({ browser }) => {
 *     const context = await browser.newContext();
 *     page = await context.newPage();
 *     await loginViaApi(page);
 *     // Browser is now authenticated and showing /board
 *   });
 * });
 */
export async function loginViaApi(page: Page): Promise<void> {
  const email = uniqueEmail();
  const res = await fetch(`${API_BASE}/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password: E2E_PASSWORD, displayName: 'E2E User' }),
  });

  if (!res.ok) throw new Error(`Register failed: ${res.status} ${await res.text()}`);

  const data = (await res.json()) as AuthResponse;
  currentToken = data.accessToken;
  const refreshToken = extractRefreshToken(res);

  // Inject refresh token cookie into browser context
  // (addCookies works at context level — no page navigation required first)
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

  // Navigate to the app and verify the silent refresh succeeds
  const refreshPromise = page.waitForResponse(
    (resp) => resp.url().includes('/api/auth/refresh'),
  );
  await page.goto('/board');
  const refreshResponse = await refreshPromise;

  if (!refreshResponse.ok()) {
    const body = await refreshResponse.text().catch(() => '(no body)');
    throw new Error(
      `Silent refresh failed: ${refreshResponse.status()} ${body}`,
    );
  }

  // Wait for the authenticated layout to render (nav only appears when logged in)
  await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });
}
