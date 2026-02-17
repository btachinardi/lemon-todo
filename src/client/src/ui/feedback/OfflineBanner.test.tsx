import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { OfflineBanner } from './OfflineBanner';

// Mock use-network-status
let mockIsOnline = true;
vi.mock('@/hooks/use-network-status', () => ({
  useNetworkStatus: () => mockIsOnline,
}));

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        'offline.cachedData': 'Viewing cached data',
        'offline.banner': 'You are offline',
      };
      return translations[key] ?? key;
    },
  }),
}));

function renderWithQueryClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>,
  );
}

describe('OfflineBanner', () => {
  beforeEach(() => {
    mockIsOnline = true;
  });

  it('should render nothing when online', () => {
    const { container } = renderWithQueryClient(<OfflineBanner />);
    expect(container.firstChild).toBeNull();
  });

  it('should show offline banner when network is down', () => {
    mockIsOnline = false;
    renderWithQueryClient(<OfflineBanner />);
    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('You are offline')).toBeInTheDocument();
  });

  it('should show cached data message when offline with cached queries', () => {
    mockIsOnline = false;
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    // Seed a cached query
    queryClient.setQueryData(['tasks'], [{ id: '1', title: 'Test' }]);

    render(
      <QueryClientProvider client={queryClient}>
        <OfflineBanner />
      </QueryClientProvider>,
    );

    expect(screen.getByText('Viewing cached data')).toBeInTheDocument();
  });

  it('should render without crashing when QueryClientProvider is an ancestor', () => {
    mockIsOnline = false;
    expect(() => renderWithQueryClient(<OfflineBanner />)).not.toThrow();
  });
});
