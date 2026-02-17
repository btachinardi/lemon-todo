import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { DashboardLayout } from './DashboardLayout';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

const { onboardingState } = vi.hoisted(() => ({
  onboardingState: { completed: false, completedAt: null as string | null },
}));

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

// Mock useOnboardingStatus to control onboarding gate per test
vi.mock('@/domains/onboarding/hooks/use-onboarding', () => ({
  useOnboardingStatus: () => ({ data: onboardingState, isLoading: false }),
  useCompleteOnboarding: () => ({ mutate: vi.fn() }),
  onboardingKeys: { all: ['onboarding'], status: () => ['onboarding', 'status'] },
}));

// Mock NotificationDropdown to avoid TanStack Query dependency
vi.mock('@/domains/notifications/components/widgets/NotificationDropdown', () => ({
  NotificationDropdown: () => <div data-testid="notification-dropdown" />,
}));

// Mock SyncIndicator to avoid offline queue store dependency
vi.mock('@/ui/feedback/SyncIndicator', () => ({
  SyncIndicator: () => null,
}));

// Mock PWAInstallPrompt as a detectable stub (uses browser PWA APIs internally)
vi.mock('@/ui/feedback/PWAInstallPrompt', () => ({
  PWAInstallPrompt: () => <div data-testid="pwa-install-prompt" />,
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

  it('should position dev button above mobile quick-add bar', () => {
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

    const devButton = screen.getByText('Dev');
    const devContainer = devButton.closest('div.fixed');
    expect(devContainer).not.toBeNull();
    expect(devContainer!.className).toMatch(/bottom-1[4-6]/);
    expect(devContainer!.className).toMatch(/sm:bottom-3/);
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

  it('should not render install prompt when onboarding is not completed', () => {
    onboardingState.completed = false;
    onboardingState.completedAt = null;

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

    expect(screen.queryByTestId('pwa-install-prompt')).not.toBeInTheDocument();
  });

  it('should render install prompt when onboarding is completed', () => {
    onboardingState.completed = true;
    onboardingState.completedAt = '2024-01-01T00:00:00Z';

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

    expect(screen.getByTestId('pwa-install-prompt')).toBeInTheDocument();
  });

  it('should render a mobile menu toggle button in the header', () => {
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

    const header = screen.getByRole('banner');
    const menuButton = within(header).getByRole('button', { name: /menu/i });
    expect(menuButton).toBeInTheDocument();
  });

  it('should show tools in mobile menu when toggle is clicked', async () => {
    const user = userEvent.setup();
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

    const header = screen.getByRole('banner');
    const menuButton = within(header).getByRole('button', { name: /menu/i });
    await user.click(menuButton);

    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByTestId('notification-dropdown')).toBeInTheDocument();
    expect(within(dialog).getByTestId('user-menu')).toBeInTheDocument();
  });
});
