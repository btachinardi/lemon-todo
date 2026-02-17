import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import { MemoryRouter } from 'react-router';
import { DevAccountSwitcher, getActiveDevAccount, DEV_ACCOUNTS } from './DevAccountSwitcher';
import { useAuthStore } from '../stores/use-auth-store';

// Mock authApi
const mockLogin = vi.fn();
const mockLogout = vi.fn();
vi.mock('../api/auth.api', () => ({
  authApi: {
    login: (...args: unknown[]) => mockLogin(...args),
    logout: (...args: unknown[]) => mockLogout(...args),
  },
}));

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router', async () => {
  const actual = await vi.importActual('react-router');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// Mock the config hook — default to enabled
const mockUseDemoAccountsEnabled = vi.fn();
vi.mock('@/domains/config/hooks/use-config', () => ({
  useDemoAccountsEnabled: () => mockUseDemoAccountsEnabled(),
}));

// Mock AppLoadingScreen to render a testable element
vi.mock('@/ui/feedback/AppLoadingScreen', () => ({
  AppLoadingScreen: ({ message }: { message?: string }) =>
    createElement('div', { role: 'status', 'aria-live': 'polite' }, message ?? 'Loading...'),
}));

function createWrapper(existingClient?: QueryClient) {
  const queryClient = existingClient ?? new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return createElement(
      QueryClientProvider,
      { client: queryClient },
      createElement(MemoryRouter, null, children),
    );
  };
}

