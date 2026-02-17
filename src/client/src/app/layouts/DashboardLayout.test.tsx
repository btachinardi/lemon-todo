import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { DashboardLayout } from './DashboardLayout';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

const { onboardingState, mockDemoEnabled, mockGetActiveDevAccount } = vi.hoisted(() => ({
  onboardingState: { completed: false, completedAt: null as string | null },
  mockDemoEnabled: { data: true, isLoading: false },
  mockGetActiveDevAccount: vi.fn<(...args: unknown[]) => unknown>(),
}));

// Mock sonner — capture props for Toaster configuration assertions
vi.mock('sonner', () => ({
  Toaster: (props: Record<string, unknown>) => (
    <div data-testid="sonner-toaster" {...Object.fromEntries(
      Object.entries(props).map(([k, v]) => [`data-${k.toLowerCase()}`, typeof v === 'object' ? JSON.stringify(v) : String(v)])
    )} />
  ),
}));

// Mock UserMenu to avoid auth mutation hooks — captures variant prop
vi.mock('@/domains/auth/components/UserMenu', () => ({
  UserMenu: (props: Record<string, unknown>) => (
    <div data-testid="user-menu" data-variant={props.variant ?? undefined} />
  ),
}));

// Mock DevAccountSwitcher to avoid auth API dependencies
vi.mock('@/domains/auth/components/DevAccountSwitcher', () => ({
  DevAccountSwitcher: () => <div data-testid="dev-account-switcher" />,
  getActiveDevAccount: (...args: unknown[]) => mockGetActiveDevAccount(...args),
  DEV_ACCOUNTS: [],
}));

// Mock the config hook
vi.mock('@/domains/config/hooks/use-config', () => ({
  useDemoAccountsEnabled: () => mockDemoEnabled,
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

// Mock NotificationDropdown to avoid TanStack Query dependency — captures props
vi.mock('@/domains/notifications/components/widgets/NotificationDropdown', () => ({
  NotificationDropdown: (props: Record<string, unknown>) => (
    <div data-testid="notification-dropdown" data-show-label={props.showLabel ? 'true' : undefined} />
  ),
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
    // Default: demo accounts enabled, no active dev account
    mockDemoEnabled.data = true;
    mockDemoEnabled.isLoading = false;
    mockGetActiveDevAccount.mockReturnValue(undefined);
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

  it('should show "Switch demo account" trigger when no dev account is active', () => {
    mockGetActiveDevAccount.mockReturnValue(undefined);
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

    expect(screen.getByText(/switch demo account/i)).toBeInTheDocument();
    expect(screen.queryByText('Dev')).not.toBeInTheDocument();
  });

  it('should show active dev account role in trigger when logged in as dev account', () => {
    mockGetActiveDevAccount.mockReturnValue({
      email: 'dev.admin@lemondo.dev',
      roleKey: 'admin',
      labelKey: 'auth.devSwitcher.roles.admin',
      descKey: 'auth.devSwitcher.roles.adminDesc',
      icon: () => null,
      accent: 'text-amber-800',
    });
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'dev.admin@lemondo.dev', displayName: 'Admin', roles: ['User', 'Admin'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter>
        <DashboardLayout>
          <p>Page content</p>
        </DashboardLayout>
      </MemoryRouter>,
    );

    // Should show the role label, not "Dev" or "Switch demo account"
    const trigger = screen.getByRole('button', { name: /admin/i });
    expect(trigger).toBeInTheDocument();
    expect(screen.queryByText('Dev')).not.toBeInTheDocument();
  });

  it('should use ghost variant for dev trigger button', () => {
    mockGetActiveDevAccount.mockReturnValue(undefined);
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

    const trigger = screen.getByText(/switch demo account/i).closest('button')!;
    // Ghost buttons should NOT have dashed border styling
    expect(trigger.className).not.toMatch(/border-dashed/);
  });

  it('should position dev trigger above mobile quick-add bar', () => {
    mockGetActiveDevAccount.mockReturnValue(undefined);
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

    const trigger = screen.getByText(/switch demo account/i);
    const devContainer = trigger.closest('div.fixed');
    expect(devContainer).not.toBeNull();
    expect(devContainer!.className).toMatch(/bottom-1[4-6]/);
    expect(devContainer!.className).toMatch(/sm:bottom-3/);
  });

  it('should hide dev account switcher trigger when demo accounts are disabled', () => {
    mockDemoEnabled.data = false;
    mockGetActiveDevAccount.mockReturnValue(undefined);

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

    expect(screen.queryByText(/switch demo account/i)).not.toBeInTheDocument();
    expect(screen.queryByTestId('dev-account-switcher')).not.toBeInTheDocument();
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

  it('should configure toaster with mobile offset to clear the bottom quick-add bar', () => {
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

    const toaster = screen.getByTestId('sonner-toaster');
    expect(toaster).toHaveAttribute('data-mobileoffset', JSON.stringify({ bottom: '4.5rem' }));
  });

  it('should configure toaster with a close button for manual dismissal', () => {
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

    const toaster = screen.getByTestId('sonner-toaster');
    expect(toaster).toHaveAttribute('data-closebutton', 'true');
  });

  it('should render main as a flex column container for child height propagation', () => {
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

    const main = screen.getByRole('main');
    expect(main.className).toContain('flex');
    expect(main.className).toContain('flex-col');
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

  it('should pass showLabel to notification dropdown in mobile menu', async () => {
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
    const notifDropdown = within(dialog).getByTestId('notification-dropdown');
    expect(notifDropdown).toHaveAttribute('data-show-label', 'true');
  });

  it('should pass variant="inline" to UserMenu in mobile menu', async () => {
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
    const userMenu = within(dialog).getByTestId('user-menu');
    expect(userMenu).toHaveAttribute('data-variant', 'inline');
  });


});
