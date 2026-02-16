import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import { MemoryRouter } from 'react-router';
import { DevAccountSwitcher } from './DevAccountSwitcher';
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

function createWrapper() {
  const queryClient = new QueryClient({
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
  });

  it('should render three dev account cards', () => {
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

  it('should render nothing when not in development mode', () => {
    vi.stubEnv('DEV', false);

    const { container } = render(<DevAccountSwitcher />, { wrapper: createWrapper() });

    expect(container.innerHTML).toBe('');
    expect(screen.queryByText(/quick login/i)).not.toBeInTheDocument();

    vi.unstubAllEnvs();
  });
});
