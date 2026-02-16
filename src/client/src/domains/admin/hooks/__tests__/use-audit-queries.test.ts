import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import { useAuditLog } from '../use-audit-queries';

// Mock the audit API module
vi.mock('../../api/audit.api', () => ({
  auditApi: {
    searchAuditLog: vi.fn().mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    }),
  },
}));

/** Global staleTime matching the app's QueryProvider (1 minute). */
const GLOBAL_STALE_TIME = 1000 * 60;

/**
 * Creates a wrapper with a 1-minute global staleTime — matching the app's
 * QueryProvider — so that the test can verify the hook overrides it.
 */
function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: GLOBAL_STALE_TIME,
      },
    },
  });
  return {
    queryClient,
    Wrapper({ children }: { children: ReactNode }) {
      return createElement(QueryClientProvider, { client: queryClient }, children);
    },
  };
}

describe('useAuditLog', () => {
  it('should override global staleTime so audit data refetches on navigation', async () => {
    const { queryClient, Wrapper } = createWrapper();

    const { result } = renderHook(() => useAuditLog(), { wrapper: Wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // The observer's staleTime should be lower than the global default.
    // Audit logs are near-real-time event streams and must not stay cached
    // for the full 1-minute global staleTime.
    const observer = queryClient.getQueryCache().findAll()[0];
    expect(observer).toBeDefined();

    const observerStaleTime = observer!.observers[0]?.options.staleTime;
    expect(observerStaleTime).toBeDefined();
    expect(observerStaleTime).toBeLessThan(GLOBAL_STALE_TIME);
  });
});