describe('DevAccountSwitcher', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      accessToken: null,
      user: null,
      isAuthenticated: false,
    });
    mockLogin.mockResolvedValue({
      accessToken: 'test-token',
      user: { id: '1', email: 'd***@l***.dev', displayName: 'D** U***', roles: ['User'] },
    });
    mockLogout.mockResolvedValue(undefined);
    // Default: demo accounts enabled
    mockUseDemoAccountsEnabled.mockReturnValue({ data: true, isLoading: false });
  });

  it('should render three dev account cards when demo accounts are enabled', () => {
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    expect(screen.getByText('User')).toBeInTheDocument();
    expect(screen.getByText('Admin')).toBeInTheDocument();
    expect(screen.getByText('System Admin')).toBeInTheDocument();
  });

  it('should display role descriptions for each account', () => {
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    expect(screen.getByText(/task management/i)).toBeInTheDocument();
    expect(screen.getByText(/user management/i)).toBeInTheDocument();
    expect(screen.getByText(/full system access/i)).toBeInTheDocument();
  });

  it('should call login directly when not authenticated', async () => {
    const user = userEvent.setup();
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    await user.click(screen.getByText('User'));

    expect(mockLogout).not.toHaveBeenCalled();
    expect(mockLogin).toHaveBeenCalledWith({
      email: 'dev.user@lemondo.dev',
      password: 'User1234',
    });
  });

  it('should call logout then login when already authenticated', async () => {
    useAuthStore.setState({
      accessToken: 'existing-token',
      user: { id: '1', email: 'test@test.com', displayName: 'Test', roles: ['User'] },
      isAuthenticated: true,
    });
    const user = userEvent.setup();
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    await user.click(screen.getByText('Admin'));

    expect(mockLogout).toHaveBeenCalled();
    expect(mockLogin).toHaveBeenCalledWith({
      email: 'dev.admin@lemondo.dev',
      password: 'Admin1234',
    });
  });

  it('should update auth store and navigate on successful login', async () => {
    const user = userEvent.setup();
    const authResponse = {
      accessToken: 'new-token',
      user: { id: '2', email: 'd***@l***.dev', displayName: 'D** A***', roles: ['User', 'Admin'] },
    };
    mockLogin.mockResolvedValue(authResponse);

    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    await user.click(screen.getByText('Admin'));

    // Wait for async operations
    await vi.waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/board', { replace: true });
    });
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
    expect(useAuthStore.getState().accessToken).toBe('new-token');
  });

  it('should show loading state while switching accounts', async () => {
    // Make login hang to test loading state
    let resolveLogin: (value: unknown) => void;
    mockLogin.mockReturnValue(new Promise((resolve) => { resolveLogin = resolve; }));

    const user = userEvent.setup();
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    await user.click(screen.getByText('User'));

    // Should show some loading indicator
    expect(screen.getByText(/switching/i)).toBeInTheDocument();

    // Resolve to clean up
    resolveLogin!({
      accessToken: 'token',
      user: { id: '1', email: 'e', displayName: 'D', roles: ['User'] },
    });
  });

  it('should show the dev-only section header', () => {
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    expect(screen.getByText(/quick login/i)).toBeInTheDocument();
  });

  it('should have overflow-hidden on the button grid to prevent overflow in narrow containers', () => {
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    const buttons = screen.getAllByRole('button');
    const grid = buttons[0].parentElement!;

    expect(grid).toHaveClass('overflow-hidden');
  });

  it('should render nothing when demo accounts are disabled', () => {
    mockUseDemoAccountsEnabled.mockReturnValue({ data: false, isLoading: false });

    const { container } = render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    expect(container.innerHTML).toBe('');
    expect(screen.queryByText(/quick login/i)).not.toBeInTheDocument();
  });

  it('should render nothing while feature flag is loading', () => {
    mockUseDemoAccountsEnabled.mockReturnValue({ data: undefined, isLoading: true });

    const { container } = render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    expect(container.innerHTML).toBe('');
  });

  it('should mark the active dev account when user email matches', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'dev.admin@lemondo.dev', displayName: 'D** A***', roles: ['User', 'Admin'] },
      isAuthenticated: true,
    });

    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    // The active account button should have an "Active" indicator
    const adminButton = screen.getByText('Admin').closest('button')!;
    expect(adminButton).toHaveAttribute('aria-current', 'true');
  });

  it('should not mark any account as active when user is not a dev account', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'regular@example.com', displayName: 'Regular', roles: ['User'] },
      isAuthenticated: true,
    });

    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    const buttons = screen.getAllByRole('button');
    buttons.forEach((button) => {
      expect(button).not.toHaveAttribute('aria-current', 'true');
    });
  });

  it('should disable the active account button to prevent re-login', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'dev.user@lemondo.dev', displayName: 'D** U***', roles: ['User'] },
      isAuthenticated: true,
    });

    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    const userButton = screen.getByText('User').closest('button')!;
    expect(userButton).toBeDisabled();
  });

  it('should clear query cache when switching accounts while authenticated', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const resetSpy = vi.spyOn(queryClient, 'resetQueries');

    useAuthStore.setState({
      accessToken: 'existing-token',
      user: { id: '1', email: 'dev.user@lemondo.dev', displayName: 'D** U***', roles: ['User'] },
      isAuthenticated: true,
    });

    const user = userEvent.setup();
    render(<DevAccountSwitcher />, { wrapper: createWrapper(queryClient) });

    await user.click(screen.getByText('Admin'));

    await vi.waitFor(() => {
      expect(resetSpy).toHaveBeenCalled();
    });
  });

  it('should set auth token before clearing query cache to prevent stale refetches', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });

    const newToken = 'admin-token';
    const newUser = { id: '2', email: 'dev.admin@lemondo.dev', displayName: 'D** A***', roles: ['User', 'Admin'] };
    mockLogin.mockResolvedValue({ accessToken: newToken, user: newUser });

    // When queryClient.resetQueries() fires, the new token must already be
    // in the store. resetQueries() notifies mounted observers and triggers
    // refetches — those refetches must use the new user's credentials.
    let tokenAtResetTime: string | null = null;
    const originalReset = queryClient.resetQueries.bind(queryClient);
    vi.spyOn(queryClient, 'resetQueries').mockImplementation((...args) => {
      tokenAtResetTime = useAuthStore.getState().accessToken;
      return originalReset(...args);
    });

    useAuthStore.setState({
      accessToken: 'existing-token',
      user: { id: '1', email: 'dev.user@lemondo.dev', displayName: 'D** U***', roles: ['User'] },
      isAuthenticated: true,
    });

    const user = userEvent.setup();
    render(<DevAccountSwitcher />, { wrapper: createWrapper(queryClient) });

    await user.click(screen.getByText('Admin'));

    await vi.waitFor(() => {
      expect(tokenAtResetTime).not.toBeNull();
    });
    expect(tokenAtResetTime).toBe(newToken);
  });

  it('should keep isAuthenticated true during the switch to prevent login page flash', async () => {
    let resolveLogin: (value: unknown) => void;
    mockLogin.mockReturnValue(new Promise((resolve) => { resolveLogin = resolve; }));

    useAuthStore.setState({
      accessToken: 'existing-token',
      user: { id: '1', email: 'dev.user@lemondo.dev', displayName: 'D** U***', roles: ['User'] },
      isAuthenticated: true,
    });

    const user = userEvent.setup();
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    await user.click(screen.getByText('Admin'));

    // While the login is in-flight, isAuthenticated must remain true
    expect(useAuthStore.getState().isAuthenticated).toBe(true);

    // Resolve to clean up
    resolveLogin!({
      accessToken: 'new-token',
      user: { id: '2', email: 'd***@l***.dev', displayName: 'D** A***', roles: ['User', 'Admin'] },
    });
  });

  it('should render a full-screen overlay when switching from authenticated state', async () => {
    let resolveLogin: (value: unknown) => void;
    mockLogin.mockReturnValue(new Promise((resolve) => { resolveLogin = resolve; }));

    useAuthStore.setState({
      accessToken: 'existing-token',
      user: { id: '1', email: 'dev.user@lemondo.dev', displayName: 'D** U***', roles: ['User'] },
      isAuthenticated: true,
    });

    const user = userEvent.setup();
    render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    await user.click(screen.getByText('Admin'));

    // Should render a full-screen overlay with status role (same as AppLoadingScreen)
    expect(screen.getByRole('status')).toBeInTheDocument();

    // Resolve to clean up
    resolveLogin!({
      accessToken: 'new-token',
      user: { id: '2', email: 'd***@l***.dev', displayName: 'D** A***', roles: ['User', 'Admin'] },
    });
  });

  it('should call logout and clear cache when login fails during authenticated switch', async () => {
    mockLogin.mockRejectedValue(new Error('login failed'));

    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const resetSpy = vi.spyOn(queryClient, 'resetQueries');

    useAuthStore.setState({
      accessToken: 'existing-token',
      user: { id: '1', email: 'dev.user@lemondo.dev', displayName: 'D** U***', roles: ['User'] },
      isAuthenticated: true,
    });

    const user = userEvent.setup();
    render(<DevAccountSwitcher />, { wrapper: createWrapper(queryClient) });

    await user.click(screen.getByText('Admin'));

    await vi.waitFor(() => {
      // After failure, should clear auth state
      expect(useAuthStore.getState().isAuthenticated).toBe(false);
    });
    // And clear the query cache
    expect(resetSpy).toHaveBeenCalled();
  });
});

describe('getActiveDevAccount', () => {
  it('should return the matching dev account for a dev email', () => {
    const account = getActiveDevAccount('dev.admin@lemondo.dev');
    expect(account).toBeDefined();
    expect(account!.roleKey).toBe('admin');
  });

  it('should return undefined for a non-dev email', () => {
    const account = getActiveDevAccount('regular@example.com');
    expect(account).toBeUndefined();
  });

  it('should return undefined for null email', () => {
    const account = getActiveDevAccount(undefined);
    expect(account).toBeUndefined();
  });

  it('should match all three dev accounts', () => {
    for (const devAccount of DEV_ACCOUNTS) {
      const result = getActiveDevAccount(devAccount.email);
      expect(result).toBeDefined();
      expect(result!.roleKey).toBe(devAccount.roleKey);
    }
  });
});
