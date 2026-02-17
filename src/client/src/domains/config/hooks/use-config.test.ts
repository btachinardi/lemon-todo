import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import { useDemoAccountsEnabled } from './use-config';

const mockGetConfig = vi.fn();
vi.mock('../api/config.api', () => ({
  configApi: {
    getConfig: () => mockGetConfig(),
  },
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

describe('useDemoAccountsEnabled', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should return true when API reports enableDemoAccounts=true', async () => {
    mockGetConfig.mockResolvedValue({ enableDemoAccounts: true });

    const { result } = renderHook(() => useDemoAccountsEnabled(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.data).toBe(true);
    });
  });

  it('should return false when API reports enableDemoAccounts=false', async () => {
    mockGetConfig.mockResolvedValue({ enableDemoAccounts: false });

    const { result } = renderHook(() => useDemoAccountsEnabled(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.data).toBe(false);
    });
  });

  it('should default to false while loading', () => {
    mockGetConfig.mockReturnValue(new Promise(() => {})); // Never resolves

    const { result } = renderHook(() => useDemoAccountsEnabled(), { wrapper: createWrapper() });

    // While loading, data should be undefined (falsy)
    expect(result.current.data).toBeUndefined();
    expect(result.current.isLoading).toBe(true);
  });
});
