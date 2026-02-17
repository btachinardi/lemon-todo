import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useSaveIndicator } from '../use-save-indicator';

describe('useSaveIndicator', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('should return synced when not pending and no queued mutations', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: false, pendingCount: 0 }),
    );
    expect(result.current).toBe('synced');
  });

  it('should return pending when isPending is true', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: true, pendingCount: 0 }),
    );
    expect(result.current).toBe('pending');
  });

  it('should return pending when isPending is true even with queued mutations', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: true, pendingCount: 2 }),
    );
    expect(result.current).toBe('pending');
  });

  it('should return not-synced when pendingCount is greater than zero', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: false, pendingCount: 3 }),
    );
    expect(result.current).toBe('not-synced');
  });

  it('should return synced when not pending and no queued mutations', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: false, pendingCount: 0 }),
    );
    expect(result.current).toBe('synced');
  });

  it('should return synced after mutation completes and no queued mutations', () => {
    const { result, rerender } = renderHook(
      (props) => useSaveIndicator(props),
      {
        initialProps: {
          isPending: true,
          pendingCount: 0,
        },
      },
    );

    expect(result.current).toBe('pending');

    rerender({ isPending: false, pendingCount: 0 });

    expect(result.current).toBe('synced');
  });

  it('should transition from not-synced to synced when queue drains', () => {
    const { result, rerender } = renderHook(
      (props) => useSaveIndicator(props),
      {
        initialProps: {
          isPending: false,
          pendingCount: 2,
        },
      },
    );

    expect(result.current).toBe('not-synced');

    rerender({ isPending: false, pendingCount: 0 });

    expect(result.current).toBe('synced');
  });
});
