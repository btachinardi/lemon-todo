import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import { useLogin, useRegister, useLogout } from './use-auth-mutations';
import { useAuthStore } from '../stores/use-auth-store';

// Mock authApi
const mockLogin = vi.fn();
const mockRegister = vi.fn();
const mockLogout = vi.fn();
vi.mock('../api/auth.api', () => ({
  authApi: {
    login: (...args: unknown[]) => mockLogin(...args),
    register: (...args: unknown[]) => mockRegister(...args),
    logout: (...args: unknown[]) => mockLogout(...args),
  },
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return {
    queryClient,
    wrapper: ({ children }: { children: ReactNode }) =>
      createElement(QueryClientProvider, { client: queryClient }, children),
  };
}

const AUTH_RESPONSE = {
  accessToken: 'new-token',
  user: { id: '1', email: 'test@test.com', displayName: 'Test User', roles: ['User'] as string[] },
};

describe('useLogin', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ accessToken: null, user: null, isAuthenticated: false });
  });

  it('should clear query cache on successful login', async () => {
    mockLogin.mockResolvedValue(AUTH_RESPONSE);
    const { queryClient, wrapper } = createWrapper();
    const clearSpy = vi.spyOn(queryClient, 'clear');

    // Seed the cache with stale data from a previous session
    queryClient.setQueryData(['tasks', 'list', undefined], { items: [{ id: 'stale' }] });

    const { result } = renderHook(() => useLogin(), { wrapper });

    result.current.mutate({ email: 'test@test.com', password: 'pass' });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(clearSpy).toHaveBeenCalled();
    expect(useAuthStore.getState().accessToken).toBe('new-token');
  });
});

describe('useRegister', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ accessToken: null, user: null, isAuthenticated: false });
  });

  it('should clear query cache on successful registration', async () => {
    mockRegister.mockResolvedValue(AUTH_RESPONSE);
    const { queryClient, wrapper } = createWrapper();
    const clearSpy = vi.spyOn(queryClient, 'clear');

    // Seed the cache with stale data
    queryClient.setQueryData(['boards', 'default'], { id: 'stale-board' });

    const { result } = renderHook(() => useRegister(), { wrapper });

    result.current.mutate({
      email: 'new@test.com',
      password: 'Password1',
      displayName: 'New User',
    });

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(clearSpy).toHaveBeenCalled();
    expect(useAuthStore.getState().accessToken).toBe('new-token');
  });
});

describe('useLogout', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      accessToken: 'token',
      user: AUTH_RESPONSE.user,
      isAuthenticated: true,
    });
  });

  it('should clear auth store and query cache on logout', async () => {
    mockLogout.mockResolvedValue(undefined);
    const { queryClient, wrapper } = createWrapper();
    const clearSpy = vi.spyOn(queryClient, 'clear');

    const { result } = renderHook(() => useLogout(), { wrapper });

    result.current.mutate();

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true);
    });

    expect(clearSpy).toHaveBeenCalled();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });
});
