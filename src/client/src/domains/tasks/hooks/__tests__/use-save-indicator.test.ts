import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useSaveIndicator } from '../use-save-indicator';

describe('useSaveIndicator', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('should return synced when online, not pending, and no queued mutations', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: false, isOnline: true, pendingCount: 0, taskId: 'task-1' }),
    );
    expect(result.current).toBe('synced');
  });

  it('should return pending when isPending is true', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: true, isOnline: true, pendingCount: 0, taskId: 'task-1' }),
    );
    expect(result.current).toBe('pending');
  });

  it('should return pending when isPending is true even if offline', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: true, isOnline: false, pendingCount: 2, taskId: 'task-1' }),
    );
    expect(result.current).toBe('pending');
  });

  it('should return not-synced when offline with pending mutations', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: false, isOnline: false, pendingCount: 3, taskId: 'task-1' }),
    );
    expect(result.current).toBe('not-synced');
  });

  it('should return not-synced when online but pendingCount is greater than zero', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: false, isOnline: true, pendingCount: 1, taskId: 'task-1' }),
    );
    expect(result.current).toBe('not-synced');
  });

  it('should return synced when offline but no pending mutations', () => {
    const { result } = renderHook(() =>
      useSaveIndicator({ isPending: false, isOnline: false, pendingCount: 0, taskId: 'task-1' }),
    );
    expect(result.current).toBe('synced');
  });

  it('should return synced after mutation completes and no queued mutations', () => {
    const { result, rerender } = renderHook(
      (props) => useSaveIndicator(props),
      {
        initialProps: {
          isPending: true,
          isOnline: true,
          pendingCount: 0,
          taskId: 'task-1',
        },
      },
    );

    expect(result.current).toBe('pending');

    rerender({ isPending: false, isOnline: true, pendingCount: 0, taskId: 'task-1' });

    expect(result.current).toBe('synced');
  });

  it('should reset to synced when taskId changes', () => {
    const { result, rerender } = renderHook(
      (props) => useSaveIndicator(props),
      {
        initialProps: {
          isPending: false,
          isOnline: false,
          pendingCount: 2,
          taskId: 'task-1',
        },
      },
    );

    expect(result.current).toBe('not-synced');

    rerender({ isPending: false, isOnline: true, pendingCount: 0, taskId: 'task-2' });

    expect(result.current).toBe('synced');
  });

  it('should transition from not-synced to synced when queue drains', () => {
    const { result, rerender } = renderHook(
      (props) => useSaveIndicator(props),
      {
        initialProps: {
          isPending: false,
          isOnline: false,
          pendingCount: 2,
          taskId: 'task-1',
        },
      },
    );

    expect(result.current).toBe('not-synced');

    // Come back online and queue drained
    rerender({ isPending: false, isOnline: true, pendingCount: 0, taskId: 'task-1' });

    expect(result.current).toBe('synced');
  });
});
