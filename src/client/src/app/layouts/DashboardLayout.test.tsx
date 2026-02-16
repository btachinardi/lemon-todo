import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { DashboardLayout } from './DashboardLayout';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

// Mock sonner to avoid layout complexity
vi.mock('sonner', () => ({
  Toaster: () => null,
}));

// Mock UserMenu to avoid auth mutation hooks
vi.mock('@/domains/auth/components/UserMenu', () => ({
  UserMenu: () => <div data-testid="user-menu" />,
}));

// Mock DevAccountSwitcher to avoid auth API dependencies
vi.mock('@/domains/auth/components/DevAccountSwitcher', () => ({
  DevAccountSwitcher: () => <div data-testid="dev-account-switcher" />,
}));

// Mock OnboardingTour to avoid TanStack Query dependency
vi.mock('@/domains/onboarding/components/widgets/OnboardingTour', () => ({
  OnboardingTour: () => null,
}));

describe('DashboardLayout', () => {
  beforeEach(() => {
    useAuthStore.setState({
      accessToken: null,
      user: null,
      isAuthenticated: false,
    });
  });

  it('should hide admin link when user has no admin role', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'user@test.com', displayName: 'User', roles: ['User'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter>
        <DashboardLayout>
          <p>Page content</p>
        </DashboardLayout>
      </MemoryRouter>,
    );

    expect(screen.queryByText('Admin')).not.toBeInTheDocument();
  });

  it('should show admin link when user has Admin role', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'admin@test.com', displayName: 'Admin', roles: ['User', 'Admin'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter>
        <DashboardLayout>
          <p>Page content</p>
        </DashboardLayout>
      </MemoryRouter>,
    );

    expect(screen.getByText('Admin')).toBeInTheDocument();
  });

  it('should show admin link when user has SystemAdmin role', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'sysadmin@test.com', displayName: 'SysAdmin', roles: ['User', 'SystemAdmin'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter>
        <DashboardLayout>
          <p>Page content</p>
        </DashboardLayout>
      </MemoryRouter>,
    );

    expect(screen.getByText('Admin')).toBeInTheDocument();
  });

  it('should render dev account switcher trigger in development mode', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'user@test.com', displayName: 'User', roles: ['User'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter>
        <DashboardLayout>
          <p>Page content</p>
        </DashboardLayout>
      </MemoryRouter>,
    );

    expect(screen.getByText('Dev')).toBeInTheDocument();
  });

  it('should hide dev account switcher trigger in production mode', () => {
    vi.stubEnv('DEV', false);

    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'user@test.com', displayName: 'User', roles: ['User'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter>
        <DashboardLayout>
          <p>Page content</p>
        </DashboardLayout>
      </MemoryRouter>,
    );

    expect(screen.queryByText('Dev')).not.toBeInTheDocument();
    expect(screen.queryByTestId('dev-account-switcher')).not.toBeInTheDocument();

    vi.unstubAllEnvs();
  });
});
