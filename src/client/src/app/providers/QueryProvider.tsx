import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEffect, useState, type ReactNode } from 'react';
import { captureError } from '@/lib/error-logger';

/** Props for {@link QueryProvider}. */
interface QueryProviderProps {
  children: ReactNode;
}

/**
 * App-level TanStack Query provider. Configures 1-minute stale time,
 * single retry, disables refetch-on-window-focus, and installs a global
 * mutation error handler as a safety net for forgotten onError callbacks.
 */
export function QueryProvider({ children }: QueryProviderProps) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 1000 * 60,
            gcTime: 1000 * 60 * 10, // 10 minutes — longer offline access
            retry: 1,
            refetchOnWindowFocus: false,
            networkMode: 'offlineFirst',
          },
        },
        queryCache: new QueryCache({
          onError: (error, query) => {
            // Only capture background refetch errors (not initial loads, which are handled by UI)
            if (query.state.data !== undefined) {
              captureError(error, {
                source: 'QueryCache',
                metadata: { queryKey: query.queryKey },
              });
            }
          },
        }),
        mutationCache: new MutationCache({
          onError: (error, _variables, _context, mutation) => {
            // Only capture if no component-level onError was provided
            if (!mutation.options.onError) {
              captureError(error, {
                source: 'MutationCache',
                metadata: { mutationKey: mutation.options.mutationKey },
              });
            }
          },
        }),
      }),
  );

  // Invalidate all query caches when the offline queue finishes draining.
  // This ensures the UI reflects server state after queued mutations are replayed.
  useEffect(() => {
    const handler = () => {
      queryClient.invalidateQueries();
    };
    window.addEventListener('offline-queue-drained', handler);
    return () => window.removeEventListener('offline-queue-drained', handler);
  }, [queryClient]);

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}
