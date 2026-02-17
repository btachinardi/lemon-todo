import { useEffect, useRef, useState, type ReactNode } from 'react';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';
import { API_BASE_URL } from '@/lib/api-client';
import { AppLoadingScreen } from '@/ui/feedback/AppLoadingScreen';

/** Props for {@link AuthHydrationProvider}. */
interface AuthHydrationProviderProps {
  children: ReactNode;
}

/**
 * Performs a one-time silent token refresh from the HttpOnly refresh cookie.
 *
 * The refresh token rotation is NOT idempotent: each call revokes the old token
 * and issues a new one. React StrictMode double-fires effects (mount → unmount →
 * mount), which would send two refresh requests with the same token. The first
 * succeeds but the abort in cleanup discards the Set-Cookie response, so the
 * second request sends the now-revoked token and gets 401.
 *
 * Fix: store the refresh promise in a ref that survives StrictMode remounting.
 * The fetch fires exactly once, and both mount cycles share the same promise.
 */
async function performSilentRefresh(): Promise<void> {
  if (!navigator.onLine) return;

  try {
    const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
    });

    if (response.ok) {
      const data = (await response.json()) as {
        accessToken: string;
        user: { id: string; email: string; displayName: string; roles: string[] };
      };
      useAuthStore.getState().setAuth(data.accessToken, data.user);
    }
  } catch {
    // Network error or no valid session — continue in unauthenticated state
  }
}

/**
 * Performs a silent token refresh on mount to restore the session
 * from the HttpOnly refresh cookie.
 *
 * Shows an animated loading screen until the refresh attempt completes,
 * preventing flash-of-wrong-state. If the refresh fails (no cookie / expired),
 * the app renders in unauthenticated state and ProtectedRoute handles redirect.
 */
export function AuthHydrationProvider({ children }: AuthHydrationProviderProps) {
  const [ready, setReady] = useState(false);
  const refreshRef = useRef<Promise<void> | null>(null);

  useEffect(() => {
    // Re-use the in-flight refresh promise if StrictMode remounts the component.
    // This prevents a second fetch from sending the already-rotated token.
    if (!refreshRef.current) {
      refreshRef.current = performSilentRefresh();
    }

    let active = true;
    refreshRef.current.then(() => {
      if (active) setReady(true);
    });

    return () => {
      active = false;
    };
  }, []);

  if (!ready) {
    return <AppLoadingScreen />;
  }

  return <>{children}</>;
}
