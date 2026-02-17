import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { AuthHydrationProvider } from './AuthHydrationProvider';

// Mock the auth store
vi.mock('@/domains/auth/stores/use-auth-store', () => ({
  useAuthStore: {
    getState: () => ({
      setAuth: vi.fn(),
    }),
  },
}));

// Mock API_BASE_URL
vi.mock('@/lib/api-client', () => ({
  API_BASE_URL: 'http://localhost:5155',
}));

describe('AuthHydrationProvider', () => {
  let fetchResolve: (value: Response) => void;

  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn(() =>
      new Promise<Response>((resolve) => {
        fetchResolve = resolve;
      }),
    ));
    // Default to online
    vi.stubGlobal('navigator', { ...navigator, onLine: true });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('should show a loading screen while the refresh is in-flight', () => {
    render(
      <AuthHydrationProvider>
        <div data-testid="child-content">App Content</div>
      </AuthHydrationProvider>,
    );

    // The loading screen should be visible
    expect(screen.getByRole('status')).toBeInTheDocument();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();

    // Children should NOT be rendered yet
    expect(screen.queryByTestId('child-content')).not.toBeInTheDocument();
  });

  it('should render children after the refresh completes successfully', async () => {
    render(
      <AuthHydrationProvider>
        <div data-testid="child-content">App Content</div>
      </AuthHydrationProvider>,
    );

    // Resolve the fetch with a non-ok response (no valid session)
    fetchResolve(new Response(null, { status: 401 }));

    await waitFor(() => {
      expect(screen.getByTestId('child-content')).toBeInTheDocument();
    });

    // Loading screen should be gone
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('should render children after the refresh fails with a network error', async () => {
    // Override fetch to reject
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('Network error'))));

    render(
      <AuthHydrationProvider>
        <div data-testid="child-content">App Content</div>
      </AuthHydrationProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('child-content')).toBeInTheDocument();
    });
  });

  it('should render children immediately when offline', async () => {
    vi.stubGlobal('navigator', { ...navigator, onLine: false });

    render(
      <AuthHydrationProvider>
        <div data-testid="child-content">App Content</div>
      </AuthHydrationProvider>,
    );

    // When offline, performSilentRefresh returns immediately, so children
    // should appear after the microtask resolves
    await waitFor(() => {
      expect(screen.getByTestId('child-content')).toBeInTheDocument();
    });
  });

  it('should display the LemonDo logo in the loading screen', () => {
    render(
      <AuthHydrationProvider>
        <div>App Content</div>
      </AuthHydrationProvider>,
    );

    const logo = screen.getByAltText(/lemon/i);
    expect(logo).toBeInTheDocument();
    expect(logo.tagName).toBe('IMG');
  });
});
