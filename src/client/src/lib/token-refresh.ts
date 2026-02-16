/**
 * Automatic token refresh utility.
 *
 * Uses a singleton promise to deduplicate concurrent refresh attempts.
 * Refresh token is sent automatically via HttpOnly cookie (credentials: 'include').
 * On success, updates the in-memory Zustand auth store.
 */
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';
import { API_BASE_URL } from './api-client';

/**
 * Response from the token refresh endpoint.
 * Contains the new access token and refreshed user profile.
 */
interface RefreshResponse {
  accessToken: string;
  user: { id: string; email: string; displayName: string; roles: string[] };
}

/** In-flight refresh promise — prevents multiple concurrent refresh calls. */
let refreshPromise: Promise<RefreshResponse | null> | null = null;

/**
 * Attempts to refresh the access token using the HttpOnly cookie.
 * Returns the new auth response on success, or null on failure (clears auth state).
 * Deduplicates concurrent calls via a shared promise.
 *
 * @returns The refresh response with new token and user on success, null if refresh failed (user redirected to /login).
 */
export function attemptTokenRefresh(): Promise<RefreshResponse | null> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = doRefresh().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}

async function doRefresh(): Promise<RefreshResponse | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
    });

    if (!response.ok) {
      clearAuthAndRedirect();
      return null;
    }

    const data = (await response.json()) as RefreshResponse;
    useAuthStore.getState().setAuth(data.accessToken, data.user);
    return data;
  } catch {
    clearAuthAndRedirect();
    return null;
  }
}

/**
 * Clears authentication state and redirects to the login page.
 * Side effects: Clears Zustand auth store, sets window.location.href to '/login'.
 */
function clearAuthAndRedirect(): void {
  useAuthStore.getState().logout();
  window.location.href = '/login';
}
