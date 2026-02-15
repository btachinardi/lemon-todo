import { useEffect, useState, type ReactNode } from 'react';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

interface AuthHydrationProviderProps {
  children: ReactNode;
}

/**
 * Triggers Zustand auth store rehydration from localStorage after mount.
 * Renders nothing until hydration completes, preventing flash-of-wrong-state.
 *
 * Required because `skipHydration: true` is set on the auth store to avoid
 * React 19's "getSnapshot should be cached" infinite loop with useSyncExternalStore.
 */
export function AuthHydrationProvider({ children }: AuthHydrationProviderProps) {
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    const hydrate = async () => {
      await useAuthStore.persist.rehydrate();
      setHydrated(true);
    };
    hydrate();
  }, []);

  if (!hydrated) {
    return null;
  }

  return <>{children}</>;
}
