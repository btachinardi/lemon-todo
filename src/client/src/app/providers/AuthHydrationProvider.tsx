import { useEffect, useState, type ReactNode } from 'react';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

interface AuthHydrationProviderProps {
  children: ReactNode;
}

/**
 * Performs a silent token refresh on mount to restore the session
 * from the HttpOnly refresh cookie.
 *
 * Renders nothing until the refresh attempt completes, preventing
 * flash-of-wrong-state. If the refresh fails (no cookie / expired),
 * the app renders in unauthenticated state and ProtectedRoute handles redirect.
 */
export function AuthHydrationProvider({ children }: AuthHydrationProviderProps) {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    const silentRefresh = async () => {
      try {
        const response = await fetch('/api/auth/refresh', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          signal: controller.signal,
        });

        if (response.ok) {
          const data = (await response.json()) as {
            accessToken: string;
            user: { id: string; email: string; displayName: string; roles: string[] };
          };
          useAuthStore.getState().setAuth(data.accessToken, data.user);
        }
      } catch (error: unknown) {
        // AbortError is expected on cleanup; all others mean no valid session
        if (error instanceof DOMException && error.name === 'AbortError') return;
      } finally {
        if (!controller.signal.aborted) {
          setReady(true);
        }
      }
    };
    silentRefresh();
    return () => controller.abort();
  }, []);

  if (!ready) {
    return null;
  }

  return <>{children}</>;
}
