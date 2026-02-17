import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import { UserMenu } from './UserMenu';
import { useAuthStore } from '../stores/use-auth-store';

// Mock auth mutations
vi.mock('../hooks/use-auth-mutations', () => ({
  useLogout: vi.fn().mockReturnValue({ mutate: vi.fn(), isPending: false }),
}));

// Mock reveal hook — use inline function to avoid hoisting issues
vi.mock('../hooks/use-reveal-own-profile', () => ({
  useRevealOwnProfile: vi.fn().mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
    error: null,
    data: null,
    reset: vi.fn(),
  }),
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

describe('UserMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      accessToken: 'test-token',
      user: {
        id: '1',
        email: 't***@lemondo.dev',
        displayName: 'T***r',
        roles: ['User'],
      },
      isAuthenticated: true,
    });
  });

  it('should display redacted user data', () => {
    render(<UserMenu />, { wrapper: createWrapper() });

    // The redacted display name should appear (in the trigger button and dropdown)
    expect(screen.getByText('T***r')).toBeInTheDocument();
  });

  it('should show security popover when clicking redacted info', async () => {
    const user = userEvent.setup();
    render(<UserMenu />, { wrapper: createWrapper() });

    // Open the dropdown menu first
    await user.click(screen.getByRole('button', { name: /t\*\*\*r/i }));

    // Click the redacted email/name area to trigger the security popover
    const redactedEmail = screen.getByText('t***@lemondo.dev');
    await user.click(redactedEmail);

    // Should show the security explanation popover
    expect(screen.getByText(/secure/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /reveal my data/i })).toBeInTheDocument();
  });

  it('should open reveal dialog when clicking reveal button in popover', async () => {
    const user = userEvent.setup();
    render(<UserMenu />, { wrapper: createWrapper() });

    // Open dropdown
    await user.click(screen.getByRole('button', { name: /t\*\*\*r/i }));

    // Click redacted data to show popover
    const redactedEmail = screen.getByText('t***@lemondo.dev');
    await user.click(redactedEmail);

    // Click reveal button in popover
    const revealButton = screen.getByRole('button', { name: /reveal my data/i });
    await user.click(revealButton);

    // The SelfRevealDialog should open — look for the password input
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
  });

  describe('inline variant', () => {
    it('should render user info directly without a dropdown trigger', () => {
      render(<UserMenu variant="inline" />, { wrapper: createWrapper() });

      // User name and email should be visible directly (no need to click a trigger)
      expect(screen.getByText('T***r')).toBeInTheDocument();
      expect(screen.getByText('t***@lemondo.dev')).toBeInTheDocument();

      // There should be no dropdown trigger button with avatar
      const buttons = screen.getAllByRole('button');
      const avatarTrigger = buttons.find((b) => b.textContent?.includes('T*'));
      // The avatar trigger should not behave as a dropdown — it shouldn't be there
      expect(avatarTrigger).toBeUndefined();
    });

    it('should render sign out action directly', () => {
      render(<UserMenu variant="inline" />, { wrapper: createWrapper() });

      // Sign out should be visible without opening any dropdown
      expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument();
    });

    it('should render reveal action directly', () => {
      render(<UserMenu variant="inline" />, { wrapper: createWrapper() });

      // Reveal button should be visible without opening any dropdown
      expect(screen.getByRole('button', { name: /reveal my data/i })).toBeInTheDocument();
    });
  });
});
