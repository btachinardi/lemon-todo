import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, act } from '@testing-library/react';
import { useQueryClient } from '@tanstack/react-query';
import { QueryProvider } from './QueryProvider';

// Mock error-logger so it doesn't interfere
vi.mock('@/lib/error-logger', () => ({
  captureError: vi.fn(),
}));

/** Helper component that exposes the QueryClient for test assertions. */
function QueryClientSpy({ onClient }: { onClient: (client: ReturnType<typeof useQueryClient>) => void }) {
  const client = useQueryClient();
  onClient(client);
  return null;
}

describe('QueryProvider', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should invalidate all queries when offline-queue-drained event fires', () => {
    let capturedClient: ReturnType<typeof useQueryClient> | null = null;

    render(
      <QueryProvider>
        <QueryClientSpy onClient={(c) => { capturedClient = c; }} />
      </QueryProvider>,
    );

    expect(capturedClient).not.toBeNull();
    const invalidateSpy = vi.spyOn(capturedClient!, 'invalidateQueries');

    act(() => {
      window.dispatchEvent(new CustomEvent('offline-queue-drained'));
    });

    expect(invalidateSpy).toHaveBeenCalledTimes(1);
    expect(invalidateSpy).toHaveBeenCalledWith();
  });

  it('should not invalidate queries for unrelated events', () => {
    let capturedClient: ReturnType<typeof useQueryClient> | null = null;

    render(
      <QueryProvider>
        <QueryClientSpy onClient={(c) => { capturedClient = c; }} />
      </QueryProvider>,
    );

    const invalidateSpy = vi.spyOn(capturedClient!, 'invalidateQueries');

    act(() => {
      window.dispatchEvent(new Event('online'));
      window.dispatchEvent(new Event('offline'));
    });

    expect(invalidateSpy).not.toHaveBeenCalled();
  });

  it('should clean up event listener on unmount', () => {
    let capturedClient: ReturnType<typeof useQueryClient> | null = null;

    const { unmount } = render(
      <QueryProvider>
        <QueryClientSpy onClient={(c) => { capturedClient = c; }} />
      </QueryProvider>,
    );

    const invalidateSpy = vi.spyOn(capturedClient!, 'invalidateQueries');

    unmount();

    act(() => {
      window.dispatchEvent(new CustomEvent('offline-queue-drained'));
    });

    expect(invalidateSpy).not.toHaveBeenCalled();
  });
});
