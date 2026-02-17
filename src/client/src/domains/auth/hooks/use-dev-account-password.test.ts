import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useDevAccountPassword } from './use-dev-account-password';
import { useAuthStore } from '../stores/use-auth-store';

// Mock the config hook
const mockUseDemoAccountsEnabled = vi.fn();
vi.mock('@/domains/config/hooks/use-config', () => ({
  useDemoAccountsEnabled: () => mockUseDemoAccountsEnabled(),
}));

describe('useDevAccountPassword', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      accessToken: null,
      user: null,
      isAuthenticated: false,
    });
    // Default: demo accounts enabled
    mockUseDemoAccountsEnabled.mockReturnValue({ data: true, isLoading: false });
  });

  it('should return password when user is the dev.user demo account', () => {
    useAuthStore.setState({
      user: { id: '1', email: 'dev.user@lemondo.dev', displayName: 'Dev User', roles: ['User'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBe('User1234');
  });

  it('should return password when user is the dev.admin demo account', () => {
    useAuthStore.setState({
      user: { id: '2', email: 'dev.admin@lemondo.dev', displayName: 'Dev Admin', roles: ['Admin'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBe('Admin1234');
  });

  it('should return password when user is the dev.sysadmin demo account', () => {
    useAuthStore.setState({
      user: { id: '3', email: 'dev.sysadmin@lemondo.dev', displayName: 'Dev SysAdmin', roles: ['SystemAdmin'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBe('SysAdmin1234');
  });

  it('should return null when user is not a demo account', () => {
    useAuthStore.setState({
      user: { id: '4', email: 'real.user@example.com', displayName: 'Real User', roles: ['User'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBeNull();
  });

  it('should return null when no user is logged in', () => {
    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBeNull();
  });

  it('should return null when demo accounts feature flag is disabled', () => {
    mockUseDemoAccountsEnabled.mockReturnValue({ data: false, isLoading: false });

    useAuthStore.setState({
      user: { id: '1', email: 'dev.user@lemondo.dev', displayName: 'Dev User', roles: ['User'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBeNull();
  });
});
