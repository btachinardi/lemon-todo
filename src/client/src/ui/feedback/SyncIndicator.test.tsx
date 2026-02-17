import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { SyncIndicator } from './SyncIndicator';
import { useOfflineQueueStore } from '@/stores/use-offline-queue-store';

// Mock use-network-status
let mockIsOnline = true;
vi.mock('@/hooks/use-network-status', () => ({
  useNetworkStatus: () => mockIsOnline,
}));

describe('SyncIndicator', () => {
  beforeEach(() => {
    mockIsOnline = true;
    useOfflineQueueStore.setState({
      pendingCount: 0,
      isSyncing: false,
      lastSyncResult: null,
    });
  });

  it('should render nothing when online with no pending changes', () => {
    const { container } = render(<SyncIndicator />);
    expect(container.firstChild).toBeNull();
  });

  it('should show syncing indicator when isSyncing is true', () => {
    useOfflineQueueStore.setState({ isSyncing: true });

    render(<SyncIndicator />);

    expect(screen.getByText('Syncing...')).toBeInTheDocument();
  });

  it('should show all synced indicator when lastSyncResult is success', () => {
    useOfflineQueueStore.setState({ lastSyncResult: 'success' });

    render(<SyncIndicator />);

    expect(screen.getByText('All synced')).toBeInTheDocument();
  });

  it('should show pending count when offline with queued mutations', () => {
    mockIsOnline = false;
    useOfflineQueueStore.setState({ pendingCount: 3 });

    render(<SyncIndicator />);

    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('should render nothing when offline but no pending mutations', () => {
    mockIsOnline = false;
    useOfflineQueueStore.setState({ pendingCount: 0 });

    const { container } = render(<SyncIndicator />);
    expect(container.firstChild).toBeNull();
  });

  it('should prioritize syncing state over pending count', () => {
    mockIsOnline = false;
    useOfflineQueueStore.setState({ pendingCount: 5, isSyncing: true });

    render(<SyncIndicator />);

    expect(screen.getByText('Syncing...')).toBeInTheDocument();
  });
});
