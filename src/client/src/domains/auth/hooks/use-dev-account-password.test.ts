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

  it('should return User1234 when logged in as the dev.user demo account (redacted email)', () => {
    useAuthStore.setState({
      user: { id: '1', email: 'd***@lemondo.dev', displayName: 'D***r', roles: ['User'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBe('User1234');
  });

  it('should return Admin1234 when logged in as the dev.admin demo account (redacted email)', () => {
    useAuthStore.setState({
      user: { id: '2', email: 'd***@lemondo.dev', displayName: 'D***n', roles: ['User', 'Admin'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBe('Admin1234');
  });

  it('should return SysAdmin1234 when logged in as the dev.sysadmin demo account (redacted email)', () => {
    useAuthStore.setState({
      user: { id: '3', email: 'd***@lemondo.dev', displayName: 'D***n', roles: ['User', 'Admin', 'SystemAdmin'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBe('SysAdmin1234');
  });

  it('should return null when user is not on the demo domain', () => {
    useAuthStore.setState({
      user: { id: '4', email: 'r***@example.com', displayName: 'Real User', roles: ['User'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBeNull();
  });

  it('should return null when a non-demo admin has matching roles', () => {
    useAuthStore.setState({
      user: { id: '5', email: 'a***@company.com', displayName: 'Admin', roles: ['User', 'Admin'] },
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
      user: { id: '1', email: 'd***@lemondo.dev', displayName: 'D***r', roles: ['User'] },
      isAuthenticated: true,
      accessToken: 'token',
    });

    const { result } = renderHook(() => useDevAccountPassword());
    expect(result.current).toBeNull();
  });
});
