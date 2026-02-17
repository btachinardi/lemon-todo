/**
 * Headless API helpers for controlling onboarding state in E2E tests.
 *
 * The most common use is calling {@link completeOnboarding} in `beforeAll`
 * to suppress the onboarding tour overlay so it doesn't interfere with
 * tests that are not specifically exercising the tour.
 *
 * All functions require an active auth session via {@link createTestUser}
 * or {@link loginViaApi}. Every function throws on non-2xx API responses.
 *
 * @module
 */

import { getAuthToken } from './auth.helpers';
import { API_BASE } from './e2e.config';

interface OnboardingStatusResponse {
  completed: boolean;
  /** ISO 8601 timestamp when onboarding was completed, or `null` if not yet done. */
  completedAt: string | null;
}

async function authHeaders(): Promise<Record<string, string>> {
  const token = await getAuthToken();
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  };
}

/**
 * Queries the server for the current user's onboarding completion state.
 * Use for asserting that a browser-driven tour completion was persisted.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function getOnboardingStatus(): Promise<OnboardingStatusResponse> {
  const res = await fetch(`${API_BASE}/onboarding/status`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Get onboarding status failed: ${res.status}`);
  return res.json();
}

/**
 * Marks onboarding as complete via API so the tour overlay does **not**
 * appear when navigating to `/board`. Call in `beforeAll` of any describe
 * block whose tests are not specifically testing the onboarding tour.
 *
 * This is irreversible within a test run — there is no reset endpoint.
 *
 * @throws If the API returns a non-2xx response.
 *
 * @example
 * test.beforeAll(async ({ browser }) => {
 *   const context = await browser.newContext();
 *   page = await context.newPage();
 *   await loginViaApi(page);
 *   await completeOnboarding(); // prevents tour overlay
 * });
 */
export async function completeOnboarding(): Promise<OnboardingStatusResponse> {
  const res = await fetch(`${API_BASE}/onboarding/complete`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Complete onboarding failed: ${res.status}`);
  return res.json();
}
